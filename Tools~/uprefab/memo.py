"""同 argv 的極短期 memo —— 擋掉「同一條指令連打兩次」。

readcache 只能救 `prefab read`，而 usage log 顯示重複的是各種指令：同一條 `find` /
`refs` / `catalog` 在幾十秒內被原封不動重打（換 agent、續派、重新確認）。
那些查詢在 60 秒內幾乎不可能有不同答案，但每次都要一趟 Unity 或一輪 sqlite。

**失效條件刻意做得很粗**：TTL 60 秒，而且期間跑過任何一個「寫入類」指令就整批失效
（epoch 檔的 mtime 變了）。寧可白算一次，也不要回一份剛被自己改掉的舊資料。

不 memo 的：
- 寫入類（`prefab do` / `asset set` / `scene do` / `prompt` / `poke` / `play` …）
- runtime 讀取（`peek` / `logs` / `effect-trace`）—— 那些的答案本來就每秒都在變，
  60 秒的 memo 會讓「改完再確認」看到改前的值。
"""

from __future__ import annotations

import hashlib
import json
import os
import time

CACHE_DIR = os.path.join(".uprefab-cache", "memo")
TTL = 60.0
KEEP = 80
MAX_STORE = 400_000  # 比這更肥就不存，memo 不該變成第二份磁碟快取

# 這些子指令（`_sub_cmd` 的字串）memo 得起來
MEMOIZABLE = {
    "find", "guid", "overrides", "catalog", "cat", "types", "fields", "refs",
    "prefab read", "prefab peek", "prefab peek-batch", "prefab locate",
    "scene ls", "scene count", "obj", "gid", "asset fields", "scope",
}
# 讀但不 memo（答案會自己變），也不會讓別人的 memo 失效
NEUTRAL = {"peek", "logs", "effect-trace", "usage", "clear"}


def _dir(root: str) -> str:
    return os.path.join(root, CACHE_DIR)


def _epoch_path(root: str) -> str:
    return os.path.join(_dir(root), "epoch")


def epoch(root: str) -> str:
    try:
        with open(_epoch_path(root), encoding="utf-8") as fh:
            return fh.read().strip()
    except Exception:
        return "0"


def bump(root: str) -> None:
    """寫入類指令跑之前呼叫 —— 讓所有既有 memo 立刻失效。"""
    try:
        os.makedirs(_dir(root), exist_ok=True)
        with open(_epoch_path(root), "w", encoding="utf-8") as fh:
            fh.write(str(time.time_ns()))
    except Exception:
        pass


def _key(root: str, argv: list[str]) -> str:
    h = hashlib.sha256()
    h.update((os.path.abspath(root) + "\n").encode())
    for a in argv:
        h.update(a.encode())
        h.update(b"\0")
    return h.hexdigest()[:16]


def _path(root: str, key: str) -> str:
    return os.path.join(_dir(root), key + ".json")


def load(root: str, argv: list[str]) -> str | None:
    try:
        with open(_path(root, _key(root, argv)), encoding="utf-8") as fh:
            row = json.load(fh)
        if time.time() - float(row.get("ts", 0)) > TTL:
            return None
        if row.get("epoch") != epoch(root):
            return None
        return row.get("out")
    except Exception:
        return None


def store(root: str, argv: list[str], out: str) -> None:
    try:
        if len(out) > MAX_STORE:
            return
        os.makedirs(_dir(root), exist_ok=True)
        path = _path(root, _key(root, argv))
        tmp = path + ".tmp"
        with open(tmp, "w", encoding="utf-8") as fh:
            json.dump({"ts": time.time(), "epoch": epoch(root), "out": out}, fh,
                      ensure_ascii=False)
        os.replace(tmp, path)
        _evict(_dir(root))
    except Exception:
        pass


def _evict(d: str) -> None:
    names = [n for n in os.listdir(d) if n.endswith(".json")]
    if len(names) <= KEEP:
        return
    entries = []
    for n in names:
        full = os.path.join(d, n)
        try:
            entries.append((os.path.getmtime(full), full))
        except OSError:
            continue
    entries.sort()
    for _, full in entries[: len(entries) - KEEP // 2]:
        try:
            os.remove(full)
        except OSError:
            pass
