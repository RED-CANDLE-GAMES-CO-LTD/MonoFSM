"""C# 型別目錄：class → 用途說明 + serialized 欄位。

組 FSM 時最花 token 的不是改 prefab，是「先搞清楚有哪些 Action / Condition
可以用、每個欄位要填什麼」——沒有目錄就只能一個個 Read .cs。這張表把那份
資訊離線抽出來（純字串比對，不需要 Unity），讓 `up catalog` 一次回完。

Unity 規定 MonoBehaviour 的 class name 等於檔名，所以只認「宣告名 == 檔名
stem」的那個 class，巢狀型別與同檔的輔助 class 一律略過。
"""

from __future__ import annotations

import html
import json
import os
import re

SKIP_DIRS = {".git", "Library", "Temp", "Logs", "obj", "Build", "Builds"}

# 這些 root base 決定 kind；子類會沿繼承鏈遞移解析。
# 順序有意義：一個 class 沿它自己的 base 清單由左到右找，第一個命中的就是 kind。
KIND_ROOTS = {
    "AbstractStateAction": "action",
    "AbstractConditionBehaviour": "condition",
    "AbstractRenderBehaviour": "render",       # 每幀畫面表現，不改狀態
    "AbstractEventHandler": "handler",         # 收事件並分派給 action
    "AbstractGetter": "getter",                # 提供數值給欄位引用（含 AbstractValueSource）
    "AbstractMonoVariable": "var",
    "ScriptableObject": "so",
}

_DECL_RE_CACHE: dict[str, re.Pattern] = {}

# `[SerializeField] private Foo _bar;` / `public Foo bar;`（含泛型與陣列）
FIELD_RE = re.compile(
    r"^[ \t]*(?P<attrs>(?:\[[^\]\n]*\][ \t\r\n]*)*)"
    r"(?P<mods>(?:public|private|protected|internal|readonly|static|new)[ \t]+)*"
    r"(?P<type>[\w.]+(?:\s*<[^;=<>]*>)?(?:\[\])?)[ \t]+"
    r"(?P<name>[A-Za-z_]\w*)[ \t]*(?P<tail>[;={])",
    re.M,
)
ATTR_RE = re.compile(r"\[([A-Za-z_]\w*)")
TOOLTIP_RE = re.compile(r'\[Tooltip\("((?:[^"\\]|\\.)*)"')
SUMMARY_LINE_RE = re.compile(r"^\s*///\s?(.*)$")
COMMENT_LINE_RE = re.compile(r"^\s*//+\s?(.*)$")

# 不算 serialized 欄位的修飾字 / 型別關鍵字
NON_FIELD_TYPES = {
    "return", "new", "class", "struct", "enum", "interface", "using", "namespace",
    "if", "else", "for", "foreach", "while", "switch", "case", "get", "set",
    "var", "void", "override", "virtual", "abstract", "partial", "sealed",
    "const", "event", "delegate", "operator", "this", "base", "yield", "await",
}
# 這些 attribute 代表欄位由框架自動填，不用人工在 prefab 上指定
AUTO_ATTRS = {"Auto", "AutoParent", "AutoChildren", "AutoConnect", "AutoAttach"}


def _decl_re(stem: str) -> re.Pattern:
    r = _DECL_RE_CACHE.get(stem)
    if r is None:
        r = re.compile(
            r"^[ \t]*(?:\[[^\]]*\][ \t]*)*"
            r"(?:public|internal|abstract|sealed|partial|static|\s)*"
            r"class\s+" + re.escape(stem) + r"\b(?P<generic><[^{:]*>)?"
            r"(?P<bases>\s*:[^{]*)?",
            re.M,
        )
        _DECL_RE_CACHE[stem] = r
    return r


def _strip_comment_markup(text: str) -> str:
    text = re.sub(r"</?(summary|para|remarks|c|code)>", " ", text)
    text = re.sub(r'<see\s+cref="[^"]*?([\w.]+)"\s*/>', r"\1", text)
    text = re.sub(r"\s+", " ", text)
    return html.unescape(text).strip()


ATTR_LINE_RE = re.compile(r"^\s*\[")


def _skip_attrs_up(lines: list[str], idx: int) -> int:
    r"""從 class 宣告往上跳過 attribute 行與空行，回傳註解區的結束行 +1。

    宣告的 regex 會把上方的 `[Attr]` 行一起吃進 match（`\s*` 跨行），所以
    idx 不一定是 `public class` 那行；而 attribute 行夾在中間也會把
    `/// <summary>` 擋在更上面 —— 兩件事都要先跳過 attribute 才判斷得準。
    """
    i = idx
    while i > 0 and ATTR_LINE_RE.match(lines[i - 1]):
        i -= 1
    return i


def _obsolete_above(lines: list[str], start: int, idx: int) -> bool:
    """宣告上方的 attribute 區裡有沒有 [Obsolete]。

    挑 component 時最糟的情況是挑到已棄用的（VariableProviderRef 那一整批），
    所以這件事要在目錄裡就看得到，而不是組完 FSM 才發現。
    """
    return any("Obsolete" in lines[i] for i in range(start, idx + 1)
               if ATTR_LINE_RE.match(lines[i]))


