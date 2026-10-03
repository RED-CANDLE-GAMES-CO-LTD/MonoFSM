"""`up verify-skills` —— 機械檢查 skill / agent / CLAUDE.md 裡的引用有沒有爛掉。

skill 記的是「現況快照」，必然 decay；Progress.md 記的是「當時為什麼」，是歷史事實、
永不 decay。所以要維護的只有前者。這支只做**機械可驗**的那一半：

  1. 路徑：文件裡提到的 `Assets/…prefab` / `.cs` 等是不是還在
  2. 型別：backtick 裡的 PascalCase 是不是還存在於離線索引（.cs + DLL public 型別白名單，見 dlltypes.py）
  3. 欄位：`Type._field` 的欄位是不是還在該型別上

語意過期（「這個 pattern 已經不建議用了」）機械查不到，那要靠 `--changed`：
用 git 找出近期動過的 .cs，反查哪些 skill 段落提到它們 —— 給人「這幾段要重讀」的
清單，而不是全域重掃 145KB。

誤判控制走 linter 慣例：第一次跑完用 `--baseline` 把現有雜訊寫進 .uprefab-skillignore，
之後只會看到新漂移。
"""

from __future__ import annotations

import difflib
import fnmatch
import json
import os
import re
import subprocess
import unicodedata

IGNORE_FILE = ".uprefab-skillignore"
DEFAULT_ROOTS = [".claude/skills", ".claude/agents", "MonoFSM/skills", "CLAUDE.md"]
PATH_EXTS = (".prefab", ".unity", ".asset", ".cs", ".md", ".py", ".sh", ".json",
             ".asmdef", ".shader", ".mat")
# backtick 裡的東西才檢查 —— 散文裡的英文大寫字全是誤判來源
_TICK_RE = re.compile(r"`([^`\n]{2,120})`")
_TYPE_RE = re.compile(r"^([A-Z][A-Za-z0-9]*)(?:<[^>]*>)?$")
_MEMBER_RE = re.compile(r"^([A-Z][A-Za-z0-9]*)\.([A-Za-z_][A-Za-z0-9_]*)$")
# 一般英文／Unity 通用詞，寫死免得每個專案都要 baseline 一遍
BUILTIN_IGNORE = {
    "Editor", "Inspector", "Unity", "Play", "Mode", "PlayMode", "Prefab", "Scene",
    "GameObject", "Component", "Transform", "MonoBehaviour", "ScriptableObject",
    "README", "TODO", "NOTE", "OK", "API", "CLI", "URL", "JSON", "YAML", "GUID",
    "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable",
    "True", "False", "None", "Null", "Debug", "Log", "LogError", "Warning", "Error",
    # Unity 訊息 / Editor / UI / .NET API：引擎 DLL 不在索引裡，但這些名字在任何 Unity 專案都成立
    "OnCollisionEnter", "OnCollisionStay", "OnCollisionExit",
    "OnTriggerEnter", "OnTriggerStay", "OnTriggerExit", "OnValidate", "OnDestroy",
    "EditorWindow", "EditorGUI", "EditorGUILayout", "EditorUtility",
    "SerializedObject", "SerializedProperty", "SerializedPropertyType", "GlobalObjectId",
    "InputField", "IPointerClickHandler", "IList", "IEnumerable", "IEnumerator",
}
# `VarXxxProviderRef` 這種是「一整批型別」的佔位寫法，不是指某個型別
_PLACEHOLDER_RE = re.compile(r"Xxx|XXX")


FILE_OPTOUT = "file:"


def _load_ignore(root: str) -> tuple[set[str], list[str]]:
    """(token 集合, 整份文件 opt-out 的 glob)。

    `file:<glob>` 那行 = 這批文件**不驗型別／欄位**（路徑照驗 —— 路徑爛掉永遠是真失效）。
    給 uloop 這種「整份都在描述外部協定」的 skill 用：裡面的 PascalCase 是 JSON 欄位名，
    逐 token 塞進 ignore 會讓 skillignore 變成幾百行沒人看的雜訊，而且協定一改又要重 baseline。
    glob 用 fnmatch（`*` 會跨 `/`），對 repo 相對路徑比。"""
    p = os.path.join(root, IGNORE_FILE)
    out = set(BUILTIN_IGNORE)
    globs: list[str] = []
    if os.path.exists(p):
        with open(p, encoding="utf-8") as fh:
            for ln in fh:
                ln = ln.split("#", 1)[0].strip()
                if ln.startswith(FILE_OPTOUT):
                    globs.append(ln[len(FILE_OPTOUT):].strip())
                elif ln:
                    out.add(ln)
    return out, globs


_DECL_RE = re.compile(
    r"^(?P<path>[^:]+):\d+:(?:class|interface|struct|enum|record)\s+(?P<name>[A-Za-z_]\w*)"
    r"\s*(?:<[^>]*>)?\s*(?::(?P<bases>[^{]*))?")


