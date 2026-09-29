"""`up anim`：AnimationClip 的 curve binding 摘要（哪些 path / 屬性有曲線），可選對照 prefab。

為什麼要有：查「某個 clip 有沒有動到 root / collider / Rigidbody」「換皮 variant 之後曲線
是不是整批落空」以前只能 grep `.anim` 的 `path:` / `attribute:` 行（2026-09-29 廢鐵青蛙
stun 穿地調查）。.anim 動輒幾萬字元，幾乎全是 keyframe，binding 資訊不到 1%。

- binding 摘要走離線文字掃描：.anim 是機器產生的固定格式，不需要 Unity。
- `--prefab` 走 Unity（LoadPrefabContents）：variant 繼承來的節點、activeSelf 只有合併後才是真值，
  離線 YAML 看不到（見 uprefab skill 的 internals.md）。這段用 inline execute-dynamic-code，
  不另開 C# 入口 —— 唯讀、一次來回，免得「新指令要先 compile 才能用」。
"""

from __future__ import annotations

import difflib
import json
import os
import re

import assetyaml as ay
import unity

USAGE = (
    "用法：up anim <clip.anim> [--values [--keys N] [--budget N]] [--path KW] [--prefab <prefab>] [--animator KW] [--limit N]\n"
    "  clip.anim       AnimationClip 檔（repo 相對或絕對路徑；只給檔名片段會列候選）\n"
    "                  FBX 裡的 clip（xxx.fbx）不支援 —— 先 Extract 成 .anim\n"
    "  --prefab P      拿 path 去對 P 裡 Animator 底下的節點（需要 Unity）：\n"
    "                  標出對不到的 path（✗）、落在 inactive 節點的（~）、節點上沒有那顆 component 的（?）\n"
    "  --animator KW   prefab 有多個 Animator 時，用節點路徑片段指定（預設挑 controller 有用到這支 clip、\n"
    "                  且對到最多 path 的那個）\n"
    "  --values        每條曲線加印 keyframe 值（時間 → 值；常數曲線印「全程 = v」，key 多時摺疊成前幾格 + 範圍）\n"
    "  --keys N        --values 每條曲線最多展開幾格（預設 6）\n"
    "  --budget N      --values 的輸出字元上限（預設 6000，0 = 不限）\n"
    "  --path KW       只列 path 含 KW 的 binding（例：--path UnlockOn）\n"
    "  --limit N       最多列幾個 path（預設 60，0 = 不限）\n"
    "例：up anim \"Assets/0_Gameplay/Safe Zone 小廟安全點/[Lock] Locked 小廟.anim\" --values\n"
    "    up anim \"Assets/.../BigHurt Enemy 洗衣機怪 Logic.anim\" "
    "--prefab \"Assets/.../1_Enemy 廢鐵青蛙 Variant.prefab\""
)

# 只收常見的；對不到就印 classID，至少不會錯
CLASS_NAMES = {
    1: "GameObject", 4: "Transform", 8: "Behaviour", 20: "Camera", 23: "MeshRenderer",
    25: "Renderer", 33: "MeshFilter", 50: "Rigidbody2D", 54: "Rigidbody", 56: "Collider",
    58: "CircleCollider2D", 60: "PolygonCollider2D", 61: "BoxCollider2D", 64: "MeshCollider",
    65: "BoxCollider", 68: "EdgeCollider2D", 70: "CapsuleCollider2D", 82: "AudioSource",
    95: "Animator", 96: "TrailRenderer", 108: "Light", 111: "Animation", 114: "MonoBehaviour",
    120: "LineRenderer", 135: "SphereCollider", 136: "CapsuleCollider",
    137: "SkinnedMeshRenderer", 143: "CharacterController", 146: "WheelCollider",
    198: "ParticleSystem", 199: "ParticleSystemRenderer", 212: "SpriteRenderer",
    222: "CanvasRenderer", 223: "Canvas", 224: "RectTransform", 225: "CanvasGroup",
    331: "SpriteMask",
}
# 動到就可能影響物理 / 碰撞 / 位移 —— 「stun 時穿地」這類問題的第一嫌疑
PHYSICS_CLASSES = {50, 54, 56, 58, 60, 61, 64, 65, 68, 70, 135, 136, 143, 146}

