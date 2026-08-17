"""uprefab 使用記錄 —— 用來量測「調查一件事到底花了多少來回」。

每次 CLI 呼叫在 repo root 的 `.uprefab-usage.jsonl` 追加一行，事後用
`uprefab.py usage` 統計。目的是把「查 prefab 好慢」變成有數據的問題：
哪些指令一直重試（猜不到關鍵字）、哪些下鑽鏈特別長（要跳很多次）、
哪些 (asset, node) 被反覆讀（重複調查）、output 到底多大。

寫檔失敗一律吞掉 —— 記錄壞掉不該讓指令本身失敗。
設 `UPREFAB_NO_USAGE_LOG=1` 可完全關閉。
"""

from __future__ import annotations

import json
import os
import time

LOG_NAME = ".uprefab-usage.jsonl"

# 不記進 args 的欄位：內部用的、或長到沒有分析價值的
_SKIP_ARGS = {"fn", "root", "cmd"}
_MAX_VAL = 120


class Tee:
    """包住 stdout，一邊照常輸出一邊數字元數。"""

    HEAD = 400

    def __init__(self, real):
        self._real = real
        self.chars = 0
        self.head = ""

    def write(self, s):
        self.chars += len(s)
        if len(self.head) < self.HEAD:
            self.head += s[: self.HEAD - len(self.head)]
        return self._real.write(s)

    def flush(self):
        return self._real.flush()

    def __getattr__(self, name):
        return getattr(self._real, name)


def enabled() -> bool:
    return os.environ.get("UPREFAB_NO_USAGE_LOG") != "1"


# 指令執行中補上的欄位（目前只有 cache=hit/miss/off），record 時併進那一行
_EXTRA: dict = {}


def note(key: str, value) -> None:
    _EXTRA[key] = value


def _clip(v):
    if isinstance(v, str) and len(v) > _MAX_VAL:
        return v[:_MAX_VAL] + "…"
    return v


def _sub_cmd(args) -> str:
    """把 `prefab` + `read` 這種兩層指令併成 `prefab read`。"""
    cmd = getattr(args, "cmd", "?")
    for k in ("prefab_action", "scene_action", "asset_action", "action"):
        v = getattr(args, k, None)
        if isinstance(v, str) and v:
            return f"{cmd} {v}"
    return cmd


def record(root: str, args, out_chars: int, elapsed_ms: int, status: str,
           out_head: str = "") -> None:
    """追加一行記錄。任何失敗都靜默忽略。"""
    if not enabled():
        return
    try:
        payload = {}
        for k, v in vars(args).items():
            if k in _SKIP_ARGS or k.endswith("_action"):
                continue
            if v is None or v is False:
                continue
            payload[k] = _clip(v)
        row = {
            "ts": round(time.time(), 1),
            "cmd": _sub_cmd(args),
            "args": payload,
            "out": out_chars,
            "ms": elapsed_ms,
            "st": status,
        }
        row.update(_EXTRA)
        # 「找不到」是診斷「猜不到入口關鍵字」的主要訊號
        if "(no match)" in out_head or "解不開" in out_head or out_chars == 0:
            row["miss"] = True
        with open(os.path.join(root, LOG_NAME), "a", encoding="utf-8") as f:
            f.write(json.dumps(row, ensure_ascii=False) + "\n")
    except Exception:
        pass


# ---------------------------------------------------------------- 統計


def _load(root: str) -> list:
    path = os.path.join(root, LOG_NAME)
    if not os.path.exists(path):
        raise SystemExit(f"# 還沒有使用記錄（{LOG_NAME} 不存在）")
    rows = []
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                rows.append(json.loads(line))
            except json.JSONDecodeError:
                continue
    return rows


def _sessions(rows: list, gap_sec: int) -> list:
    """相鄰呼叫間隔超過 gap_sec 就切一段，當作一次「調查」。"""
    out, cur = [], []
    for r in sorted(rows, key=lambda x: x.get("ts", 0)):
        if cur and r.get("ts", 0) - cur[-1].get("ts", 0) > gap_sec:
            out.append(cur)
            cur = []
        cur.append(r)
    if cur:
        out.append(cur)
    return out


def _target(r: dict) -> str:
    """一次呼叫問的是「哪個東西」—— 用來偵測反覆重查。"""
    a = r.get("args", {})
    asset = a.get("asset") or a.get("path") or ""
    node = a.get("node") or ""
    if asset or node:
        return f"{os.path.basename(str(asset))}#{node}"
    # find / guid / types 這類沒有 asset 的，用查詢條件本身當標籤
    for k in ("comp", "name", "token", "type", "link"):
        if a.get(k):
            return f"{k}={a[k]}"
    return ""


def report(root: str, gap_sec: int = 900, top: int = 8) -> None:
    rows = _load(root)
    sess = _sessions(rows, gap_sec)
    total_out = sum(r.get("out", 0) for r in rows)

    print(f"# {len(rows)} 次呼叫 / {len(sess)} 段調查"
          f"（間隔 >{gap_sec}s 視為新的一段）/ 總輸出 {total_out:,} 字元")

    # 每個指令的次數與輸出量
    per_cmd = {}
    for r in rows:
        d = per_cmd.setdefault(r.get("cmd", "?"), {"n": 0, "out": 0, "miss": 0})
        d["n"] += 1
        d["out"] += r.get("out", 0)
        d["miss"] += 1 if r.get("miss") else 0
    print("\n## 各指令")
    print("| 指令 | 次數 | 總輸出 | 平均 | 落空 |")
    print("|---|---|---|---|---|")
    for cmd, d in sorted(per_cmd.items(), key=lambda kv: -kv[1]["out"])[:top * 2]:
        print(f"| {cmd} | {d['n']} | {d['out']:,} | {d['out'] // max(d['n'], 1):,} "
              f"| {d['miss']} |")

    # 痛點 1：猜不到入口 —— 落空後緊接著同指令重試
    retries = 0
    for s in sess:
        for i, r in enumerate(s[:-1]):
            if r.get("miss") and s[i + 1].get("cmd") == r.get("cmd"):
                retries += 1
    print(f"\n## 猜不到入口：落空後立刻換參數重試 {retries} 次"
          f"（佔全部呼叫 {retries * 100 // max(len(rows), 1)}%）")

    # 痛點 2：找到後要跳很多次 —— 每段調查的呼叫數分佈
    lens = sorted((len(s) for s in sess), reverse=True)
    if lens:
        mid = lens[len(lens) // 2]
        print(f"\n## 一段調查要幾次呼叫：中位數 {mid}、最長 {lens[0]}、"
              f"前五長 {lens[:5]}")

    # 痛點 3：反覆重查 —— 同一個 (asset, node) 跨段被讀了幾次
    seen = {}
    for si, s in enumerate(sess):
        for r in s:
            t = _target(r)
            if t:
                seen.setdefault(t, set()).add(si)
    repeat = sorted(((len(v), k) for k, v in seen.items() if len(v) > 1),
                    reverse=True)
    print(f"\n## 反覆重查：{len(repeat)} 個目標在多段調查裡被重複讀")
    for n, t in repeat[:top]:
        print(f"  {n} 段  {t}")

    # 痛點 4：回傳量 —— 最肥的幾次
    fat = sorted(rows, key=lambda r: -r.get("out", 0))[:top]
    print("\n## 最肥的幾次呼叫")
    for r in fat:
        print(f"  {r.get('out', 0):>8,} 字元  {r.get('cmd')}  {_target(r)}")
