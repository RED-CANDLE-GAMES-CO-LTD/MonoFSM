"""`up mat <path.mat>`：材質的 shader、keyword、每個 property 的值（離線）。

為什麼要有：`up asset fields` 對 .mat 只給欄位型別，要看 `_EmissionColor` 是多少、
`_EMISSION` 開了沒只能 grep .mat（2026-09-29 StreetLamp emission 調查）。

- Material Variant（m_Parent 有值）的 .mat 只存「本檔 override 的屬性」，其他值在 parent
  那支 —— 所以預設沿 parent 鏈合併，`*` 標本檔改的；`--local` 只看本檔。
- 三層都沒寫的屬性吃 shader Properties 的預設值，這支不讀 shader 原始碼（會印提示）。
"""

from __future__ import annotations

import os

import assetyaml as ay

USAGE = (
    "用法：up mat <path.mat> [--prop KW] [--local] [--budget N]\n"
    "  path.mat     材質檔（repo 相對 / 絕對 / 只給檔名片段會列候選）\n"
    "  --prop KW    只列名稱含 KW 的屬性（不分大小寫，例：--prop emission）\n"
    "  --local      Material Variant 只看本檔 override 的屬性（預設沿 parent 鏈合併）\n"
    "  --budget N   輸出字元上限（預設 6000，0 = 不限）\n"
    "例：up mat Assets/0_Art/Env/Mat/BuildingSheet/LemonLight.mat --prop emission"
)

BUILTIN_SHADERS = {46: "Standard", 45: "Standard (Specular setup)", 10703: "Sprites/Default",
                   10720: "UI/Default", 10753: "Legacy Shaders/Diffuse"}
KIND_ORDER = ("Color", "Float", "Int", "Tex")


def _shader_name(root: str, fid: int, guid: str | None, paths: dict) -> str:
    if not guid:
        return "（沒有 shader）" if not fid else f"fileID {fid}"
    if guid in ay.BUILTIN_GUIDS:
        return BUILTIN_SHADERS.get(fid, f"builtin shader (fileID {fid})")
    rel = paths.get(guid)
    if not rel:
        return f"⚠ 找不到 shader（guid {guid}，可能是 missing shader → 粉紅）"
    ext = os.path.splitext(rel)[1].lower()
    name = None
    if ext == ".shader":
        try:
            with open(ay.disk_path(root, rel), encoding="utf-8-sig", errors="ignore") as fh:
                for _ in range(40):
                    ln = fh.readline()
                    if not ln:
                        break
                    s = ln.strip()
                    if s.startswith("Shader ") and '"' in s:
                        name = s.split('"')[1]
                        break
        except OSError:
            pass
    elif ext == ".shadergraph":
        name = "Shader Graphs/" + os.path.splitext(os.path.basename(rel))[0]
    return f"{name or os.path.basename(rel)}  ({rel})"


def _material_doc(path: str):
    for d in ay.load(path):
        if d.type_name == "Material":
            return d.body
    return None


def _props(body: dict) -> dict[str, tuple[str, object]]:
    """name → (kind, 原始值)。"""
    out: dict[str, tuple[str, object]] = {}
    sp = body.get("m_SavedProperties") or {}
    for key, kind in (("m_Colors", "Color"), ("m_Floats", "Float"), ("m_Ints", "Int"), ("m_TexEnvs", "Tex")):
        for item in sp.get(key) or []:
            if isinstance(item, dict):
                for name, val in item.items():
                    out[name] = (kind, val)
    return out


def _fmt_color(v) -> str:
    if not isinstance(v, dict):
        return str(v)
    vals = [ay.num(v.get(c)) for c in "rgba"]
    s = "(" + ", ".join(f"{x:.3f}".rstrip("0").rstrip(".") if x else "0" for x in vals) + ")"
    mx = max(vals[:3])
    if mx > 1:
        s += f"  HDR 最大分量 {mx:.2f}"
    return s


