"""`up mat` / `up controller` / `up anim --values` 共用：離線把 Unity YAML document 讀成樹、批次解 guid。

為什麼不用 uyaml.scan 就好：scan 只切 document、給頂層欄位；材質屬性、controller 的
layer / transition、anim 的 keyframe 都是好幾層巢狀，需要真的讀成 dict / list。
這些檔案都很小（.mat 幾 KB、controller 幾十 KB、.anim 最多幾 MB），整份讀進來沒問題 ——
**不要拿這支去讀 scene**（那是 uyaml.scan 的工作）。

只支援 Unity 會寫出來的子集：區塊 mapping、`- ` list（跟父 key 同縮排）、單行 flow
`{a: b, c: d}` / `[]`、雙引號（\\u 逸出）與單引號字串、長字串折行。
"""

from __future__ import annotations

import json
import os
import re

import uyaml

FLOW_ITEM = re.compile(r"\s*([^:,{}]+):\s*([^,{}]*)\s*(?:,|$)")


def scalar(raw: str) -> str:
    raw = raw.strip()
    if len(raw) >= 2 and raw[0] == '"' and raw[-1] == '"':
        try:
            return json.loads(raw)
        except ValueError:
            return raw[1:-1]
    if len(raw) >= 2 and raw[0] == "'" and raw[-1] == "'":
        return raw[1:-1].replace("''", "'")
    return raw


def _value(raw: str):
    raw = raw.strip()
    if raw.startswith("{") and raw.endswith("}"):
        inner = raw[1:-1]
        return {k.strip(): scalar(v) for k, v in FLOW_ITEM.findall(inner)}
    if raw == "[]":
        return []
    return scalar(raw)


def _indent(ln: str) -> int:
    return len(ln) - len(ln.lstrip(" "))


def _split_key(text: str):
    """`key: value` → (key, value)；不是 key 行回 None。value 可能是空字串。"""
    if text.startswith(("{", '"', "'")):
        return None
    i = text.find(":")
    if i <= 0:
        return None
    if i + 1 < len(text) and text[i + 1] != " ":
        return None
    return text[:i], text[i + 1:].strip()


def _parse_block(lines: list[str], i: int, indent: int):
    """從 lines[i] 開始讀一個縮排 = indent 的區塊，回 (obj, 下一行 index)。"""
    if i < len(lines) and _indent(lines[i]) == indent and lines[i].lstrip().startswith("- "):
        out = []
        while i < len(lines) and _indent(lines[i]) == indent and lines[i].lstrip().startswith("- "):
            first = lines[i][indent + 2:]
            kv = _split_key(first)
            if kv is None:
                out.append(_value(first))
                i += 1
                continue
            # list item 是 mapping：把 "- " 換成兩格空白，當成縮排 indent+2 的 mapping 讀
            lines[i] = " " * (indent + 2) + first
            obj, i = _parse_block(lines, i, indent + 2)
            out.append(obj)
        return out, i
    out: dict = {}
    while i < len(lines):
        ln = lines[i]
        if not ln.strip():
            i += 1
            continue
        ind = _indent(ln)
        if ind < indent or (ind == indent and ln.lstrip().startswith("- ")):
            break
        if ind > indent:  # 前一個值的折行（長字串）
            i += 1
            continue
        kv = _split_key(ln[indent:])
        if kv is None:
            i += 1
            continue
        key, val = kv
        i += 1
        if val:
            # 長字串會折到下一行、縮排更深、沒有 key
            while i < len(lines) and lines[i].strip() and _indent(lines[i]) > indent \
                    and _split_key(lines[i].strip()) is None and not lines[i].lstrip().startswith("- "):
                val += " " + lines[i].strip()
                i += 1
            out[key] = _value(val)
            continue
        # 空值：子區塊不是更深縮排的 mapping，就是同縮排的 list
        if i < len(lines) and lines[i].strip():
            nxt = _indent(lines[i])
            if nxt == indent and lines[i].lstrip().startswith("- "):
                out[key], i = _parse_block(lines, i, indent)
                continue
            if nxt > indent:
                out[key], i = _parse_block(lines, i, nxt)
                continue
        out[key] = ""
    return out, i


class Document:
    __slots__ = ("class_id", "file_id", "type_name", "body")

    def __init__(self, class_id, file_id, type_name, body):
        self.class_id, self.file_id, self.type_name, self.body = class_id, file_id, type_name, body


def load(path: str) -> list[Document]:
    docs = []
    for d in uyaml.scan(path):
        lines = list(d.lines)
        body, _ = _parse_block(lines, 0, 2) if lines else ({}, 0)
        docs.append(Document(d.class_id, d.file_id, d.type_name, body if isinstance(body, dict) else {}))
    return docs