# 各 curve 區段 → (固定 component, 固定屬性)。None = 從 entry 的 attribute / classID 讀
SECTIONS = {
    "m_RotationCurves": ("Transform", "localRotation"),
    "m_CompressedRotationCurves": ("Transform", "localRotation"),
    "m_EulerCurves": ("Transform", "localEulerAngles"),
    "m_PositionCurves": ("Transform", "localPosition"),
    "m_ScaleCurves": ("Transform", "localScale"),
    "m_FloatCurves": None,
    "m_PPtrCurves": None,
    # editor 端的拆分版（x/y/z 各一條），跟上面重複 —— 合併後去重，只為 runtime 區段是空的舊 clip 保底
    "m_EditorCurves": None,
    "m_EulerEditorCurves": None,
}
ATTR_ALIASES = {
    "m_LocalPosition": "localPosition", "m_LocalRotation": "localRotation",
    "m_LocalScale": "localScale", "m_LocalEulerAngles": "localEulerAngles",
    "localEulerAnglesRaw": "localEulerAngles", "localEulerAnglesBaked": "localEulerAngles",
    "localEulerAngles": "localEulerAngles",
}
COMPONENT_SUFFIX = re.compile(r"\.(x|y|z|w|r|g|b|a)$")
SECTION_RE = re.compile(r"^  (m_\w+):")
ENTRY_KEY_RE = re.compile(r"^  (?:- |  )(attribute|path|classID|script): ?(.*)$")
ENTRY_START_RE = re.compile(r"^  - ")
TOP_SCALAR_RE = re.compile(r"^  (m_\w+): (.+)$")
SETTING_RE = re.compile(r"^    (m_\w+): (.+)$")
EVENT_FN_RE = re.compile(r"^    functionName: (.*)$")
GUID_RE = re.compile(r"guid: ([0-9a-f]{32})")


def _scalar(raw: str) -> str:
    raw = raw.strip()
    if raw.startswith('"') and raw.endswith('"'):
        try:
            return json.loads(raw)
        except ValueError:
            return raw[1:-1]
    if raw.startswith("'") and raw.endswith("'"):
        return raw[1:-1].replace("''", "'")
    return raw


class Clip:
    def __init__(self):
        self.name = ""
        self.top: dict[str, str] = {}
        self.settings: dict[str, str] = {}
        self.events: list[str] = []
        # path → {(component, prop)}；component 已換成名字
        self.bindings: dict[str, set[tuple[str, str]]] = {}
        self.script_guids: set[str] = set()


def parse(path: str) -> Clip:
    clip = Clip()
    section = None
    entry: dict[str, str] | None = None

    def flush():
        nonlocal entry
        if entry is not None and section in SECTIONS and "path" in entry:
            fixed = SECTIONS[section]
            if fixed:
                comp, prop = fixed
            else:
                cid = int(entry.get("classID") or 0)
                comp = CLASS_NAMES.get(cid, f"classID {cid}")
                if cid == 114:
                    m = GUID_RE.search(entry.get("script", ""))
                    if m:
                        comp = "script:" + m.group(1)
                        clip.script_guids.add(m.group(1))
                attr = entry.get("attribute", "?")
                base = COMPONENT_SUFFIX.sub("", attr)
                prop = ATTR_ALIASES.get(base, base)
            clip.bindings.setdefault(entry["path"], set()).add((comp, prop))
        entry = None

    with open(path, encoding="utf-8", errors="replace") as fh:
        for ln in fh:
            ln = ln.rstrip("\n")
            m = SECTION_RE.match(ln)
            if m:
                flush()
                section = m.group(1)
                t = TOP_SCALAR_RE.match(ln)
                if t:
                    clip.top[section] = t.group(2)
                    if section == "m_Name":
                        clip.name = _scalar(t.group(2))
                continue
            if section == "m_AnimationClipSettings":
                s = SETTING_RE.match(ln)
                if s:
                    clip.settings[s.group(1)] = s.group(2)
                continue
            if section == "m_Events":
                e = EVENT_FN_RE.match(ln)
                if e:
                    clip.events.append(_scalar(e.group(1)))
                continue
            if section not in SECTIONS:
                continue
            if ENTRY_START_RE.match(ln):
                flush()
                entry = {}
            k = ENTRY_KEY_RE.match(ln)
            if k and entry is not None:
                val = k.group(2)
                entry[k.group(1)] = _scalar(val) if k.group(1) == "path" else val.strip()
    flush()
    return clip


