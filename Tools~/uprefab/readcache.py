"""`up prefab read` 的輸出快取。

read 走 Unity 端的 PrefabTextReader.Export，每次都是一趟 execute-dynamic-code
來回、輸出可以到 budget 上限；而同一個 prefab（尤其 variant 的 base）在一次調查裡
常被重複讀。這層以「來源檔案的 mtime」當 key 把結果存到磁碟，第二次讀近乎免費。

**正確性優先於命中率**：任何算不準的情況（guid 解不開、檔案讀不到、解析拋例外）
一律當成 miss 直接走 Unity，絕不回傳可能過時的內容。

兩層命中：
1. `key_for` 完全相同的參數 → 直接回上次的輸出。
2. `slice_for`（本地切片）：要 `--node A/B/C`，但快取裡已經有 A 或 A/B 的完整子樹
   → 在本地裁出 C 的段落，不打 Unity。**只裁 node 前綴**；帶 `--depth` / `--fsm`
   的一律回 Unity（見下面 SLICE 的註解）。
"""

from __future__ import annotations

import glob
import hashlib
import json
import os
import re

import indexer
import query

CACHE_DIR = os.path.join(".uprefab-cache", "read")
GUID_RE = re.compile(r"guid: ([0-9a-f]{32})")

# 手動 bump 的版本號。readcache.py 自己**不在** TOOL_FILES 裡（理由見 PROGRESS
# 「readcache 預設開啟 + 收窄 TOOL_FILES + argv memo」章節），所以改本檔的切片/key
# 邏輯不會自動失效既有 .txt —— 切片改壞時毒掉的檔案會在修好後繼續命中。
# **動到 _slice / key 組成就要把這個號碼 +1。**
CACHE_FORMAT_VERSION = "4"

# 只納入「真正決定輸出格式」的 C#。刻意不含 uprefab.py / readcache.py：
# 那兩支開發期天天存檔，而它們多半改的是別的子指令，卻會炸掉整包快取。
_TOOL_FIXED = (
    "MonoFSM/1_MonoFSM_Core/Editor/PrefabEditing/PrefabTextReader.cs",
    "MonoFSM/1_MonoFSM_Core/Editor/PrefabExporter/FsmTextExporter.cs",
)
_TOOL_GLOB = "MonoFSM/1_MonoFSM_Core/Editor/PrefabExporter/HierarchyText/*.cs"

# 依賴圖往下追幾層（variant base 的 base…）。再深就沒有實務意義，但掃描成本會線性上升。
MAX_DEPTH = 3

# LRU：超過 KEEP_MAX 個檔就刪到剩 KEEP_MIN
KEEP_MAX = 200
KEEP_MIN = 150

HIT_NOTE = ("# [cache] 命中；來源 prefab 依賴與匯出工具自上次讀取後未變動。"
            "若剛在 Unity Inspector 改過但未存檔，請加 --no-cache")


def tool_files(root: str) -> list[str]:
    """納入 key 的工具檔清單（相對 root）。HierarchyText/ 用 glob，新增檔案自動納入。"""
    out = list(_TOOL_FIXED)
    out += sorted(
        os.path.relpath(p, root).replace(os.sep, "/")
        for p in glob.glob(os.path.join(root, _TOOL_GLOB))
    )
    return out


def _rel(root: str, asset: str) -> str | None:
    """把 CLI 給的資產路徑正規化成 root 下的相對路徑；檔案不存在就回 None。"""
    path = asset if os.path.isabs(asset) else os.path.join(root, asset)
    if not os.path.isfile(path):
        return None
    return os.path.relpath(path, root)


def _deps(root: str, rel: str) -> list[str]:
    """從一個 .prefab 出發，離線收集它（遞迴）引用到的所有 .prefab 相對路徑。

    只掃 YAML 文字裡的 `guid: <32hex>`，再用既有的索引把 guid 翻成路徑
    （query.asset_by_guid，同 cmd_guid 走的那條），只留 .prefab。
    這樣 variant base（m_SourcePrefab）與 nested prefab 會一起被涵蓋。
    guid 翻不出來就忽略（多半是套件內資產或非 prefab）。
    """
    con = indexer.connect(root)
    try:
        visited = {rel}
        frontier = [rel]
        for _ in range(MAX_DEPTH):
            nxt = []
            for cur in frontier:
                try:
                    with open(os.path.join(root, cur), encoding="utf-8",
                              errors="ignore") as fh:
                        text = fh.read()
                except OSError:
                    continue
                for guid in set(GUID_RE.findall(text)):
                    row = query.asset_by_guid(con, guid)
                    if not row:
                        continue
                    path = row[0]
                    if not path.endswith(".prefab") or path in visited:
                        continue
                    visited.add(path)
                    nxt.append(path)
            if not nxt:
                break
            frontier = nxt
        return sorted(visited)
    finally:
        con.close()