def _src_decls(root: str):
    """從原始碼撈所有型別宣告：(型別名 → [檔案], 型別名 → [base], namespace 片段集合)。

    離線索引（scripts / comps / catalog）只收 MonoBehaviour / SO 這類掛得上去的型別，
    interface（`IAfterSimulate`）、Editor 類（`SubtreeSummarizerRegistry`）、泛型 helper
    （`VarWrapper<TVar,TValue>`）、namespace（`MonoValueProvider`）全都查不到，被報成「型別不存在」——
    但它們是真的在。partial class 的其他檔案（`GameData.Config.cs` 的 `_objConfigs`）也一樣，
    catalog 只記一個 path，欄位宣告在另一個檔就被報「欄位不存在」（2026-10-01，佔 81 筆裡約 15 筆）。

    用 `git grep --recurse-submodules`：MonoFSM / MonoFSM-Pro / MonoFSM-Photon-Fusion 都是 submodule，
    全 repo 9.7k 筆宣告 0.1 秒。跨行宣告（`class\\n    MonoBlackboard : …`）抓不到，量少就不管。"""
    paths: dict[str, list[str]] = {}
    bases: dict[str, list[str]] = {}
    spaces: set[str] = set()
    try:
        out = subprocess.run(
            ["git", "-C", root, "grep", "--recurse-submodules", "-n", "-o", "-E",
             r"(class|interface|struct|enum|record)[[:space:]]+[A-Za-z_][A-Za-z0-9_]*"
             r"([[:space:]]*<[^>]*>)?[[:space:]]*(:[^{]*)?", "--", "*.cs"],
            capture_output=True, text=True, timeout=60).stdout
        ns = subprocess.run(
            ["git", "-C", root, "grep", "--recurse-submodules", "-h", "-o", "-E",
             r"^[[:space:]]*namespace[[:space:]]+[A-Za-z_][A-Za-z0-9_.]*", "--", "*.cs"],
            capture_output=True, text=True, timeout=60).stdout
    except Exception as e:  # noqa: BLE001
        print(f"# ⚠ git grep 失敗，interface / Editor 類 / partial 欄位可能被誤報：{e}")
        return paths, bases, spaces
    for ln in out.splitlines():
        m = _DECL_RE.match(ln)
        if not m:
            continue
        name = m.group("name")
        paths.setdefault(name, []).append(m.group("path"))
        for b in (m.group("bases") or "").split(","):
            b = b.strip().split("<")[0].split(" where ")[0].strip()
            if b and re.match(r"^[A-Za-z_][\w.]*$", b):
                bases.setdefault(name, []).append(b.rsplit(".", 1)[-1])
    for ln in ns.splitlines():
        full = ln.split()[-1]
        spaces.add(full)
        spaces.update(full.split("."))
    return paths, bases, spaces


def _scan_files(root: str, prefix: str | None) -> list[str]:
    out = []
    for r in DEFAULT_ROOTS:
        full = os.path.join(root, r)
        if os.path.isfile(full):
            out.append(r)
        elif os.path.isdir(full):
            for dp, dn, fns in os.walk(full):
                dn[:] = [d for d in dn if not d.startswith(".")]
                out += [os.path.relpath(os.path.join(dp, f), root)
                        for f in fns if f.endswith(".md")]
    if prefix:
        out = [f for f in out if prefix.lower() in f.lower()]
    return sorted(out)


def _known(con) -> tuple[dict, dict]:
    """(型別小寫 → 正確大小寫, 型別小寫 → 欄位名集合)。三張表都算數：
    catalog 只收有 base 解得出 kind 的，scripts/comps 才涵蓋得到純資料類與 Unity 內建。"""
    names: dict[str, str] = {}
    fields: dict[str, set[str]] = {}
    for (c,) in con.execute("SELECT DISTINCT class FROM scripts WHERE class IS NOT NULL"):
        names[c.lower()] = c
    for (t,) in con.execute("SELECT DISTINCT type FROM comps WHERE type IS NOT NULL"):
        names.setdefault(t.lower(), t)
    for c, f in con.execute("SELECT class, fields FROM catalog"):
        names[c.lower()] = c
        try:
            fields[c.lower()] = {d["name"] for d in json.loads(f or "[]")}
        except Exception:
            pass
    return names, fields


# 型別檢查的判準：**只回報「查不到但有很像的」**。
# backtick 裡的 PascalCase 在散文中大多不是專案型別（JSON 欄位名、C# API、enum 值），
# 全報就是 1200 筆雜訊。反過來看：`NetworkedGameplayInputState` 不存在而
# `NetworkInputState` 存在 —— 那幾乎一定是改過名的舊引用，正是要抓的漂移；
# `KeyDown` 連像的都沒有，那它本來就不是專案型別，不該報。
NEAR_SUGGEST = 0.72   # 印建議用
NEAR_REPORT = 0.82    # 高到這個相似度才當成「疑似改名」報出來


ASSET_EXTS = (".prefab", ".unity", ".asset")