def _script_names(root: str, guids: set[str]) -> dict[str, str]:
    if not guids:
        return {}
    try:
        import indexer
        con = indexer.connect(root)
        q = ",".join("?" * len(guids))
        return {g: c for g, c in con.execute(
            f"SELECT guid, class FROM scripts WHERE guid IN ({q})", tuple(guids))}
    except Exception:
        return {}


def _danger(path: str, comp: str, prop: str, cid_names_physics: set[str]) -> str | None:
    if prop in ("RootT", "RootQ", "MotionT", "MotionQ"):
        return "root motion 曲線"
    if path == "" and comp == "Transform":
        return "動到 Animator 那層（root）的 Transform"
    if comp in cid_names_physics:
        return f"動到 {comp}"
    if comp == "GameObject" and prop == "m_IsActive":
        return "開關 GameObject"
    if prop == "m_Enabled":
        return f"開關 {comp}.enabled"
    return None


def _find_clip(root: str, arg: str) -> str:
    cand = arg if os.path.isabs(arg) else os.path.join(root, arg)
    if os.path.isfile(cand):
        if not cand.lower().endswith(".anim"):
            ext = os.path.splitext(cand)[1] or "（沒有副檔名）"
            raise SystemExit(
                f"# up anim 只吃 .anim，收到的是 {ext}。"
                + ("FBX 裡的 clip 要先在 Unity 選 clip → Ctrl+D 抽成 .anim。" if ext.lower() == ".fbx" else
                   "controller 的 state / transition / 用到哪些 clip：`up controller <path>`。"
                   if ext.lower() in (".controller", ".overridecontroller") else
                   "材質的值：`up mat <path.mat>`。" if ext.lower() == ".mat" else "")
                + f"\n{USAGE}")
        return cand
    # 找不到 → 用檔名片段在 Assets/ 底下找 .anim 候選（只在錯誤路徑上掃，平常不花這個時間）
    key = os.path.basename(arg.rstrip("/")).lower()
    if key.endswith(".anim"):
        key = key[:-5]
    hits = []
    for d, _, files in os.walk(os.path.join(root, "Assets")):
        for f in files:
            if f.lower().endswith(".anim") and key in f.lower():
                hits.append(os.path.relpath(os.path.join(d, f), root))
    if len(hits) == 1:
        print(f"# 找不到 {arg}，改用唯一候選 {hits[0]}")
        return os.path.join(root, hits[0])
    if hits:
        shown = "\n".join(f"#   up anim \"{h}\"" for h in sorted(hits)[:10])
        more = f"\n# …還有 {len(hits) - 10} 筆，給更長的片段" if len(hits) > 10 else ""
        raise SystemExit(f"# 找不到 {arg}；檔名含「{key}」的 .anim 有 {len(hits)} 筆：\n{shown}{more}")
    raise SystemExit(f"# 找不到 {arg}，Assets/ 底下也沒有檔名含「{key}」的 .anim。\n{USAGE}")


# ---- --prefab：Unity 端合併後的節點表 ----