def _doc_above(lines: list[str], idx: int) -> tuple[str, bool]:
    """讀宣告行上方的註解，回 (一行摘要, 是否為正式 XML doc)。

    優先吃 `/// <summary>`；沒有就退而用連續的 `//` 註解——專案裡不少說明
    是用 `//` 寫的，雖然不算正式文件，但拿來判斷用途已經夠。
    """
    doc: list[str] = []
    i = idx - 1
    while i >= 0:
        m = SUMMARY_LINE_RE.match(lines[i])
        if not m:
            break
        doc.append(m.group(1))
        i -= 1
    if doc:
        text = _strip_comment_markup(" ".join(reversed(doc)))
        if text:
            return text, True

    plain: list[str] = []
    i = idx - 1
    while i >= 0:
        m = COMMENT_LINE_RE.match(lines[i])
        if not m or lines[i].lstrip().startswith("///"):
            break
        plain.append(m.group(1))
        i -= 1
    if plain:
        text = _strip_comment_markup(" ".join(reversed(plain)))
        # 待辦與「被註解掉的程式碼 / attribute」都不是說明
        if (text and not re.match(r"^(FIXME|TODO|NOTE|HACK|XXX)\b", text, re.I)
                and not text.startswith("[") and not text.startswith("using ")):
            return text, False
    return "", False


def _parse_bases(raw: str | None) -> list[str]:
    if not raw:
        return []
    raw = raw.lstrip().lstrip(":")
    raw = re.sub(r"//.*$", "", raw, flags=re.M)
    raw = re.sub(r"\bwhere\b.*$", "", raw, flags=re.S)
    out = []
    depth = 0
    cur = ""
    for ch in raw:
        if ch in "<([":
            depth += 1
        elif ch in ">)]":
            depth -= 1
        if ch == "," and depth == 0:
            out.append(cur)
            cur = ""
        else:
            cur += ch
    out.append(cur)
    names = []
    for part in out:
        part = part.strip().split("<")[0].strip()
        if part and re.match(r"^[\w.]+$", part):
            names.append(part.rsplit(".", 1)[-1])
    return names


def _parse_fields(body: str) -> list[dict]:
    """抽 serialized 欄位。只收 `[SerializeField]` 私有欄位與 public 欄位。"""
    fields = []
    for m in FIELD_RE.finditer(body):
        if m.group("tail") == "{":
            continue  # property / 方法
        typ = m.group("type").strip()
        if typ.split("<")[0].split("[")[0] in NON_FIELD_TYPES:
            continue
        attrs_raw = m.group("attrs") or ""
        mods = (m.group("mods") or "").strip()
        attrs = set(ATTR_RE.findall(attrs_raw))
        is_public = mods.startswith("public")
        if not is_public and "SerializeField" not in attrs:
            continue
        if "NonSerialized" in attrs or "static" in mods or "const" in mods:
            continue
        tip = TOOLTIP_RE.search(attrs_raw)
        auto = sorted(attrs & AUTO_ATTRS)
        fields.append({
            "name": m.group("name"),
            "type": re.sub(r"\s+", "", typ),
            "auto": auto[0] if auto else "",
            "tip": _strip_comment_markup(tip.group(1)) if tip else "",
        })
    return fields


def _class_body(text: str, start: int) -> str:
    """從宣告位置取整個 class body（配對大括號；找不到就退回整段尾巴）。"""
    open_i = text.find("{", start)
    if open_i < 0:
        return ""
    depth = 0
    for i in range(open_i, len(text)):
        c = text[i]
        if c == "{":
            depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                return text[open_i + 1:i]
    return text[open_i + 1:]


ANY_DECL_RE = re.compile(
    r"^[ \t]*(?:public|internal|abstract|sealed|partial|static|\s)*"
    # 泛型參數與 base list 之間常常換行：`class Foo<T>\n    : Bar,`
    r"class\s+(?P<name>\w+)\s*(?:<[^{:]*>)?\s*(?P<bases>:[^{]*)?",
    re.M,
)


def parse_bases_of_all(text: str) -> dict[str, list[str]]:
    """檔案裡每一個 class 宣告的 base 清單。

    kind 靠繼承鏈遞移，而鏈上的中繼 class 不一定自己一個檔案
    （例：AbstractGetter 寫在 AbstractValueSource.cs 裡），只認檔名 stem
    的話鏈會斷在那裡，底下幾十個 class 就全部歸不了類。
    """
    out = {}
    for m in ANY_DECL_RE.finditer(text):
        out.setdefault(m.group("name"), _parse_bases(m.group("bases")))
    return out


def parse_file(path: str, text: str) -> dict | None:
    stem = os.path.basename(path)[: -len(".cs")]
    m = _decl_re(stem).search(text)
    if not m:
        return None
    all_lines = text.split("\n")
    idx = len(text[: m.start()].split("\n")) - 1
    start = _skip_attrs_up(all_lines, idx)
    summary, is_doc = _doc_above(all_lines, start)
    body = _class_body(text, m.end())
    return {
        "class": stem,
        "obsolete": _obsolete_above(all_lines, start, idx),
        "bases": _parse_bases(m.group("bases")),
        "abstract": bool(re.search(r"\babstract\b", m.group(0))),
        "summary": summary,
        "has_doc": is_doc,
        "fields": _parse_fields(body),
    }