def _tex_parts(v) -> tuple[str | None, int, str]:
    """(guid, fileID, scale/offset 附註)。"""
    if not isinstance(v, dict):
        return None, 0, ""
    fid, guid = ay.ref(v.get("m_Texture"))
    extra = []
    sc, of = v.get("m_Scale") or {}, v.get("m_Offset") or {}
    if isinstance(sc, dict) and (ay.num(sc.get("x"), 1) != 1 or ay.num(sc.get("y"), 1) != 1):
        extra.append(f"scale({ay.fmt_num(ay.num(sc.get('x')))}, {ay.fmt_num(ay.num(sc.get('y')))})")
    if isinstance(of, dict) and (ay.num(of.get("x")) != 0 or ay.num(of.get("y")) != 0):
        extra.append(f"offset({ay.fmt_num(ay.num(of.get('x')))}, {ay.fmt_num(ay.num(of.get('y')))})")
    return guid, fid, "  ".join(extra)


def run(args, root: str) -> None:
    path = ay.find_file(root, args.path, (".mat",), "mat", USAGE)
    rel = os.path.relpath(path, root)
    body = _material_doc(path)
    if body is None:
        raise SystemExit(f"# {rel} 裡沒有 Material document（不是 Unity 文字序列化？）\n{USAGE}")

    # parent 鏈（最多 8 層，防循環）
    chain = [(rel, body)]
    seen = {rel}
    cur = body
    broken = None
    while not args.local and len(chain) < 8:
        _, pg = ay.ref(cur.get("m_Parent"))
        if not pg:
            break
        prel = ay.resolve_guids(root, [pg]).get(pg)
        if not prel or prel in seen or not os.path.isfile(ay.disk_path(root, prel)):
            broken = pg
            break
        pb = _material_doc(ay.disk_path(root, prel))
        if pb is None:
            broken = pg
            break
        chain.append((prel, pb))
        seen.add(prel)
        cur = pb

    merged: dict[str, tuple[str, object, int]] = {}
    for lvl in range(len(chain) - 1, -1, -1):
        for name, (kind, val) in _props(chain[lvl][1]).items():
            merged[name] = (kind, val, lvl)

    guids = set()
    sfid, sguid = ay.ref(body.get("m_Shader"))
    if sguid:
        guids.add(sguid)
    for kind, val, _ in merged.values():
        if kind == "Tex":
            g = _tex_parts(val)[0]
            if g:
                guids.add(g)
    paths = ay.resolve_guids(root, guids)

    print(f"# mat: {body.get('m_Name', '?')}  ({rel})")
    print(f"# shader: {_shader_name(root, sfid, sguid, paths)}")
    _, own_parent = ay.ref(body.get("m_Parent"))
    if own_parent:
        names = " → ".join(os.path.basename(p) for p, _ in chain[1:]) or "?"
        print(f"# Material Variant，parent 鏈：{names}"
              + (f"  ⚠ 斷在 guid {broken}（找不到 parent .mat）" if broken else "")
              + ("" if not args.local else "（--local：只看本檔）"))
    rq = str(body.get("m_CustomRenderQueue", "-1"))
    tags = body.get("stringTagMap") or {}
    misc = [f"renderQueue={'shader 預設' if rq == '-1' else rq}"]
    if isinstance(tags, dict) and tags:
        misc.append("tags " + ", ".join(f"{k}={v}" for k, v in tags.items()))
    if body.get("m_DoubleSidedGI") == "1":
        misc.append("doubleSidedGI")
    if body.get("m_EnableInstancingVariants") == "1":
        misc.append("GPU instancing")
    dsp = body.get("disabledShaderPasses") or []
    if dsp:
        misc.append("關掉的 pass: " + ", ".join(dsp))
    print("# " + "  ".join(misc))
    kws = body.get("m_ValidKeywords") or []
    print(f"# keywords 開著 {len(kws)} 個：{', '.join(kws) if kws else '（無）'}")
    bad = body.get("m_InvalidKeywords") or []
    if bad:
        print(f"# ⚠ shader 不認得的 keyword（m_InvalidKeywords，通常是換過 shader 的殘留）：{', '.join(bad)}")
    locked = body.get("m_LockedProperties")
    if locked:
        print(f"# locked（variant 子材質不能改）：{locked}")

    kw = (args.prop or "").lower()
    rows = [(n, k, v, l) for n, (k, v, l) in merged.items() if not kw or kw in n.lower()]
    n_local = sum(1 for *_, l in rows if l == 0)
    mark_local = len(chain) > 1
    print(f"# 屬性 {len(rows)} 個" + (f"（`*` = 本檔 override，{n_local} 個；其他繼承自 parent）" if mark_local else "")
          + (f"，篩 --prop {args.prop}" if kw else ""))
    if not rows:
        print("# （沒有符合的屬性）" + ("——沒寫在 .mat 裡的屬性吃 shader Properties 預設值" if kw else ""))
        return

    budget = ay.Budget(args.budget, "用 --prop KW 篩屬性")
    empty_tex = []
    for kind in KIND_ORDER:
        items = sorted((r for r in rows if r[1] == kind), key=lambda r: r[0].lower())
        if not items:
            continue
        label = {"Color": "Colors", "Float": "Floats", "Int": "Ints", "Tex": "Textures"}[kind]
        body_lines = []
        for name, _, val, lvl in items:
            mk = "*" if (mark_local and lvl == 0) else " "
            if kind == "Color":
                body_lines.append(f"{mk} {name} = {_fmt_color(val)}")
            elif kind == "Tex":
                g, fid, extra = _tex_parts(val)
                if not g and not extra:
                    empty_tex.append(mk.strip() + name)
                    continue
                tp = (paths.get(g) or f"⚠ missing guid {g}") if g else "（無）"
                if g in ay.BUILTIN_GUIDS:
                    tp = f"builtin fileID {fid}"
                body_lines.append(f"{mk} {name} = {tp}" + (f"  {extra}" if extra else ""))
            else:
                body_lines.append(f"{mk.strip()}{name}={ay.fmt_num(ay.num(val)) if not isinstance(val, dict) else val}")
        if kind in ("Float", "Int"):
            # 純量一個一行太浪費（FlatKit 一顆就 80 個 float），併成一行多個
            packed, cur = [], ""
            for item in body_lines:
                if cur and len(cur) + 2 + len(item) > 118:
                    packed.append(cur)
                    cur = ""
                cur = f"{cur}  {item}" if cur else f"  {item}"
            if cur:
                packed.append(cur)
            body_lines = packed
        if body_lines:
            budget.emit(f"## {label}")
            for ln in body_lines:
                budget.emit(ln)
    if empty_tex:
        budget.emit(f"## 空貼圖 {len(empty_tex)} 個：{', '.join(empty_tex)}")
    budget.finish()


def register(sub) -> None:
    pm = sub.add_parser(
        "mat", help="材質的 shader / keyword / 每個 property 的值（離線；Material Variant 會合併 parent 鏈）",
        description="讀 .mat 印 shader、開著的 keyword、Color / Float / Int / Texture 屬性值"
                    "（貼圖 guid 解成路徑）。Material Variant 預設沿 parent 鏈合併，* 標本檔 override。\n" + USAGE,
        epilog=USAGE)
    pm.add_argument("path", help=".mat 路徑（或檔名片段）")
    pm.add_argument("--prop", metavar="KW", help="只列名稱含 KW 的屬性")
    pm.add_argument("--local", action="store_true", help="variant 只看本檔 override 的屬性")
    pm.add_argument("--budget", type=int, default=6000, help="輸出字元上限（預設 6000，0 = 不限）")
    pm.set_defaults(fn=lambda args, root, cfg: run(args, root))