PREFAB_CODE = r"""
var root = UnityEditor.PrefabUtility.LoadPrefabContents(%(prefab)s);
try {
  var clipPath = %(clip)s;
  var sb = new System.Text.StringBuilder();
  System.Func<UnityEngine.Transform, UnityEngine.Transform, string> rel = (from, t) => {
    var parts = new System.Collections.Generic.List<string>();
    for (var c = t; c != null && c != from; c = c.parent) parts.Add(c.name);
    parts.Reverse();
    return string.Join("/", parts);
  };
  foreach (var an in root.GetComponentsInChildren<UnityEngine.Animator>(true)) {
    var uses = false;
    var ctrl = an.runtimeAnimatorController;
    if (ctrl != null)
      foreach (var c in ctrl.animationClips)
        if (c != null && UnityEditor.AssetDatabase.GetAssetPath(c) == clipPath) { uses = true; break; }
    sb.Append("A\t").Append(rel(root.transform.parent, an.transform)).Append('\t')
      .Append(uses ? 1 : 0).Append('\t').Append(ctrl != null ? ctrl.name : "-").Append('\n');
    foreach (var t in an.GetComponentsInChildren<UnityEngine.Transform>(true)) {
      sb.Append("N\t").Append(rel(an.transform, t)).Append('\t').Append(t.gameObject.activeSelf ? 1 : 0).Append('\t');
      var first = true;
      foreach (var comp in t.GetComponents<UnityEngine.Component>()) {
        if (!first) sb.Append(',');
        first = false;
        sb.Append(comp == null ? "Missing" : comp.GetType().Name);
      }
      sb.Append('\n');
    }
  }
  return sb.ToString();
} finally { UnityEditor.PrefabUtility.UnloadPrefabContents(root); }
"""


class AnimNode:
    __slots__ = ("path", "active_self", "comps")

    def __init__(self, path, active_self, comps):
        self.path, self.active_self, self.comps = path, active_self, comps


def _load_prefab(prefab: str, clip_unity_path: str):
    code = PREFAB_CODE % {"prefab": unity.lit(prefab), "clip": unity.lit(clip_unity_path)}
    try:
        out = unity.csharp(code, method="uprefab.anim.PrefabNodes")
    except unity.UnityError as e:
        msg = str(e)
        if "Unable to load" in msg or "not a prefab" in msg.lower() or "ArgumentException" in msg:
            raise SystemExit(f"# Unity 載不到 prefab {prefab}：{msg.splitlines()[0][:200]}") from None
        raise SystemExit(
            f"# --prefab 要 Unity 開著（不帶 --prefab 的 binding 摘要是離線的，可以先跑）。\n{msg}") from None
    animators = []  # [(path, uses, ctrl, {rel: AnimNode})]
    for ln in out.splitlines():
        cols = ln.split("\t")
        if cols[0] == "A" and len(cols) >= 4:
            animators.append((cols[1], cols[2] == "1", cols[3], {}))
        elif cols[0] == "N" and len(cols) >= 4 and animators:
            animators[-1][3][cols[1]] = AnimNode(cols[1], cols[2] == "1",
                                                 cols[3].split(",") if cols[3] else [])
    return animators


def _comp_present(comp: str, comps: list[str]) -> bool:
    if comp in ("GameObject", "Transform", "Behaviour") or comp.startswith("classID"):
        return True
    if comp == "Renderer":
        return any(c.endswith("Renderer") for c in comps)
    if comp == "Collider":
        return any(c.endswith("Collider") for c in comps)
    if comp == "Transform":
        return True
    return comp in comps


def _match(path: str, nodes: dict) -> tuple[str, str]:
    """回 (狀態, 說明)。狀態：ok / inactive / missing。"""
    node = nodes.get(path)
    if node is None:
        parts = path.split("/")
        deepest = ""
        for i in range(len(parts), 0, -1):
            p = "/".join(parts[:i])
            if p in nodes:
                deepest = p
                break
        nxt = parts[len(deepest.split("/")) if deepest else 0]
        prefix = deepest + "/" if deepest else ""
        kids = sorted({k[len(prefix):].split("/")[0] for k in nodes
                       if k.startswith(prefix) and k != deepest and k})
        close = difflib.get_close_matches(nxt, kids, n=2, cutoff=0.5)
        hint = f"對到 `{deepest or '(Animator 本身)'}` 為止，底下沒有 `{nxt}`"
        if close:
            hint += f"（相近：{', '.join(close)}）"
        elif kids:
            hint += f"（實際子節點：{', '.join(kids[:5])}{' …' if len(kids) > 5 else ''}）"
        return "missing", hint
    parts = path.split("/") if path else []
    for i in range(1, len(parts) + 1):
        p = "/".join(parts[:i])
        n = nodes.get(p)
        if n is not None and not n.active_self:
            return "inactive", f"`{p}` activeSelf=false"
    return "ok", ""


