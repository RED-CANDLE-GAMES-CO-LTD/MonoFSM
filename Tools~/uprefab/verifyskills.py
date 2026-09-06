"""`up verify-skills` —— 機械檢查 skill / agent / CLAUDE.md 裡的引用有沒有爛掉。

skill 記的是「現況快照」，必然 decay；Progress.md 記的是「當時為什麼」，是歷史事實、
永不 decay。所以要維護的只有前者。這支只做**機械可驗**的那一半：

  1. 路徑：文件裡提到的 `Assets/…prefab` / `.cs` 等是不是還在
  2. 型別：backtick 裡的 PascalCase 是不是還存在於離線索引
  3. 欄位：`Type._field` 的欄位是不是還在該型別上

語意過期（「這個 pattern 已經不建議用了」）機械查不到，那要靠 `--changed`：
用 git 找出近期動過的 .cs，反查哪些 skill 段落提到它們 —— 給人「這幾段要重讀」的
清單，而不是全域重掃 145KB。

誤判控制走 linter 慣例：第一次跑完用 `--baseline` 把現有雜訊寫進 .uprefab-skillignore，
之後只會看到新漂移。
"""

from __future__ import annotations

import difflib
import json
import os
import re
import subprocess

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
}


def _load_ignore(root: str) -> set[str]:
    p = os.path.join(root, IGNORE_FILE)
    out = set(BUILTIN_IGNORE)
    if os.path.exists(p):
        with open(p, encoding="utf-8") as fh:
            for ln in fh:
                ln = ln.split("#", 1)[0].strip()
                if ln:
                    out.add(ln)
    return out


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
        out.setdefault(os.path.basename(path), []).append(path)
    for (path,) in con.execute("SELECT path FROM scripts WHERE path IS NOT NULL"):
        out.setdefault(os.path.basename(path), []).append(path)
    return out


def _path_check(root: str, here: str, tok: str, bases: dict) -> tuple[bool, str]:
    """(過不過, 提示)。三關：repo 根相對 → 文件自身資料夾相對 → 檔名反查索引。

    最後一關同時解掉兩件事：文件裡的簡寫路徑不該被誤報，以及資產真的搬家時
    要直接把新路徑講出來，而不是只說「不存在」讓下一隻 agent 自己去找。
    """
    t = tok.rstrip("/")
    if os.path.exists(os.path.join(root, t)) or os.path.exists(os.path.join(here, t)):
        return True, ""
    base = os.path.basename(t)
    if not base.endswith(ASSET_EXTS + (".cs",)):
        # 沒有副檔名又接不到實體路徑的，多半是 prefab 內的節點路徑（`Modules/`、
        # `CharacterModules/Character FSM/`），不是檔案，不歸這支管
        return True, ""
    hit = bases.get(base)
    if hit:
        return (True, "") if len(hit) > 1 or t.endswith(hit[0]) else \
               (False, f"（實際在：{hit[0]}）")
    near = difflib.get_close_matches(base, list(bases), n=1, cutoff=0.8)
    return False, (f"（相近檔名：{near[0]}）" if near else "")


def _near(tok: str, pool, n=2) -> list[str]:
    return difflib.get_close_matches(tok, pool, n=n, cutoff=NEAR_SUGGEST)


def _hint(hits: list[str]) -> str:
    return f"（相近：{'、'.join(hits)}）" if hits else ""


def _candidates(text: str):
    """產出 (行號, kind, token)。kind = path / type / member。"""
    for i, ln in enumerate(text.splitlines(), 1):
        if ln.lstrip().startswith("```"):
            continue
        for tok in _TICK_RE.findall(ln):
            tok = tok.strip().rstrip(",.;:）)")
            if not tok:
                continue
            if "/" in tok and any(c in tok for c in "<>*?"):
                continue  # `Packages/<package-id>/`、`Module Test/*.unity` 是佔位／glob
            if "/" in tok and (tok.endswith(PATH_EXTS) or tok.endswith("/")):
                yield i, "path", (tok, tok)
            elif _MEMBER_RE.match(tok):
                yield i, "member", (tok, tok)
            else:
                m = _TYPE_RE.match(tok)
                if m:
                    # 查表用裸名，回報用原樣 —— `Foo<T>` 的 T 不該影響「型別在不在」
                    yield i, "type", (m.group(1), tok)