def ref(v) -> tuple[int, str | None]:
    """flow ref dict → (fileID, guid)；不是 ref 回 (0, None)。"""
    if not isinstance(v, dict):
        return 0, None
    try:
        fid = int(v.get("fileID", 0) or 0)
    except ValueError:
        fid = 0
    return fid, v.get("guid")


def num(v, default=0.0) -> float:
    try:
        return float(v)
    except (TypeError, ValueError):
        return default


def fmt_num(x: float) -> str:
    if x == int(x) and abs(x) < 1e9:
        return str(int(x))
    s = f"{x:.4f}".rstrip("0").rstrip(".")
    return s if s not in ("-0", "") else "0"


# ---- guid → path（一次掃完，不要一個 guid 掃一次 .meta） ----

BUILTIN_GUIDS = {
    "0000000000000000f000000000000000": "Resources/unity_builtin_extra",
    "0000000000000000e000000000000000": "Library/unity default resources",
}
SKIP_DIRS = {"Library", "Temp", "Obj", "obj", "Build", "Builds", ".git", "node_modules", "Logs",
             "UserSettings", ".uprefab-cache"}


def _scan_meta(top: str, want: set[str], found: dict, root: str, skip: set[str]) -> None:
    if not os.path.isdir(top):
        return
    for d, dirs, files in os.walk(top):
        dirs[:] = [x for x in dirs if x not in skip]
        for f in files:
            if not f.endswith(".meta"):
                continue
            full = os.path.join(d, f)
            try:
                with open(full, encoding="utf-8", errors="ignore") as fh:
                    for _ in range(3):
                        ln = fh.readline()
                        if ln.startswith("guid: "):
                            g = ln[6:].strip()
                            if g in want:
                                found[g] = os.path.relpath(full[:-5], root)
                                want.discard(g)
                            break
            except OSError:
                continue
            if not want:
                return


CACHE_FILE = os.path.join(".uprefab-cache", "guidmap.json")


def _meta_guid(full: str) -> str | None:
    try:
        with open(full + ".meta", encoding="utf-8", errors="ignore") as fh:
            for _ in range(3):
                ln = fh.readline()
                if ln.startswith("guid: "):
                    return ln[6:].strip()
    except OSError:
        return None
    return None


def unity_path(rel: str) -> str:
    """Library/PackageCache/<pkg>@<hash>/x → Packages/<pkg>/x（Unity 認的路徑，也比較好讀）。"""
    parts = rel.replace(os.sep, "/").split("/")
    if len(parts) > 3 and parts[0] == "Library" and parts[1] == "PackageCache":
        return "/".join(["Packages", parts[2].split("@")[0]] + parts[3:])
    return rel


def disk_path(root: str, rel: str) -> str:
    """unity_path 的反向：回可以 open() 的絕對路徑（Packages/<pkg>/x → Library/PackageCache/<pkg>@*/x）。"""
    full = rel if os.path.isabs(rel) else os.path.join(root, rel)
    if os.path.exists(full) or not rel.startswith("Packages/"):
        return full
    parts = rel.split("/", 2)
    cache = os.path.join(root, "Library", "PackageCache")
    if len(parts) == 3 and os.path.isdir(cache):
        for d in os.listdir(cache):
            if d.split("@")[0] == parts[1]:
                alt = os.path.join(cache, d, parts[2])
                if os.path.exists(alt):
                    return alt
    return full


def resolve_guids(root: str, guids) -> dict[str, str]:
    """guid → 路徑。內建 → 索引 → 小快取 → 掃 repo 的 .meta → 掃 Library/PackageCache（URP Lit 這類）。

    索引只收 prefab / scene / SO，shader / 貼圖 / clip 都不在裡面，每次掃 .meta 要 1~3 秒；
    所以掃到的結果記進 `.uprefab-cache/guidmap.json`，命中時用 .meta 的 guid 行驗證（搬家 / 改名就重掃）。
    """
    want = {g for g in guids if g}
    found: dict[str, str] = {}
    for g in list(want):
        if g in BUILTIN_GUIDS:
            found[g] = BUILTIN_GUIDS[g]
            want.discard(g)
    if not want:
        return found
    cache_path = os.path.join(root, CACHE_FILE)
    try:
        with open(cache_path, encoding="utf-8") as fh:
            cache = json.load(fh)
    except (OSError, ValueError):
        cache = {}
    for g in list(want):
        rel = cache.get(g)
        if rel and _meta_guid(os.path.join(root, rel)) == g:
            found[g] = rel
            want.discard(g)
    if not want:
        return {g: unity_path(p) for g, p in found.items()}
    before = set(found)
    try:
        import indexer
        import query
        con = indexer.connect(root)
        for g in list(want):
            row = query.asset_by_guid(con, g)
            if row and os.path.exists(os.path.join(root, row[0])):
                found[g] = row[0]
                want.discard(g)
    except Exception:
        pass
    if want:
        _scan_meta(root, want, found, root, SKIP_DIRS)
    if want:
        _scan_meta(os.path.join(root, "Library", "PackageCache"), want, found, root, {".git"})
    new = {g: p for g, p in found.items() if g not in before and g not in BUILTIN_GUIDS}
    if new:
        cache.update(new)
        try:
            os.makedirs(os.path.dirname(cache_path), exist_ok=True)
            with open(cache_path, "w", encoding="utf-8") as fh:
                json.dump(cache, fh, ensure_ascii=False)
        except OSError:
            pass
    return {g: unity_path(p) for g, p in found.items()}