# ---- --values：keyframe 值 ----
# 單一分量曲線是基本單位：(path, comp_raw, prop) → {分量字尾: [(t, v)]}。
# runtime 區段（Float / Position / Euler / Scale / Rotation / PPtr）優先，
# editor 區段（m_EditorCurves / m_EulerEditorCurves）只補 runtime 沒有的 —— 兩邊內容重複。

VEC_SECTIONS = {
    "m_PositionCurves": "localPosition", "m_ScaleCurves": "localScale",
    "m_EulerCurves": "localEulerAngles", "m_RotationCurves": "localRotation",
}
SCALAR_SECTIONS = ("m_FloatCurves", "m_EditorCurves", "m_EulerEditorCurves")
SUFFIX_ORDER = {c: i for i, c in enumerate("xyzwrgba")}
BOOLISH = {"m_IsActive": ("關", "開"), "m_Enabled": ("關", "開")}


def _comp_raw(item: dict) -> str:
    cid = int(ay.num(item.get("classID"), 0))
    comp = CLASS_NAMES.get(cid, f"classID {cid}")
    if cid == 114:
        _, g = ay.ref(item.get("script"))
        if g:
            comp = "script:" + g
    return comp


def parse_values(path: str):
    """回 (vals, pptr, compressed)。vals[(path, comp, prop)][suffix] = [(t, v)]；pptr 同 key → [(t, (fid, guid))]。"""
    doc = next((d for d in ay.load(path) if d.type_name == "AnimationClip"), None)
    body = doc.body if doc else {}
    vals: dict = {}
    pptr: dict = {}

    def put(key, suf, keys, runtime):
        slot = vals.setdefault(key, {})
        if suf in slot and not runtime:
            return
        slot[suf] = keys

    for sec, prop in VEC_SECTIONS.items():
        for it in body.get(sec) or []:
            p = it.get("path", "")
            per: dict = {}
            for k in (it.get("curve") or {}).get("m_Curve") or []:
                t = ay.num(k.get("time"))
                v = k.get("value") or {}
                if isinstance(v, dict):
                    for c, x in v.items():
                        per.setdefault(c, []).append((t, ay.num(x)))
            for c, keys in per.items():
                put((p, "Transform", prop), c, keys, True)
    for sec in SCALAR_SECTIONS:
        runtime = sec == "m_FloatCurves"
        for it in body.get(sec) or []:
            attr = it.get("attribute", "?")
            m = COMPONENT_SUFFIX.search(attr)
            suf = m.group(1) if m else ""
            base = COMPONENT_SUFFIX.sub("", attr)
            prop = ATTR_ALIASES.get(base, base)
            comp = _comp_raw(it)
            keys = [(ay.num(k.get("time")), ay.num(k.get("value")))
                    for k in (it.get("curve") or {}).get("m_Curve") or []]
            put((it.get("path", ""), comp, prop), suf, keys, runtime)
    for it in body.get("m_PPtrCurves") or []:
        keys = [(ay.num(k.get("time")), ay.ref(k.get("value"))) for k in it.get("curve") or []]
        pptr[(it.get("path", ""), _comp_raw(it), it.get("attribute", "?"))] = keys
    # 有 euler 就不印 quaternion（同一件事，euler 才看得懂）
    for (p, c, pr) in list(vals):
        if pr == "localRotation" and (p, c, "localEulerAngles") in vals:
            del vals[(p, c, pr)]
    compressed = bool(body.get("m_CompressedRotationCurves"))
    return vals, pptr, compressed


