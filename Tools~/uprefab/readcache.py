"""`up prefab read` 的輸出快取。

read 走 Unity 端的 PrefabTextReader.Export，每次都是一趟 execute-dynamic-code
來回、輸出可以到 budget 上限；而同一個 prefab（尤其 variant 的 base）在一次調查裡
常被重複讀。這層以「來源檔案的 mtime」當 key 把結果存到磁碟，第二次讀近乎免費。

**正確性優先於命中率**：任何算不準的情況（guid 解不開、檔案讀不到、解析拋例外）
一律當成 miss 直接走 Unity，絕不回傳可能過時的內容。
"""

from __future__ import annotations

import hashlib
import os
import re

import indexer
import query

CACHE_DIR = os.path.join(".uprefab-cache", "read")
GUID_RE = re.compile(r"guid: ([0-9a-f]{32})")

# 依賴圖往下追幾層（variant base 的 base…）。再深就沒有實務意義，但掃描成本會線性上升。
MAX_DEPTH = 3

# LRU：超過 KEEP_MAX 個檔就刪到剩 KEEP_MIN
KEEP_MAX = 200
KEEP_MIN = 150

HIT_NOTE = ("# [cache] 命中；來源 prefab 自上次讀取後未變動。"
            "若剛在 Unity Inspector 改過但未存檔，請加 --no-cache")


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


def key_for(root: str, asset: str, params: dict) -> str | None:
    """算出 cache key（16 hex）。算不出來就回 None，呼叫端當成「不快取」處理。"""
    try:
        rel = _rel(root, asset)
        if rel is None or not rel.endswith(".prefab"):
            return None
        h = hashlib.sha256()
        for k in sorted(params):
            h.update(f"{k}={params[k]}\n".encode())
        for dep in _deps(root, rel):
            st = os.stat(os.path.join(root, dep))
            h.update(f"{dep}|{st.st_mtime_ns}|{st.st_size}\n".encode())
        return h.hexdigest()[:16]
    except Exception:
        return None


def _path(root: str, key: str) -> str:
    return os.path.join(root, CACHE_DIR, key + ".txt")


def load(root: str, key: str) -> str | None:
    try:
        path = _path(root, key)
        with open(path, encoding="utf-8") as fh:
            text = fh.read()
        os.utime(path, None)  # LRU：讀到就算「用過」
        return text
    except Exception:
        return None


def store(root: str, key: str, text: str) -> None:
    """先寫 .tmp 再 rename，避免中途中斷留下半截檔案。任何失敗都靜默忽略。"""
    try:
        d = os.path.join(root, CACHE_DIR)
        os.makedirs(d, exist_ok=True)
        tmp = _path(root, key) + ".tmp"
        with open(tmp, "w", encoding="utf-8") as fh:
            fh.write(text)
        os.replace(tmp, _path(root, key))
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
        except OSError:
            pass