def fbx_clip_name(root: str, rel: str, file_id: int) -> str | None:
    """模型檔（.fbx）裡的 clip：從 .meta 的 internalIDToNameTable 找 74:<fileID> 的名字。"""
    try:
        with open(disk_path(root, rel) + ".meta", encoding="utf-8", errors="ignore") as fh:
            txt = fh.read()
    except OSError:
        return None
    m = re.search(r"- first:\s*\n\s*74: %d\s*\n\s*second: (.*)" % file_id, txt)
    if not m:  # 舊版 meta：fileIDToRecycleName 底下 `7400000: 名字`
        m = re.search(r"^    %d: (.*)$" % file_id, txt, re.M)
    return scalar(m.group(1)) if m else None


# ---- 檔案定位：錯副檔名 / 找不到時直接說該打什麼 ----

KIND_CMD = {
    ".anim": "up anim <clip.anim> --values",
    ".mat": "up mat <path.mat>",
    ".controller": "up controller <path.controller>",
    ".overridecontroller": "up controller <path.overrideController>",
    ".prefab": "up prefab read <prefab> / up prefab peek",
    ".asset": "up asset fields <asset>",
    ".fbx": "（模型檔：clip 要先 Extract 成 .anim；material remap 看 .fbx.meta）",
}


def find_file(root: str, arg: str, exts: tuple[str, ...], cmd: str, usage: str) -> str:
    """回傳絕對路徑；副檔名不對印「你可能想要」，找不到用檔名片段列候選。"""
    if not arg or not arg.strip():
        raise SystemExit(f"# up {cmd} 少了檔案路徑（收到空字串 —— shell 變數沒展開？）\n{usage}")
    cand = disk_path(root, arg)
    if os.path.isfile(cand):
        ext = os.path.splitext(cand)[1].lower()
        if ext not in exts:
            tip = KIND_CMD.get(ext)
            raise SystemExit(
                f"# up {cmd} 只吃 {' / '.join(exts)}，收到的是 {ext or '（沒有副檔名）'}。"
                + (f"\n# 你可能想要：{tip}" if tip else "") + f"\n{usage}")
        return cand
    key = os.path.basename(arg.rstrip("/")).lower()
    for e in exts:
        if key.endswith(e):
            key = key[: -len(e)]
    hits = []
    for d, dirs, files in os.walk(os.path.join(root, "Assets")):
        for f in files:
            fl = f.lower()
            if fl.endswith(exts) and key in fl:
                hits.append(os.path.relpath(os.path.join(d, f), root))
    if len(hits) == 1:
        print(f"# 找不到 {arg}，改用唯一候選 {hits[0]}")
        return os.path.join(root, hits[0])
    if hits:
        shown = "\n".join(f"#   up {cmd} \"{h}\"" for h in sorted(hits)[:10])
        more = f"\n# …還有 {len(hits) - 10} 筆，給更長的片段" if len(hits) > 10 else ""
        raise SystemExit(f"# 找不到 {arg}；檔名含「{key}」的 {'/'.join(exts)} 有 {len(hits)} 筆：\n{shown}{more}")
    raise SystemExit(f"# 找不到 {arg}，Assets/ 底下也沒有檔名含「{key}」的 {'/'.join(exts)}。\n{usage}")


class Budget:
    """charBudget 摺疊：超過就停印，最後補一行「還有多少沒印、怎麼縮小」。0 = 不限。"""

    def __init__(self, limit: int, hint: str):
        self.limit, self.hint, self.used, self.dropped = limit, hint, 0, 0

    def emit(self, line: str) -> bool:
        # 一超過就整段停印（不讓後面比較短的行插隊，輸出才不會跳著斷）
        if self.dropped or (self.limit and self.used + len(line) + 1 > self.limit):
            self.dropped += 1
            return False
        print(line)
        self.used += len(line) + 1
        return True

    def finish(self) -> None:
        if self.dropped:
            print(f"# …超過 --budget {self.limit}，還有 {self.dropped} 行沒印（{self.hint}；--budget 0 不限）")