# 同一支 prefab 的「工具 + 依賴」指紋在一次 process 內不會變。切片時要對每個候選
# 重算一次 key 驗新鮮度，沒有這層 memo 會把 _deps（掃 YAML + 查 sqlite）跑好幾遍。
_SIG: dict = {}


def _sig(root: str, rel: str) -> str:
    hit = _SIG.get((root, rel))
    if hit is not None:
        return hit
    h = hashlib.sha256()
    for tool in tool_files(root):
        with open(os.path.join(root, tool), "rb") as fh:
            h.update(tool.encode())
            h.update(hashlib.sha256(fh.read()).digest())
    for dep in _deps(root, rel):
        st = os.stat(os.path.join(root, dep))
        h.update(f"{dep}|{st.st_mtime_ns}|{st.st_size}\n".encode())
    val = h.hexdigest()
    _SIG[(root, rel)] = val
    return val


def _key(rel: str, sig: str, params: dict) -> str:
    h = hashlib.sha256()
    h.update(("format=" + CACHE_FORMAT_VERSION + "\n").encode())
    h.update(("rel=" + rel + "\n").encode())
    for k in sorted(params):
        h.update(f"{k}={params[k]}\n".encode())
    h.update(("sig=" + sig).encode())
    return h.hexdigest()[:16]


def key_for(root: str, asset: str, params: dict) -> str | None:
    """算出 cache key（16 hex）。算不出來就回 None，呼叫端當成「不快取」處理。

    `rel.endswith(".prefab")` 的限制刻意不拆：scene 的依賴（scene 裡的 prefab instance
    加上 override）算不準，快取 scene 讀取不安全。
    """
    try:
        rel = _rel(root, asset)
        if rel is None or not rel.endswith(".prefab"):
            return None
        return _key(rel, _sig(root, rel), params)
    except Exception:
        return None


def _path(root: str, key: str) -> str:
    return os.path.join(root, CACHE_DIR, key + ".txt")


def _meta_path(root: str, key: str) -> str:
    return os.path.join(root, CACHE_DIR, key + ".json")


def load(root: str, key: str) -> str | None:
    try:
        path = _path(root, key)
        with open(path, encoding="utf-8") as fh:
            text = fh.read()
        os.utime(path, None)  # LRU：讀到就算「用過」
        return text
    except Exception:
        return None


def store(root: str, key: str, text: str, asset: str = None,
          params: dict = None) -> None:
    """先寫 .tmp 再 rename，避免中途中斷留下半截檔案。任何失敗都靜默忽略。

    另外寫一份 `<key>.json`（rel + params）當切片用的索引 —— 沒有它就只能靠 key
    的雜湊反查，那是算不回來的。
    """
    try:
        d = os.path.join(root, CACHE_DIR)
        os.makedirs(d, exist_ok=True)
        tmp = _path(root, key) + ".tmp"
        with open(tmp, "w", encoding="utf-8") as fh:
            fh.write(text)
        os.replace(tmp, _path(root, key))
        if asset is not None and params is not None:
            rel = _rel(root, asset)
            if rel:
                meta = {"rel": rel, "params": params}
                with open(_meta_path(root, key), "w", encoding="utf-8") as fh:
                    json.dump(meta, fh, ensure_ascii=False)
        _evict(d)
    except Exception:
        pass


def _evict(d: str) -> None:
    names = [n for n in os.listdir(d) if n.endswith(".txt")]
    if len(names) <= KEEP_MAX:
        return
    entries = []
    for n in names:
        full = os.path.join(d, n)
        try:
            entries.append((os.path.getmtime(full), full))
        except OSError:
            continue
    entries.sort()
    for _, full in entries[: max(len(entries) - KEEP_MIN, 0)]:
        try:
            os.remove(full)
            meta = full[: -len(".txt")] + ".json"
            if os.path.exists(meta):
                os.remove(meta)
        except OSError:
            pass


# ------------------------------------------------------------------ 本地切片
#
# 為什麼只切 node、不切 depth：exporter 的摺疊決策是「整棵樹一起算」的
# （PrefabTextReader.Layered 用 charBudget 探出一個全域深度），本地沒有辦法重現
# 「如果只讀這顆子樹，budget 允許展到第幾層」。裁 depth 會讓 agent 拿到一顆
# 看起來完整、其實少了幾層的子樹 —— 那種錯誤沒有任何徵兆。
#
# 反之 node 前綴是純文字操作：只要目標節點那一行到它子樹最後一行都**沒有摺疊標記**，
# 這段文字就是「完整展開的該子樹」，跟直接 read 它等價（相對引用路徑是以節點自己
# 為基準算的，見 CompactValueFormatter 的 GetRelativePath(ctx.Current, …)，
# 換匯出 root 不影響）。有任何一個摺疊標記就放棄，寧可多打一趟 Unity。