def _fmt_keys(rows: list, fmt, keep: int, boolish=None) -> str:
    """rows = [(t, v)]，v 已經是可比較的值。"""
    if not rows:
        return "（沒有 key）"
    first = rows[0][1]
    if all(v == first for _, v in rows):
        tag = f"（{boolish[1 if first else 0]}）" if boolish and not isinstance(first, tuple) else ""
        n = f"，{len(rows)} 格" if len(rows) > 1 else ""
        return f"全程 = {fmt(first)}{tag}{n}"

    def one(t, v):
        tag = f"({boolish[1 if v else 0]})" if boolish and not isinstance(v, tuple) else ""
        return f"{ay.fmt_num(round(t, 4))}s→{fmt(v)}{tag}"
    if len(rows) <= keep:
        return ", ".join(one(t, v) for t, v in rows)
    # 摺疊時只留前幾格：純量 3 格、向量 2 格（向量一格就 30 字元，多了反而看不到範圍）
    n_head = min(max(1, keep - 1), 2 if isinstance(first, tuple) else 3)
    head = ", ".join(one(t, v) for t, v in rows[:n_head])
    rng = ""
    flat = [v for _, v in rows if not isinstance(v, tuple)]
    if flat:
        rng = f"，範圍 {fmt(min(flat))}~{fmt(max(flat))}"
    elif rows and isinstance(rows[0][1], tuple) and all(isinstance(x, float) for x in rows[0][1]):
        lo = tuple(min(v[i] for _, v in rows) for i in range(len(rows[0][1])))
        hi = tuple(max(v[i] for _, v in rows) for i in range(len(rows[0][1])))
        rng = f"，範圍 {fmt(lo)}~{fmt(hi)}"
    return f"{head}, … 共 {len(rows)} 格{rng}，最後 {one(*rows[-1])}"


def _fmt_v(v) -> str:
    if isinstance(v, tuple):
        return "(" + ", ".join(ay.fmt_num(round(x, 4)) for x in v) + ")"
    return ay.fmt_num(round(v, 4))


def value_lines(slot: dict, prop: str, keep: int) -> list[str]:
    """一個 (comp, prop) 的值 → 1~N 行（分量時間軸一致就併成向量，不一致就逐分量）。"""
    sufs = sorted(slot, key=lambda c: SUFFIX_ORDER.get(c, 99))
    boolish = BOOLISH.get(prop)
    if len(sufs) == 1:
        return [_fmt_keys(slot[sufs[0]], _fmt_v, keep, boolish)]
    times = [tuple(round(t, 5) for t, _ in slot[c]) for c in sufs]
    if all(t == times[0] for t in times):
        rows = [(slot[sufs[0]][i][0], tuple(slot[c][i][1] for c in sufs)) for i in range(len(times[0]))]
        return [f"({','.join(sufs)}) " + _fmt_keys(rows, _fmt_v, keep)]
    return [f".{c}: " + _fmt_keys(slot[c], _fmt_v, keep, boolish) for c in sufs]


