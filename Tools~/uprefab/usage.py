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
    """包住 stdout：數字元數、（選配）攔在 cap、（選配）錄下實際印出的內容給 memo 用。

    cap 是**第二道網**，不是主要手段：每個子指令該有自己的 -n / --budget。這裡只負責
    「不管哪條路漏了上限，都不會一次吃掉整個 context」。攔截時一定要附**原長**與
    **縮小範圍的建議** —— 只說「被截斷了」會讓人原封不動重打一次更貴的指令。
    """

    HEAD = 400

    def __init__(self, real, cap: int = 0, hint: str = ""):
        self._real = real
        self.chars = 0
        self.head = ""
        self.cap = max(0, int(cap or 0))
        self.hint = hint or ""
        self.truncated = False
        self._written = 0
        self._buf = None

    def capture(self) -> None:
        """開始錄「實際印出去」的內容（含截斷提示），讓 memo replay 逐字相同。"""
        self._buf = []

    def text(self) -> str:
        return "".join(self._buf) if self._buf is not None else ""

    def _out(self, chunk):
        if self._buf is not None:
            self._buf.append(chunk)
        return self._real.write(chunk)

    def write(self, s):
        self.chars += len(s)
        if len(self.head) < self.HEAD:
            self.head += s[: self.HEAD - len(self.head)]
        if not self.cap:
            return self._out(s)
        room = self.cap - self._written
        if room <= 0:
            self.truncated = True
            return len(s)
        self._written += len(s)
        if len(s) <= room:
            return self._out(s)
        self.truncated = True
        self._out(s[:room])
        return len(s)

    def replay(self, text: str):
        """memo 命中時原樣吐回去 —— 已經是攔截後的內容，不要再攔一次。"""
        self.chars += len(text)
        if len(self.head) < self.HEAD:
            self.head += text[: self.HEAD - len(self.head)]
        self._real.write(text)
        if self._buf is not None:
            self._buf.append(text)

    def finish(self) -> None:
        if not self.truncated:
            return
        note = (f"\n# ⚠ 輸出被 uprefab 攔在 {self.cap:,} 字元"
                f"（完整輸出 {self.chars:,}，截掉 {self.chars - self.cap:,}）。"
                f"{self.hint} 真的要全部：--max-chars 0\n")
        self._out(note)
        self.chars += len(note)

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


def _percentile(values: list[int], q: float) -> int:
    if not values:
        return 0
    ordered = sorted(values)
    return ordered[min(int((len(ordered) - 1) * q), len(ordered) - 1)]


def report(root: str, gap_sec: int = 900, top: int = 8,
           since_hours: float | None = None) -> None:
    rows = _load(root)
    if since_hours is not None:
        cutoff = time.time() - since_hours * 3600
        rows = [r for r in rows if r.get("ts", 0) >= cutoff]
        if not rows:
            raise SystemExit(f"# 最近 {since_hours:g} 小時沒有使用記錄")
    sess = _sessions(rows, gap_sec)
    total_out = sum(r.get("out", 0) for r in rows)
    elapsed = [int(r.get("ms", 0)) for r in rows]

    print(f"# {len(rows)} 次呼叫 / {len(sess)} 段調查"
          f"（間隔 >{gap_sec}s 視為新的一段）/ 總輸出 {total_out:,} 字元"
          f" / elapsed avg={sum(elapsed) // max(len(elapsed), 1)}ms"
          f" p95={_percentile(elapsed, .95)}ms")

    # 每個指令的次數與輸出量
    per_cmd = {}
    for r in rows:
        d = per_cmd.setdefault(r.get("cmd", "?"),
                               {"n": 0, "out": 0, "miss": 0, "ms": []})
        d["n"] += 1
        d["out"] += r.get("out", 0)
        d["miss"] += 1 if r.get("miss") else 0
        d["ms"].append(int(r.get("ms", 0)))
    print("\n## 各指令")
    print("| 指令 | 次數 | 總輸出 | 平均字元 | avg ms | p95 ms | 落空 |")
    print("|---|---|---|---|---|---|---|")
    for cmd, d in sorted(per_cmd.items(), key=lambda kv: -kv[1]["out"])[:top * 2]:
        print(f"| {cmd} | {d['n']} | {d['out']:,} | {d['out'] // max(d['n'], 1):,} "
              f"| {sum(d['ms']) // max(d['n'], 1)} | {_percentile(d['ms'], .95)} "
              f"| {d['miss']} |")

    # hit ratio 不把 bypass/unavailable/off 算進分母。slice = 本地從較大子樹裁出來的，
    # 一樣沒打 Unity，所以算在 hit 那邊。
    cache_rows = [r for r in rows if r.get("cmd") == "prefab read" and r.get("cache")]
    cache_counts = {}
    for r in cache_rows:
        cache_counts[r["cache"]] = cache_counts.get(r["cache"], 0) + 1
    hits = cache_counts.get("hit", 0) + cache_counts.get("slice", 0)
    attempts = hits + cache_counts.get("miss", 0)
    print("\n## prefab read cache")
    print(f"hit ratio: {hits}/{attempts} "
          f"({hits * 100 // max(attempts, 1)}%)；狀態 {cache_counts or '(無)'}")

    # argv memo（跨指令）：同一條指令在 60 秒內被原封不動重打
    memo_hits = sum(1 for r in rows if r.get("memo") == "hit")
    print(f"## argv memo 命中 {memo_hits} 次"
          f"（佔全部呼叫 {memo_hits * 100 // max(len(rows), 1)}%）")

    # 舊版 depth / FSM 可繞過 budget；新版 hard cap 上線後這裡應逐漸歸零。
    oversized = []
    for r in rows:
        if r.get("cmd") != "prefab read":
            continue
        a = r.get("args", {})
        budget = a.get("budget", 20000)
        if isinstance(budget, int) and budget > 0 and r.get("out", 0) > budget + 300:
            oversized.append(r)
    explicit = sum(1 for r in oversized if r.get("args", {}).get("depth", -1) >= 0)
    with_fsm = sum(1 for r in oversized if r.get("args", {}).get("fsm"))
    print("\n## prefab read budget 超量")
    print(f"{len(oversized)} 次（explicit depth={explicit}、含 FSM={with_fsm}）；"
          "新版 hard budget 正常時應為 0")

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