def _basenames(con) -> dict[str, list[str]]:
    """檔名 → 實際路徑。文件常用簡寫路徑（`Plug/[Detector] xxx.prefab`）指資產，
    拿 repo root 接一定找不到；改以檔名反查，順便能在資產搬家時指出新位置。"""
    out: dict[str, list[str]] = {}
    for (path,) in con.execute("SELECT path FROM assets"):
        path = _nfc(path)
        out.setdefault(os.path.basename(path), []).append(path)
    for (path,) in con.execute("SELECT path FROM scripts WHERE path IS NOT NULL"):
        path = _nfc(path)
        out.setdefault(os.path.basename(path), []).append(path)
    return out


def _nfc(s: str) -> str:
    # 檔案系統 / git 吐出來的中文檔名有可能是 NFD，文件裡打的是 NFC；兩邊統一再比
    return unicodedata.normalize("NFC", s)


def _path_variants(tok: str) -> list[str]:
    """反引號內容可能是「純路徑（本身含空白）」或「整條指令＋路徑參數」，分不出來就都試。

    2026-09-23 修：以前只要有空白就取最後一個字，`Assets/0_Gameplay/Train Station FSM.prefab`
    被切成 `FSM.prefab`，專案裡幾乎每條帶空白的中文 prefab 路徑都被誤報「不存在」。
    順序：原樣 → 從第一個含 `/` 的字開始（去掉 `up prefab read` 這類指令頭）→ 最後一個字。"""
    out = [tok]
    words = tok.split()
    for i, w in enumerate(words):
        if "/" in w:
            if i:
                out.append(" ".join(words[i:]))
            break
    if len(words) > 1:
        out.append(words[-1])
    return list(dict.fromkeys(out))


def _path_check(root: str, here: str, tok: str, bases: dict) -> tuple[bool, str, str | None]:
    """(過不過, 提示, 實際路徑)。三關：repo 根相對 → 文件自身資料夾相對 → 檔名反查索引。

    最後一關同時解掉兩件事：文件裡的簡寫路徑不該被誤報，以及資產真的搬家時
    要直接把新路徑講出來，而不是只說「不存在」讓下一隻 agent 自己去找。
    實際路徑（repo 相對）給 guid 檢查用：簡寫對到多個檔、或根本不是檔案路徑時回 None。
    """
    t = _nfc(tok.rstrip("/"))
    if os.path.exists(os.path.join(root, t)):
        return True, "", t
    if os.path.exists(os.path.join(here, t)):
        return True, "", _nfc(os.path.relpath(os.path.join(here, t), root))
    if t.startswith("Packages/"):
        # 文件照 Unity 寫 `Packages/com.monofsm.core/…`（up prefab read 也只認這種），
        # 實體在 repo 是 `MonoFSM/…`；用 manifest 的 file: 對應換回來再查一次，
        # 不然每條合法的 Packages/ 路徑都會被報「不存在」（2026-09-15 module-assembly.md 兩條誤報）
        for repo_dir, pkg in _pkg_map(root).items():
            prefix = f"Packages/{pkg}/"
            real = os.path.join(repo_dir, t[len(prefix):])
            if t.startswith(prefix) and os.path.exists(os.path.join(root, real)):
                return True, "", _nfc(real)
    base = os.path.basename(t)
    if not base.endswith(ASSET_EXTS + (".cs",)):
        # 沒有副檔名又接不到實體路徑的，多半是 prefab 內的節點路徑（`Modules/`、
        # `CharacterModules/Character FSM/`），不是檔案，不歸這支管
        return True, "", None
    hit = bases.get(base)
    if hit:
        # 文件常寫簡寫路徑（`Physics Object/Fan/x.prefab`），要看的是「實際路徑以它結尾」；
        # `MonoFSM/…/MonoEntity.cs` 這種中段省略的，省略號當萬用字元
        tail = [h for h in hit if h.endswith(t)]
        if tail:
            return True, "", (tail[0] if len(tail) == 1 else None)
        if len(hit) > 1:
            return True, "", None
        if "…" in t or "..." in t:
            pat = ".*".join(re.escape(x) for x in re.split(r"…|\.\.\.", t)) + "$"
            if re.search(pat, hit[0]):
                return True, "", hit[0]
        return False, f"（實際在：{hit[0]}）", None
    near = difflib.get_close_matches(base, list(bases), n=1, cutoff=0.8)
    return False, (f"（相近檔名：{near[0]}）" if near else ""), None


def _pkg_map(root: str) -> dict:
    import uprefab
    return uprefab._pkg_map(root)


# ── guid ──────────────────────────────────────────────────────────────────────
# skill 寫 asset 一律「`路徑` (guid:<32 hex>)」：路徑給人和 agent 讀、可以直接拿去下 up 指令；
# guid 給這支定位 —— 改名 / 搬家時用 guid 查出現在在哪，--fix 直接換掉（2026-10-03）。
# 只有「有 .meta 的 asset」要 guid；.cs 不補（腳本改名幾乎都連型別一起改，型別檢查會抓到；
# 檔名反查也已經會印「實際在」），ProjectSettings/*.asset 沒有 .meta 也不補。
GUID_EXTS = (".prefab", ".unity", ".asset", ".mat", ".shader", ".asmdef")
_GUID_TAG_RE = re.compile(r"[ \t]*\(guid:([^)\s]*)\)")
_GUID_OK_RE = re.compile(r"^[0-9a-f]{32}$")