def cmd(args, root, cfg):
    import indexer

    if args.changed is not None:
        return _changed(args, root)

    con = indexer.connect(root)
    names, fields = _known(con)
    if not names:
        raise SystemExit("# 索引是空的 —— 先跑 `up index`")
    bases = _basenames(con)
    ignore = _load_ignore(root)
    files = _scan_files(root, args.path)
    if not files:
        raise SystemExit(f"# 沒有要掃的文件（--path '{args.path}' 沒命中）\n"
                         f"# 預設掃：{'、'.join(DEFAULT_ROOTS)}")

    pool = list(names.values())
    bad: dict[str, list[tuple]] = {}
    checked = 0
    weak = 0   # 查不到又沒有相近型別的 —— 多半根本不是專案型別，--loose 才列
    for rel in files:
        with open(os.path.join(root, rel), encoding="utf-8") as fh:
            text = fh.read()
        here = os.path.dirname(os.path.join(root, rel))
        for line, kind, (tok, shown) in _candidates(text):
            if tok in ignore or shown in ignore:
                continue
            if kind == "path":
                checked += 1
                ok, why = _path_check(root, here, tok, bases)
                if not ok:
                    bad.setdefault(rel, []).append((line, "路徑不存在", shown, why))
            elif kind == "member":
                cls, fld = _MEMBER_RE.match(tok).group(1, 2)
                if cls.lower() not in names or cls in ignore:
                    continue
                fs = fields.get(cls.lower())
                if not fs or not fld.startswith("_"):
                    continue  # 只驗序列化欄位（專案慣例以底線開頭），屬性/方法放過
                checked += 1
                if fld not in fs:
                    bad.setdefault(rel, []).append(
                        (line, "欄位不存在", shown, _hint(_near(fld, fs))))
            else:
                # 嚴格門檻：至少兩個大寫且夠長，才當它是型別名而不是普通英文字
                if len(tok) < 5 or sum(c.isupper() for c in tok) < 2:
                    continue
                checked += 1
                if tok.lower() in names:
                    continue
                hits = _near(tok, pool)
                strong = hits and difflib.SequenceMatcher(
                    None, tok.lower(), hits[0].lower()).ratio() >= NEAR_REPORT
                if not (strong or args.loose):
                    weak += 1
                    continue
                bad.setdefault(rel, []).append((line, "型別不存在", shown, _hint(hits)))

    total = sum(len(v) for v in bad.values())
    if args.baseline:
        toks = sorted({t for v in bad.values() for (_, k, t, _) in v if k != "路徑不存在"})
        p = os.path.join(root, IGNORE_FILE)
        with open(p, "a", encoding="utf-8") as fh:
            fh.write(f"\n# baseline：{len(toks)} 個既有誤判（up verify-skills --baseline）\n")
            fh.write("\n".join(toks) + "\n")
        print(f"# 寫進 {IGNORE_FILE}：{len(toks)} 個 token（路徑類不寫入，那些是真失效）")
        print("# 之後 `up verify-skills` 只會看到新漂移；誤加的自己從檔案刪掉")
        return

    tail = f"，另有 {weak} 個查不到但沒有相近型別的（--loose 看）" if weak else ""
    print(f"# 掃 {len(files)} 份文件、{checked} 個引用，{total} 個失效{tail}")
    if not total:
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
    if shown and not os.path.exists(os.path.join(root, IGNORE_FILE)):
        print(f"\n# 第一次跑通常有既有雜訊：確認過就 `up verify-skills --baseline` "
              f"寫進 {IGNORE_FILE}，之後只顯示新漂移")


def _changed(args, root):
    """diff-driven：近期動過的 .cs → 哪些 skill 段落提到它。這是每週該跑的那一支。"""
    since = args.changed or "1.week"
    try:
        out = subprocess.run(
            ["git", "-C", root, "log", "--name-only", "--pretty=format:",
             f"--since={since}", "--", "*.cs"],
            capture_output=True, text=True, timeout=60, check=True).stdout
    except Exception as e:
        raise SystemExit(f"# git 查不到改動：{e}")
    stems = sorted({os.path.splitext(os.path.basename(p))[0]
                    for p in out.split() if p.endswith(".cs")})
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