def run(args, root: str, resolve_prefab) -> None:
    clip_path = _find_clip(root, args.clip)
    clip = parse(clip_path)
    rel = os.path.relpath(clip_path, root)
    scripts = _script_names(root, clip.script_guids)

    def comp_name(c: str) -> str:
        if c.startswith("script:"):
            return scripts.get(c[7:], f"MonoBehaviour(guid {c[7:15]}…)")
        return c

    bindings = {p: sorted({(comp_name(c), pr) for c, pr in s}) for p, s in clip.bindings.items()}
    path_kw = getattr(args, "path_kw", None)
    all_count = len(bindings)
    if path_kw:
        bindings = {p: v for p, v in bindings.items() if path_kw.lower() in p.lower()}
    values = getattr(args, "values", False)
    vals = pptr = None
    if values:
        raw_vals, raw_pptr, compressed = parse_values(clip_path)
        vals, pptr = {}, {}
        for (p, c, pr), slot in raw_vals.items():
            vals.setdefault(p, {})[(comp_name(c), pr)] = slot
        ref_guids = {g for keys in raw_pptr.values() for _, (_, g) in keys if g}
        ref_paths = ay.resolve_guids(root, ref_guids)
        for (p, c, pr), keys in raw_pptr.items():
            rows = [(t, os.path.basename(ref_paths.get(g, f"guid {g[:8]}…")) + (f":{f}" if f not in (0, 2100000, 7400000, 21300000) and g else "")
                     if g else ("None" if not f else f"fileID {f}")) for t, (f, g) in keys]
            pptr.setdefault(p, {})[(comp_name(c), pr)] = rows
    physics_names = {CLASS_NAMES[i] for i in PHYSICS_CLASSES}

    st = clip.settings
    try:
        length = float(st.get("m_StopTime", "0")) - float(st.get("m_StartTime", "0"))
    except ValueError:
        length = 0.0
    loop = st.get("m_LoopTime", "0") == "1"
    root_motion_flags = [k for k in ("m_HasGenericRootTransform", "m_HasMotionFloatCurves")
                         if clip.top.get(k, "0").strip() == "1"]
    root_curves = "" in bindings
    rm = []
    if root_motion_flags:
        rm.append("+".join(root_motion_flags) + "=1")
    if root_curves:
        rm.append("root(\"\") 有曲線")
    if any(pr in ("RootT", "RootQ", "MotionT", "MotionQ") for s in bindings.values() for _, pr in s):
        rm.append("有 RootT/RootQ/MotionT/MotionQ")
    n_props = sum(len(s) for s in bindings.values())
    print(f"# clip: {clip.name}  ({rel})")
    print(f"# 長度 {length:.2f}s @{clip.top.get('m_SampleRate', '?').strip()}fps  "
          f"loop={'是' if loop else '否'}  legacy={'是' if clip.top.get('m_Legacy', '0').strip() == '1' else '否'}  "
          f"root motion: {'；'.join(rm) if rm else '無'}")
    if clip.events:
        print(f"# events {len(clip.events)} 個：{', '.join(clip.events[:8])}{' …' if len(clip.events) > 8 else ''}")
    print(f"# {len(bindings)} 個 path / {n_props} 組屬性（.x/.y/.z/.w 已合併）"
          + (f"，篩 --path {path_kw}（全部 {all_count} 個 path）" if path_kw else ""))
    if not bindings:
        print("# （沒有任何曲線）" if not path_kw else f"# （沒有 path 含「{path_kw}」的 binding）")
        return
    if values:
        print(f"# --values：每條曲線 時間→值；常數曲線印「全程 = v」；超過 {args.keys} 格摺疊"
              + ("；⚠ 有壓縮的 rotation 曲線（m_CompressedRotationCurves），值不解" if compressed else ""))

    anim_nodes = None
    if args.prefab:
        prefab = resolve_prefab(root, args.prefab)
        clip_unity = rel.replace(os.sep, "/")
        animators = _load_prefab(prefab, clip_unity)
        if not animators:
            raise SystemExit(f"# {prefab} 裡沒有 Animator（含 inactive 節點）—— 這支 clip 不會被它播")
        cands = animators
        if args.animator:
            cands = [a for a in animators if args.animator in a[0]]
            if not cands:
                raise SystemExit("# --animator 沒對到。這支 prefab 的 Animator：\n"
                                 + "\n".join(f"#   {a[0]}  (controller {a[2]})" for a in animators))
        using = [a for a in cands if a[1]]
        pool = using or cands

        def score(a):
            return sum(1 for p in bindings if _match(p, a[3])[0] != "missing")
        chosen = max(pool, key=score)
        why = "controller 有用到這支 clip" if chosen[1] else "⚠ 沒有任何 Animator 的 controller 用到這支 clip，挑對到最多 path 的"
        print(f"# prefab: {prefab}")
        print(f"# Animator 節點: {chosen[0]}  (controller {chosen[2]}；{why})")
        others = [a[0] for a in animators if a is not chosen]
        if others:
            print(f"# 其他 Animator（--animator 切換）：{', '.join(others[:4])}{' …' if len(others) > 4 else ''}")
        anim_nodes = chosen[3]

    lines = []
    dangers = []
    counts = {"ok": 0, "inactive": 0, "missing": 0}
    seen_notes: set[str] = set()
    for p in sorted(bindings):
        props = bindings[p]
        status, note = ("ok", "")
        node = None
        if anim_nodes is not None:
            status, note = _match(p, anim_nodes)
            counts[status] += 1
            node = anim_nodes.get(p)
        parts = []
        for comp, pr in props:
            d = _danger(p, comp, pr, physics_names)
            tag = ""
            if d:
                dangers.append(f"{p or '(root)'} → {comp}.{pr}：{d}")
                tag = "⚠"
            if node is not None and not _comp_present(comp, node.comps):
                tag += "?"
            parts.append(f"{tag}{comp}.{pr}")
        # 同一 component 的屬性收成一組，行比較短
        mark = {"ok": " ", "inactive": "~", "missing": "✗"}[status] if anim_nodes is not None else " "
        line = f"{mark} {p or '(root)'}" + ("" if values else "  " + ", ".join(parts))
        if note:
            # 同一個斷點（例：整批 ViewRoot/Prototype/... 都卡在同一層）只完整印一次
            line += f"   [{note if note not in seen_notes else '同上'}]"
            seen_notes.add(note)
        group = [line]
        if values:
            for (comp, pr), label in zip(props, parts):
                slot = (vals.get(p) or {}).get((comp, pr))
                if slot is not None:
                    vl = value_lines(slot, pr, args.keys)
                else:
                    rows = (pptr.get(p) or {}).get((comp, pr))
                    vl = [_fmt_keys(rows, str, args.keys)] if rows is not None else None
                if not vl:
                    vl = ["（值沒讀到：可能只在壓縮 / editor 區段）"]
                if vl:
                    group.append(f"      {label}: {vl[0]}" if len(vl) == 1 else f"      {label}:")
                    if len(vl) > 1:
                        group.extend(f"        {x}" for x in vl)
        lines.append(group)

    limit = args.limit
    budget = ay.Budget(getattr(args, "budget", 0) if values else 0, "用 --path KW 只看幾個節點、或降 --keys")
    for grp in lines[: limit or None]:
        for ln in grp:
            budget.emit(ln)
    budget.finish()
    if limit and len(lines) > limit:
        print(f"# …還有 {len(lines) - limit} 個 path（--limit 0 全列，或 --path KW 篩）")

    if anim_nodes is not None:
        print(f"# 對照：{counts['ok']} 對到 active / {counts['inactive']} 落在 inactive（~）/ "
              f"{counts['missing']} 找不到節點（✗）；? = 節點上沒有那顆 component（曲線一樣落空）")
        if counts["missing"] + counts["inactive"] == len(bindings):
            print("# ⚠ 整支 clip 沒有一條曲線會生效 —— 常見原因：換皮 variant 關掉 / 改名了原本的 view 節點")
    if dangers:
        print(f"# ⚠ 危險 binding {len(dangers)} 條（root / 物理 / enabled / active）：")
        for d in dangers[:20]:
            print(f"#   {d}")
        if len(dangers) > 20:
            print(f"#   …還有 {len(dangers) - 20} 條")
    else:
        print("# 危險 binding：無（沒動 root Transform / Collider / Rigidbody / CharacterController / enabled / active）")