def _meta_guid(root: str, rel: str) -> str | None:
    try:
        with open(os.path.join(root, rel + ".meta"), encoding="utf-8", errors="ignore") as fh:
            for _ in range(6):
                ln = fh.readline()
                if not ln:
                    break
                if ln.startswith("guid:"):
                    return ln.split(":", 1)[1].strip()
    except OSError:
        pass
    return None


class _GuidIndex:
    """guid → 現在的路徑。先查離線索引（prefab / unity / asset），索引的路徑已不在磁碟上、
    或索引沒收的類型（.mat / .shader），才整個 repo 掃一次 .meta（約 4 萬個，只在需要時掃一次）。"""

    def __init__(self, con, root: str):
        self.con, self.root = con, root
        self._walk: dict[str, str] | None = None

    def path(self, guid: str) -> str | None:
        row = self.con.execute("SELECT path FROM assets WHERE guid=?", (guid,)).fetchone()
        if row and os.path.exists(os.path.join(self.root, row[0])):
            return _nfc(row[0])
        if self._walk is None:
            import uprefab
            self._walk = {}
            for dp, dn, fns in os.walk(self.root):
                dn[:] = [d for d in dn if d not in uprefab.META_SKIP_DIRS and not d.startswith(".")]
                for fn in fns:
                    if fn.endswith(".meta"):
                        rel = os.path.relpath(os.path.join(dp, fn[:-5]), self.root)
                        g = _meta_guid(self.root, rel)
                        if g:
                            self._walk[g] = _nfc(rel)
        return self._walk.get(guid)


def _as_written(root: str, cand: str, real: str) -> str:
    """換路徑時盡量保持文件原本的寫法：原本寫 `Packages/<pkg>/…` 就換回 Packages 形式。"""
    if cand.startswith("Packages/"):
        for repo_dir, pkg in _pkg_map(root).items():
            if real.startswith(repo_dir.rstrip("/") + "/"):
                return f"Packages/{pkg}/" + real[len(repo_dir.rstrip("/")) + 1:]
    return real


def _src_chain(con, root: str, decl_paths: dict, decl_bases: dict, fields: dict):
    """回傳 (has_member(cls, fld), chain_fields(cls))。

    has_member：沿繼承鏈讀 .cs 原始碼找這個成員有沒有宣告。
    catalog.fields 只收 serialized 欄位，`AbstractMonoVariable._valueSources` 這種
    [NonSerialized] / private 快取欄位查不到就被報「欄位不存在」—— 但它是真的在的
    （2026-09-23）。查不到 serialized 時再回原始碼確認，分成「非 serialized」跟「真的沒有」。

    chain_fields：catalog 的 fields 只有該 class 自己宣告的，`MonoEntity._descriptableTags`
    宣告在父類 `MonoBlackboard` 就被報「欄位不存在」（2026-10-01）；沿 base 把 serialized 欄位聯集起來。
    繼承鏈和 partial 檔都用 `_src_decls` 補 —— catalog 漏掉的中間層（`AbstractMonoDescriptable`）
    才接得起來。"""
    info: dict[str, tuple[list[str], list[str]]] = {}
    for c, path, bases in con.execute("SELECT class, path, bases FROM catalog"):
        info[c] = ([path] if path else [],
                   [b.strip().split("<")[0] for b in (bases or "").split(",") if b.strip()])
    for c, path in con.execute("SELECT class, path FROM scripts WHERE path IS NOT NULL"):
        info.setdefault(c, ([path], []))
    for c, ps in decl_paths.items():
        cur_paths, cur_bases = info.setdefault(c, ([], []))
        cur_paths += [p for p in ps if p not in cur_paths]
        cur_bases += [b for b in decl_bases.get(c, []) if b not in cur_bases]
    cache: dict[str, str] = {}

    def chain(cls: str) -> list[str]:
        seen, todo, out = set(), [cls], []
        while todo:
            c = todo.pop()
            if c in seen or c not in info:
                continue
            seen.add(c)
            out.append(c)
            todo += info[c][1]
        return out

    def chain_fields(cls: str) -> set[str]:
        fs: set[str] = set()
        for c in chain(cls):
            fs |= fields.get(c.lower(), set())
        return fs

    def src(path: str) -> str:
        if path not in cache:
            try:
                with open(os.path.join(root, path), encoding="utf-8", errors="ignore") as fh:
                    cache[path] = fh.read()
            except OSError:
                cache[path] = ""
        return cache[path]

    def has_member(cls: str, fld: str) -> bool:
        decl = re.compile(rf"[\w>\]?]\s+{re.escape(fld)}\s*(=|;|\{{|=>)")
        return any(decl.search(src(p)) for c in chain(cls) for p in info[c][0])

    return has_member, chain_fields