def iter_cs_paths(root: str):
    """全庫 .cs 的相對路徑（跳過 Library / Temp / `~` 結尾等目錄）。"""
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS and not d.endswith("~")]
        for fn in filenames:
            if fn.endswith(".cs") and not fn.endswith(".g.cs"):
                yield os.path.relpath(os.path.join(dirpath, fn), root)


def parse_one(root: str, rel: str):
    """讀單一 .cs：回傳 (info | None, 該檔所有 class 的 base 表)。"""
    try:
        text = open(os.path.join(root, rel), encoding="utf-8", errors="replace").read()
    except OSError:
        return None, {}
    if "class " not in text:
        return None, {}
    return parse_file(rel, text), parse_bases_of_all(text)


def scan(root: str, paths: list[str] | None = None):
    """掃 .cs 產出 catalog 列。paths 為 None 時掃全庫。"""
    if paths is None:
        paths = list(iter_cs_paths(root))
    for rel in paths:
        info, bases = parse_one(root, rel)
        if not bases and info is None:
            continue
        yield info, rel, bases


def resolve_kinds(rows: dict[str, dict], all_bases: dict[str, list[str]]) -> None:
    """沿繼承鏈把 kind 遞移下去（就地寫進每一列的 'kind'）。

    all_bases 涵蓋全庫每一個 class 宣告（不只有進 catalog 的那些），
    中繼 class 才不會讓鏈斷掉。
    """
    memo: dict[str, str] = {}

    def kind_of(cls: str, depth=0) -> str:
        if cls in KIND_ROOTS:
            return KIND_ROOTS[cls]
        if cls in memo:
            return memo[cls]
        bases = all_bases.get(cls)
        if depth > 12 or bases is None:
            return ""
        memo[cls] = ""  # 先佔位擋循環繼承
        k = ""
        for b in bases:
            k = kind_of(b, depth + 1)
            if k:
                break
        memo[cls] = k
        return k

    for cls, row in rows.items():
        row["kind"] = kind_of(cls)


def resolve_obsolete(rows: dict[str, dict], all_bases: dict[str, list[str]]) -> None:
    """base 標了 [Obsolete]，子類實際上也不該再用 —— 沿鏈遞移。"""
    flagged = {c for c, r in rows.items() if r.get("obsolete")}
    memo: dict[str, bool] = {}

    def is_obs(cls: str, depth=0) -> bool:
        if cls in flagged:
            return True
        if cls in memo:
            return memo[cls]
        bases = all_bases.get(cls)
        if depth > 12 or bases is None:
            return False
        memo[cls] = False
        r = any(is_obs(b, depth + 1) for b in bases)
        memo[cls] = r
        return r

    for cls, row in rows.items():
        row["obsolete"] = is_obs(cls)


def rows_to_tuples(rows: dict[str, dict], all_bases: dict[str, list[str]]) -> list[tuple]:
    """解 kind / obsolete 繼承鏈後，轉成可直接寫進 catalog 表的 tuple 列。

    `self_obsolete` 另外存一欄：resolve_obsolete 會把 base 的 [Obsolete] 遞移給子類，
    若增量重建時拿「遞移後的值」當種子，base 拿掉標記後子類會永遠清不掉。
    """
    for r in rows.values():
        r["self_obsolete"] = r.get("self_obsolete", r.get("obsolete", False))
        r["obsolete"] = r["self_obsolete"]
    resolve_kinds(rows, all_bases)
    resolve_obsolete(rows, all_bases)
    out = []
    for cls, r in rows.items():
        out.append((
            cls,
            r["path"],
            r["kind"],
            ",".join(r["bases"][:3]),
            1 if r["abstract"] else 0,
            1 if r["obsolete"] else 0,
            r["summary"],
            1 if r["has_doc"] else 0,
            json.dumps(r["fields"], ensure_ascii=False),
            1 if r["self_obsolete"] else 0,
        ))
    return out


def build_rows(root: str, per_file_bases: dict | None = None) -> list[tuple]:
    """全庫掃描版（`up index --rebuild` 用）。

    傳入 per_file_bases 時順便把「每支檔案宣告了哪些 class、各自的 base」帶出來，
    給增量索引存進 cs_files —— 否則要再掃一次全庫才拿得到，等於付兩次 parse 錢。
    """
    rows: dict[str, dict] = {}
    all_bases: dict[str, list[str]] = {}
    for info, rel, bases in scan(root):
        if per_file_bases is not None:
            per_file_bases[rel] = bases
        for name, bs in bases.items():
            if bs or name not in all_bases:
                all_bases[name] = bs
        if not info:
            continue
        info["path"] = rel
        rows[info["class"]] = info
    return rows_to_tuples(rows, all_bases)