def register(sub, resolve_prefab) -> None:
    pa = sub.add_parser(
        "anim",
        help="AnimationClip 的 curve binding 摘要（path → 屬性），--values 加印 keyframe 值，可對照 prefab 標出落空的曲線",
        description="讀 .anim 印每個 path 有哪些屬性有曲線、長度 / loop / root motion，"
                    "標出動到 root / Collider / Rigidbody / enabled / active 的 binding（離線）。\n" + USAGE,
        epilog=USAGE,
    )
    pa.add_argument("clip", help=".anim 路徑（或檔名片段）")
    pa.add_argument("--prefab", help="拿 path 去對這支 prefab 裡 Animator 底下的節點（需要 Unity）")
    pa.add_argument("--animator", metavar="KW", help="多個 Animator 時用節點路徑片段指定")
    pa.add_argument("--limit", type=int, default=60, help="最多列幾個 path（預設 60，0 = 不限）")
    pa.add_argument("--values", action="store_true", help="加印每條曲線的 keyframe 值")
    pa.add_argument("--keys", type=int, default=6, help="--values 每條曲線最多展開幾格（預設 6）")
    pa.add_argument("--budget", type=int, default=6000, help="--values 輸出字元上限（預設 6000，0 = 不限）")
    pa.add_argument("--path", dest="path_kw", metavar="KW", help="只列 path 含 KW 的 binding")
    pa.set_defaults(fn=lambda args, root, cfg: run(args, root, resolve_prefab))