def _dll_names(con, root: str) -> set[str]:
    """DLL（Fusion / Photon）裡的 public 型別名。只拿來判斷「存在」，不進相近建議的 pool ——
    幾百個 DLL 型別混進 difflib 會把原本 weak 的散文詞拉成 strong，雜訊反而變多。
    查詢前順手對齊（DLL 沒變只花一次 os.walk）；讀不了就退回舊檢查、印一行提示。"""
    import dlltypes
    try:
        total, errors = dlltypes.refresh(con, root)
    except Exception as e:  # noqa: BLE001
        total, errors = 0, [f"{type(e).__name__}: {e}"]
    if not total:
        why = errors[0] if errors else "沒有讀到任何 DLL（看 .uprefab.json 的 dllTypes）"
        print(f"# ⚠ DLL 型別白名單不可用，退回只用 .cs 索引，Fusion 等 DLL 型別可能被誤報：{why}")
    elif errors:
        print(f"# ⚠ {len(errors)} 顆 DLL 讀不了，裡面的型別可能被誤報：{errors[0]}")
    return dlltypes.names(con)


def _near(tok: str, pool, n=2) -> list[str]:
    return difflib.get_close_matches(tok, pool, n=n, cutoff=NEAR_SUGGEST)


def _hint(hits: list[str]) -> str:
    return f"（相近：{'、'.join(hits)}）" if hits else ""


def _candidates(text: str):
    """產出 (行號, kind, (token, 原樣), tag)。kind = path / type / member。

    tag 只有 path 有：(guid 或 None, 反引號開頭 col, 反引號結尾 col, guid 標註結尾 col)，
    --fix 照這幾個位置改寫那一行；guid 標註是緊接在反引號後面的 `(guid:<hex>)`。"""
    for i, ln in enumerate(text.splitlines(), 1):
        if ln.lstrip().startswith("```"):
            continue
        for m in _TICK_RE.finditer(ln):
            tok = m.group(1).strip().rstrip(",.;:）)")
            if not tok:
                continue
            if "/" in tok and any(c in tok for c in "<>*?"):
                continue  # `Packages/<package-id>/`、`Module Test/*.unity` 是佔位／glob
            if "/" in tok and (tok.endswith(PATH_EXTS) or tok.endswith("/")):
                # 可能是整條指令包在反引號裡，也可能是本身帶空白的路徑 —— 交給 _path_variants
                g = _GUID_TAG_RE.match(ln, m.end())
                tag = (g.group(1) if g else None, m.start(), m.end(), g.end() if g else m.end())
                yield i, "path", (tok, tok), tag
            elif _MEMBER_RE.match(tok):
                yield i, "member", (tok, tok), None
            else:
                t = _TYPE_RE.match(tok)
                if t:
                    # 查表用裸名，回報用原樣 —— `Foo<T>` 的 T 不該影響「型別在不在」
                    yield i, "type", (t.group(1), tok), None


# 路徑類的失效：不進 --baseline（那些是真失效，不是誤判）
PATH_KINDS = {"路徑不存在", "改名", "guid 不一致", "guid 查不到", "guid 格式錯"}


def _check_path(root, here, tok, tag, bases, gidx):
    """一個路徑 token → (失效 kind 或 None, 提示, fix)。fix = (舊路徑字串或 None, 新路徑或 None, guid)；
    kind 是 "缺 guid" 時也帶 fix（--fix 補上）。"""
    why = ""
    ok, real, cand = False, None, tok
    for c in _path_variants(tok):
        ok, w, real = _path_check(root, here, c, bases)
        if ok:
            cand = c
            break
        why = why or w
    if not ok:
        cand = next((c for c in _path_variants(tok) if c.endswith(PATH_EXTS)), tok)
    is_asset = _nfc(cand).endswith(GUID_EXTS)
    guid = tag[0] if tag else None
    has_meta = bool(real) and os.path.exists(os.path.join(root, real + ".meta"))

    if guid is None:
        if not ok:
            return "路徑不存在", why, None
        if is_asset and has_meta:
            return "缺 guid", "", (None, None, _meta_guid(root, real))
        if is_asset and real is None and _nfc(cand).endswith(ASSET_EXTS):
            return "缺 guid", "（簡寫路徑對到不只一個檔，--fix 補不了：改寫完整路徑）", None
        return None, "", None
    if not is_asset:
        return None, "", None   # .cs / .md 上的 guid 標註不管
    if ok and real is None and not _nfc(cand).endswith(ASSET_EXTS):
        ok = False   # .mat / .shader 找不到時 _path_check 照舊放過；有 guid 就能判斷，當成失效

    if not _GUID_OK_RE.match(guid):
        if ok and has_meta:
            return "guid 格式錯", f"（要 32 位小寫 hex：{guid}）", (None, None, _meta_guid(root, real))
        return "guid 格式錯", f"（要 32 位小寫 hex：{guid}）", None
    if ok and has_meta and _meta_guid(root, real) == guid:
        return None, "", None
    gpath = gidx.path(guid)
    if gpath is None:
        if ok and has_meta:
            return "guid 查不到", f"（路徑還在，guid 寫錯；現在的 guid 是 {_meta_guid(root, real)}）", \
                (None, None, _meta_guid(root, real))
        if ok:
            return None, "", None   # 簡寫對到多個檔、guid 又查不到：沒得判斷，維持舊行為放過
        return "路徑不存在", f"（guid {guid} 也查不到 —— asset 已刪）", None
    if ok and real is None and gpath.endswith(_nfc(cand.rstrip("/"))):
        return None, "", None       # 簡寫路徑對到多個檔，guid 指的是其中一個
    new = _as_written(root, cand, gpath)
    if not ok:
        return "改名", f"（改名成：{new}）", (cand, new, guid)
    return "guid 不一致", f"（以 guid 為準，應該是：{new}）", (cand, new, guid)