# 摺疊/截斷的所有出口都會留下這些字樣：
#   FoldTail → " (+12 nodes)" / " (+12 nodes, 2 notes)"（深度到頂、inactive、已知子樹摘要）
#   maxChildrenPerNode → "… (+5 more siblings)"
#   bare transform chain → " :: bones/transform-only (+9 nodes)"
FOLD_RE = re.compile(r"\(\+\d+ (?:nodes|more siblings)\b")
# 節點名前綴的旗標（BuildFlags）：override `+`、inactive `~` 可以疊。
FLAG_RE = re.compile(r"^[~+\-]+")
# `--node` 支援的同名 sibling 索引語法。本地切片認不出索引（文字裡沒有），
# 遇到就整段放棄。
INDEX_RE = re.compile(r"\[\d+\]$")


def _indent(line: str) -> int:
    return len(line) - len(line.lstrip(" "))


def _node_name(line: str) -> str:
    """從一行匯出文字取回節點名。"""
    body = FLAG_RE.sub("", line.strip())
    for sep in (" <", " :: ", " (+", "   #"):
        i = body.find(sep)
        if i >= 0:
            body = body[:i]
    return body.strip()


def _slice(text: str, rel_path: str) -> str | None:
    """從一段匯出文字裡裁出 rel_path 指的子樹（dedent 過）。裁不出來或不完整回 None。"""
    lines = text.splitlines()
    # 匯出 root = 第一行 indent 0 且不是註解的
    start = None
    for i, line in enumerate(lines):
        if line.strip() and not line.lstrip().startswith("#") and _indent(line) == 0:
            start = i
            break
    if start is None:
        return None

    cur, cur_indent = start, 0
    for seg in rel_path.split("/"):
        if INDEX_RE.search(seg):
            return None
        want = cur_indent + 2
        found = None
        j = cur + 1
        while j < len(lines):
            line = lines[j]
            if not line.strip():
                j += 1
                continue
            ind = _indent(line)
            if ind <= cur_indent:
                break  # 離開這顆子樹了
            if ind == want and _node_name(line) == seg:
                if found is not None:
                    return None  # 同名 sibling，本地分不出是哪一顆
                found = j
            j += 1
        if found is None:
            return None
        cur, cur_indent = found, want

    if FOLD_RE.search(lines[cur]):
        return None  # 目標節點本身是摺疊行，子樹不在文字裡

    out = [lines[cur]]
    j = cur + 1
    while j < len(lines):
        line = lines[j]
        if line.strip() and _indent(line) <= cur_indent:
            break
        out.append(line)
        j += 1
    body = "\n".join(out)
    if FOLD_RE.search(body):
        return None  # 子樹裡有任何摺疊 → 不完整，不准回傳
    pad = " " * cur_indent
    return "\n".join(l[len(pad):] if l.startswith(pad) else l for l in out) + "\n"


def slice_for(root: str, asset: str, params: dict) -> tuple[str, str] | None:
    """試著從既有快取裁出 params 要的子樹。回 (文字, 來源 node) 或 None。"""
    try:
        node = (params.get("node") or "").strip("/")
        if not node:
            return None
        # depth / fsm 一律回 Unity（理由見上面 SLICE 註解）
        if params.get("depth") != -1 or params.get("fsm") or params.get("fsm_only"):
            return None
        rel = _rel(root, asset)
        if rel is None or not rel.endswith(".prefab"):
            return None

        cands = []
        for meta_file in glob.glob(os.path.join(root, CACHE_DIR, "*.json")):
            try:
                with open(meta_file, encoding="utf-8") as fh:
                    meta = json.load(fh)
            except Exception:
                continue
            if meta.get("rel") != rel:
                continue
            p = meta.get("params") or {}
            if (p.get("full") != params.get("full")
                    or p.get("structure_only") != params.get("structure_only")
                    or p.get("fsm") or p.get("fsm_only")
                    or p.get("depth") != -1):
                continue
            cnode = (p.get("node") or "").strip("/")
            if cnode and not node.startswith(cnode + "/"):
                continue
            if cnode == node:
                continue  # 完全相同的參數走 key_for 那條
            key = os.path.basename(meta_file)[: -len(".json")]
            cands.append((len(cnode), key, cnode, p))

        if not cands:
            return None
        sig = _sig(root, rel)
        # 由最靠近的祖先開始試 —— 文字最短、也最可能是完整展開的
        for _, key, cnode, p in sorted(cands, reverse=True):
            if _key(rel, sig, p) != key:
                continue  # 那份快取已經過期
            text = load(root, key)
            if text is None:
                continue
            rest = node[len(cnode) + 1:] if cnode else node
            sliced = _slice(text, rest)
            if sliced is not None:
                return sliced, (cnode or "(root)")
        return None
    except Exception:
        return None