def _apply_fixes(root: str, rel: str, edits: list) -> int:
    """edits = [(行號, tag, (舊, 新, guid))]。同一行從右往左改，col 才不會位移。"""
    p = os.path.join(root, rel)
    with open(p, encoding="utf-8", newline="") as fh:
        lines = fh.read().splitlines(keepends=True)
    n = 0
    by_line: dict[int, list] = {}
    for line, tag, fx in edits:
        by_line.setdefault(line, []).append((tag, fx))
    for line, items in by_line.items():
        ln = lines[line - 1]
        for (_, ts, te, tend), (old, new, guid) in sorted(items, key=lambda x: -x[0][1]):
            body = ln[ts + 1: te - 1]
            if old and new:
                if old not in body:
                    continue
                body = body.replace(old, new, 1)
            ln = ln[:ts] + "`" + body + "`" + (f" (guid:{guid})" if guid else "") + ln[tend:]
            n += 1
        lines[line - 1] = ln
    with open(p, "w", encoding="utf-8", newline="") as fh:
        fh.write("".join(lines))
    return n


def _scan(args, root, con, ctx):
    names, fields, bases, ignore, optout, dll_names, src_types, has_member, chain_fields, pool, files = ctx
    gidx = _GuidIndex(con, root)
    nonser: list[tuple[str, int, str]] = []   # 原始碼有、但不是 serialized 的欄位
    bad: dict[str, list[tuple]] = {}
    noguid: dict[str, list[tuple]] = {}       # 有路徑沒 guid
    fixes: dict[str, list[tuple]] = {}
    checked = 0
    weak = 0   # 查不到又沒有相近型別的 —— 多半根本不是專案型別，--loose 才列
    for rel in files:
        with open(os.path.join(root, rel), encoding="utf-8") as fh:
            text = fh.read()
        here = os.path.dirname(os.path.join(root, rel))
        types_off = any(fnmatch.fnmatch(rel, g) for g in optout)
        for line, kind, (tok, shown), tag in _candidates(text):
            if tok in ignore or shown in ignore:
                continue
            if types_off and kind != "path":
                continue
            if kind != "path" and _PLACEHOLDER_RE.search(tok):
                continue
            if kind == "path":
                checked += 1
                k, hint, fx = _check_path(root, here, tok, tag, bases, gidx)
                if fx:
                    fixes.setdefault(rel, []).append((line, tag, fx))
                if k == "缺 guid":
                    noguid.setdefault(rel, []).append((line, k, shown, hint))
                elif k:
                    bad.setdefault(rel, []).append((line, k, shown, hint))
            elif kind == "member":
                cls, fld = _MEMBER_RE.match(tok).group(1, 2)
                if cls.lower() not in names or cls in ignore:
                    continue
                fs = chain_fields(names[cls.lower()]) or fields.get(cls.lower())
                if not fs or not fld.startswith("_"):
                    continue  # 只驗序列化欄位（專案慣例以底線開頭），屬性/方法放過
                checked += 1
                if fld not in fs and has_member(names[cls.lower()], fld):
                    nonser.append((rel, line, shown))
                elif fld not in fs:
                    bad.setdefault(rel, []).append(
                        (line, "欄位不存在", shown, _hint(_near(fld, fs))))
            else:
                # 嚴格門檻：至少兩個大寫且夠長，才當它是型別名而不是普通英文字
                if len(tok) < 5 or sum(c.isupper() for c in tok) < 2:
                    continue
                checked += 1
                if tok.lower() in names or tok in dll_names or tok in src_types:
                    continue
                hits = _near(tok, pool)
                strong = hits and difflib.SequenceMatcher(
                    None, tok.lower(), hits[0].lower()).ratio() >= NEAR_REPORT
                if not (strong or args.loose):
                    weak += 1
                    continue
                bad.setdefault(rel, []).append((line, "型別不存在", shown, _hint(hits)))
    return bad, noguid, fixes, nonser, checked, weak


def cmd(args, root, cfg):
    import indexer

    if args.changed is not None:
        return _changed(args, root)

    con = indexer.connect(root)
    names, fields = _known(con)
    if not names:
        raise SystemExit("# 索引是空的 —— 先跑 `up index`")
    bases = _basenames(con)
    ignore, optout = _load_ignore(root)
    files = _scan_files(root, args.path)
    if not files:
        raise SystemExit(f"# 沒有要掃的文件（--path '{args.path}' 沒命中）\n"
                         f"# 預設掃：{'、'.join(DEFAULT_ROOTS)}")

    quiet = getattr(args, "quiet", False)
    if quiet:
        # DLL 白名單對齊的 ⚠ 提示在 --quiet 下也吞掉：pre-commit 只該在真的有失效時出聲
        import contextlib
        import io
        with contextlib.redirect_stdout(io.StringIO()):
            dll_names = _dll_names(con, root)
    else:
        dll_names = _dll_names(con, root)
    decl_paths, decl_bases, namespaces = _src_decls(root)
    src_types = set(decl_paths) | namespaces
    has_member, chain_fields = _src_chain(con, root, decl_paths, decl_bases, fields)
    pool = list(names.values())
    ctx = (names, fields, bases, ignore, optout, dll_names, src_types, has_member, chain_fields,
           pool, files)
    bad, noguid, fixes, nonser, checked, weak = _scan(args, root, con, ctx)

    if getattr(args, "fix", False) and fixes:
        n = sum(_apply_fixes(root, rel, ed) for rel, ed in fixes.items())
        kinds = {}
        for ed in fixes.values():
            for _, _, (old, new, _g) in ed:
                k = "換路徑" if (old and new) else "補／改 guid"
                kinds[k] = kinds.get(k, 0) + 1
        print(f"# --fix 改了 {len(fixes)} 份文件、{n} 處（"
              + "、".join(f"{k} {v}" for k, v in kinds.items()) + "）")
        bad, noguid, fixes, nonser, checked, weak = _scan(args, root, con, ctx)

    total = sum(len(v) for v in bad.values())
    missing = sum(len(v) for v in noguid.values())
    if args.baseline:
        toks = sorted({t for v in bad.values() for (_, k, t, _) in v if k not in PATH_KINDS})
        paths_left = sum(1 for v in bad.values() for (_, k, _, _) in v if k in PATH_KINDS)
        p = os.path.join(root, IGNORE_FILE)
        if toks:
            # 沒東西就不寫空標題（已在檔裡的 token 上面就被濾掉了，不會重複）
            with open(p, "a", encoding="utf-8") as fh:
                fh.write(f"\n# baseline：{len(toks)} 個既有誤判（up verify-skills --baseline）\n")
                fh.write("\n".join(toks) + "\n")
        print(f"# 寫進 {IGNORE_FILE}：{len(toks)} 個 token（路徑類不寫入，那些是真失效）")
        if paths_left:
            print(f"# ⚠ 還有 {paths_left} 個路徑失效沒進 baseline —— 去修文件（改名 / guid 類的 `--fix` 會換）；"
                  f"真的是在講「這個檔已經不在」就手動把那條路徑加進 {IGNORE_FILE}")
        if missing:
            print(f"# ⚠ 還有 {missing} 個 asset 路徑沒附 guid —— `up verify-skills --fix` 補")
        print("# 之後 `up verify-skills` 只會看到新漂移；誤加的自己從檔案刪掉")
        return

    if quiet and not total and not missing:
        return
    tail = f"，另有 {weak} 個查不到但沒有相近型別的（--loose 看）" if weak else ""
    print(f"# 掃 {len(files)} 份文件、{checked} 個引用，{total} 個失效{tail}")
    if nonser and not quiet:
        # 不算失效：成員還在，只是 up fields 看不到（寫 skill 時要知道 prefab 上改不到它）
        print(f"# 另有 {len(nonser)} 個是原始碼有、但非 serialized 的欄位（不算失效"
              f"{'' if args.loose else '，--loose 列出'}）")
        if args.loose:
            for rel, line, tok in nonser:
                print(f"  {rel}:{line}  非 serialized  {tok}")
    if not total and not missing:
        print("# 乾淨。語意層的過期用 `up verify-skills --changed` 挑要重讀的段落")
        return
    shown = 0
    for rel in sorted(bad, key=lambda r: -len(bad[r])):
        print(f"\n{rel}")
        for line, kind, tok, hint in bad[rel][: args.limit]:
            print(f"  :{line}  {kind}  {tok}{hint}")
            shown += 1
        if len(bad[rel]) > args.limit:
            print(f"  … 還有 {len(bad[rel]) - args.limit} 個（加 -n）")
    if missing:
        print(f"\n# {missing} 個 asset 路徑沒附 guid（skill 寫 asset 一律 `路徑` (guid:<32 hex>)，"
              f"改名時工具才找得回來）")
        for rel in sorted(noguid, key=lambda r: -len(noguid[r])):
            print(f"{rel}")
            for line, _, tok, hint in noguid[rel][: args.limit]:
                print(f"  :{line}  缺 guid  {tok}{hint}")
            if len(noguid[rel]) > args.limit:
                print(f"  … 還有 {len(noguid[rel]) - args.limit} 個（加 -n）")
        print("# 補法：`up verify-skills --fix` 自動補（只補在反引號後面，不動路徑）；"
              "手動就 `up guid '<路徑>'` 查出來，接在反引號後寫 ` (guid:<hex>)`")
    fixable = sum(1 for v in fixes.values() for _, _, (o, n, _g) in v if o and n)
    if fixable:
        print(f"\n# 其中 {fixable} 個改名 / 不一致可以 `up verify-skills --fix` 直接換成 guid 指的現在路徑")
    if shown and not os.path.exists(os.path.join(root, IGNORE_FILE)):
        print(f"\n# 第一次跑通常有既有雜訊：確認過就 `up verify-skills --baseline` "
              f"寫進 {IGNORE_FILE}，之後只顯示新漂移")
    elif shown:
        print(f"\n# 修法：路徑照「改名成／實際在」改（有 guid 的 `--fix` 會換）；型別／欄位用 `up types` / `up fields` 查現況改寫；"
              f"確定是誤判（Unity API、prefab 節點名、協定欄位）才加進 {IGNORE_FILE}")
    # 非 0 才掛得進 pre-commit / CI（`up vs --quiet || exit 1`）
    raise SystemExit(1)


def _git(cwd: str, *a: str) -> str:
    return subprocess.run(["git", "-C", cwd, *a], capture_output=True, text=True,
                          timeout=60, check=True).stdout


def _submodules(root: str) -> list[str]:
    try:
        out = _git(root, "config", "-f", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$")
    except Exception:  # noqa: BLE001
        return []
    return [ln.split(None, 1)[1].strip() for ln in out.splitlines() if " " in ln]


def _changed_cs(root: str, spec: str) -> list[str]:
    """spec 有 `..` 就當 ref 區間（`origin/main..HEAD`，PR-scoped），否則當 git `--since`。

    MonoFSM / MonoFSM-Pro / MonoFSM-Photon-Fusion 都是 submodule，只在主 repo 跑 git log
    看不到框架 .cs 的改動（偏偏 MonoFSM skill 引用的全是那邊）：
    - `--since`：每個 submodule 各自 log 一次
    - ref 區間：主 repo 兩端記錄的 submodule commit 各取出來，在 submodule 裡 diff
      （submodule 沒 fetch 到那個 commit 就印一行提示跳過，不讓整支掛掉）"""
    out: list[str] = []
    is_range = ".." in spec
    if is_range:
        out += _git(root, "diff", "--name-only", spec, "--", "*.cs").split("\n")
        a, b = spec.split("...", 1) if "..." in spec else spec.split("..", 1)
        a, b = a or "HEAD", b or "HEAD"
        for sub in _submodules(root):
            try:
                sa = _git(root, "rev-parse", f"{a}:{sub}").strip()
                sb = _git(root, "rev-parse", f"{b}:{sub}").strip()
            except Exception:  # noqa: BLE001
                continue  # 該區間兩端有一端還沒有這個 submodule
            if sa == sb:
                continue
            try:
                out += [f"{sub}/{p}" for p in
                        _git(os.path.join(root, sub), "diff", "--name-only", sa, sb, "--", "*.cs").split("\n")]
            except Exception:  # noqa: BLE001
                print(f"# ⚠ {sub} 裡找不到 {sa[:9]}..{sb[:9]}（沒 fetch？），這段框架改動沒算進來")
    else:
        for cwd in [root] + [os.path.join(root, s) for s in _submodules(root)]:
            if not os.path.exists(os.path.join(cwd, ".git")):
                continue
            out += _git(cwd, "log", "--name-only", "--pretty=format:",
                        f"--since={spec}", "--", "*.cs").split("\n")
    return [p for p in out if p.endswith(".cs")]


def _changed(args, root):
    """diff-driven：近期動過的 .cs → 哪些 skill 段落提到它。這是每週該跑的那一支。"""
    since = args.changed or "1.week"
    try:
        changed = _changed_cs(root, since)
    except Exception as e:
        raise SystemExit(f"# git 查不到改動：{e}")
    stems = sorted({os.path.splitext(os.path.basename(p))[0] for p in changed})
    if not stems:
        print(f"# {since} 內沒有 .cs 改動 —— skill 不會因為程式碼而過期")
        return
    files = _scan_files(root, args.path)
    hits: dict[str, list[tuple]] = {}
    for rel in files:
        with open(os.path.join(root, rel), encoding="utf-8") as fh:
            lines = fh.read().splitlines()
        for i, ln in enumerate(lines, 1):
            for s in stems:
                if len(s) >= 5 and s in ln:
                    hits.setdefault(rel, []).append((i, s))
                    break
    print(f"# {since} 內動過 {len(stems)} 支 .cs；"
          f"{len(hits)} 份文件提到它們，共 {sum(len(v) for v in hits.values())} 處")
    if not hits:
        print("# 沒有文件提到這批改動 —— 不用重讀")
        return
    for rel in sorted(hits, key=lambda r: -len(hits[r])):
        rows = hits[rel][: args.limit]
        types = sorted({s for _, s in rows})
        print(f"\n{rel}  （{len(hits[rel])} 處）")
        print(f"  行：{', '.join(str(i) for i, _ in rows)}")
        print(f"  涉及：{'、'.join(types[:8])}")
    print("\n# 只重讀上面這些行附近的段落，不要整份重掃")
