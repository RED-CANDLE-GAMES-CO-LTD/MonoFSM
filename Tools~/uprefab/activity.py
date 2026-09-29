"""Agent activity log —— 讓 Unity Editor 看得到「agent 現在正在用 Editor 做什麼」。

`unity.run` / `unity.csharp`（所有碰 Unity 的 up 指令都經過這兩個）每次呼叫前後各 append 一行到
`Library/AgentActivity/activity.jsonl`，Unity 端的 toolbar（`ToolbarAgentActivity`）和
`Tools/MonoFSM/Agent Activity` 視窗輪詢這個檔顯示。

為什麼在 Python 端記、不在 Unity 端 probe 記：這裡才有完整的 up argv 和結果；而且 Unity main
thread 卡住（正是使用者最想知道「agent 在幹嘛」的時候）Unity 端根本寫不進去。

每行格式（JSON，key 名跟 Unity 端 `AgentActivityLog.Line` 欄位一一對應）：
  {"ev":"begin","id":"…","session":"6b79dd","argv":"up prefab read …","method":"…","t":<epoch 秒>,"pid":123}
  {"ev":"end","id":"…","t":<epoch 秒>,"ok":true,"summary":"第一行"}

另外管「測試中」旗標 `.claude/.agent-testing`（內容：`<session>\\t<ISO 時間>`）：
`up play play` 成功寫入、`up play stop` 清掉；期間同 session 每次碰 Unity 都 touch mtime。
Unity 端把 mtime 超過 10 分鐘的旗標當成過期（agent 掛掉沒 stop 時不會永遠卡在「測試中」）。

寫 log 失敗一律吞掉 —— 這是給人看的旁路資訊，不能讓它把 up 指令本身弄壞。
"""

from __future__ import annotations

import datetime
import json
import os
import re
import sys
import time
import uuid

try:
    import fcntl
except ImportError:  # Windows：沒有 flock，就不鎖（最多偶爾一行交錯）
    fcntl = None

LOG_REL = os.path.join("Library", "AgentActivity", "activity.jsonl")
FLAG_REL = os.path.join(".claude", ".agent-testing")
# 一次呼叫兩行，保留最後 500 次呼叫 ≈ 1000 行；超過 MAX_BYTES 才截（不是每次都讀整份）
KEEP_LINES = 1000
MAX_BYTES = 512 * 1024
SUMMARY_CLIP = 200

_root_cache: str | None = None
_session_cache: str | None = None


def _project_root() -> str | None:
    """Unity 專案根目錄（同時有 Assets/ 和 ProjectSettings/ 的那層）。先從這支檔案往上找
    （up 可能在任何 cwd 被叫），找不到再從 cwd 找。"""
    global _root_cache
    if _root_cache is not None:
        return _root_cache or None
    for start in (os.path.dirname(os.path.abspath(__file__)), os.getcwd()):
        cur = start
        while True:
            if os.path.isdir(os.path.join(cur, "Assets")) and os.path.isdir(
                os.path.join(cur, "ProjectSettings")
            ):
                _root_cache = cur
                return cur
            parent = os.path.dirname(cur)
            if parent == cur:
                break
            cur = parent
    _root_cache = ""
    return None


def _find_claude_pid() -> int | None:
    """沒有 session 環境變數時的後備：沿 ppid 往上找名字含 claude 的 process。"""
    import subprocess

    pid = os.getppid()
    for _ in range(12):
        if pid <= 1:
            return None
        try:
            out = subprocess.run(
                ["ps", "-o", "ppid=,comm=", "-p", str(pid)],
                capture_output=True, text=True, timeout=2,
            ).stdout.strip()
        except Exception:
            return None
        if not out:
            return None
        ppid_s, _, comm = out.partition(" ")
        if "claude" in os.path.basename(comm.strip()).lower():
            return pid
        try:
            pid = int(ppid_s)
        except ValueError:
            return None
    return None


def session_tag() -> str:
    """同一個 Claude Code session 內穩定不變的短標籤。

    優先 `CLAUDE_CODE_SESSION_ID` 前 6 碼（subagent 繼承主線的值，所以同 session 的 agent 都同一個標籤）；
    沒有就 `CLAUDE_PID`；再沒有就沿 ppid 找 claude process；都找不到代表是人在 terminal 手打的。
    """
    global _session_cache
    if _session_cache:
        return _session_cache
    sid = os.environ.get("CLAUDE_CODE_SESSION_ID", "").strip()
    if sid:
        tag = sid.replace("-", "")[:6]
    elif os.environ.get("CLAUDE_PID", "").strip():
        tag = "pid" + os.environ["CLAUDE_PID"].strip()
    else:
        pid = _find_claude_pid()
        tag = f"pid{pid}" if pid else "human"
    _session_cache = tag
    return tag


def _argv_text() -> str:
    parts = ["up"]
    for a in sys.argv[1:]:
        parts.append(a if a and not re.search(r"\s|['\"]", a) else json.dumps(a, ensure_ascii=False))
    return " ".join(parts)


def _append(line: dict) -> None:
    root = _project_root()
    if not root:
        return
    path = os.path.join(root, LOG_REL)
    data = (json.dumps(line, ensure_ascii=False, separators=(",", ":")) + "\n").encode("utf-8")
    try:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "ab") as f:
            if fcntl:
                fcntl.flock(f, fcntl.LOCK_EX)
            try:
                f.write(data)
                f.flush()
                if f.tell() > MAX_BYTES:
                    _trim(path)
            finally:
                if fcntl:
                    fcntl.flock(f, fcntl.LOCK_UN)
    except Exception:
        pass


def _trim(path: str) -> None:
    """只保留最後 KEEP_LINES 行。呼叫端持有 flock；用 tmp + replace，Unity 端讀到的永遠是完整檔。"""
    with open(path, "rb") as f:
        lines = f.read().splitlines(keepends=True)
    if len(lines) <= KEEP_LINES:
        return
    tmp = path + ".tmp"
    with open(tmp, "wb") as f:
        f.writelines(lines[-KEEP_LINES:])
    os.replace(tmp, path)


def _flag_path() -> str | None:
    root = _project_root()
    return os.path.join(root, FLAG_REL) if root else None


def _touch_flag() -> None:
    """測試中旗標存在、而且是自己這個 session 寫的，就更新 mtime（續命）。"""
    path = _flag_path()
    if not path or not os.path.exists(path):
        return
    try:
        with open(path, encoding="utf-8") as f:
            owner = f.read().split("\t", 1)[0].strip()
        if owner == session_tag():
            os.utime(path, None)
    except Exception:
        pass


def set_testing(on: bool) -> None:
    path = _flag_path()
    if not path:
        return
    try:
        if on:
            os.makedirs(os.path.dirname(path), exist_ok=True)
            now = datetime.datetime.now().isoformat(timespec="seconds")
            with open(path, "w", encoding="utf-8") as f:
                f.write(f"{session_tag()}\t{now}\n")
        elif os.path.exists(path):
            os.remove(path)
    except Exception:
        pass


_METHOD_RE = re.compile(r"return\s+([\w.]+)\s*\(")


def method_of(code: str) -> str:
    m = _METHOD_RE.search(code)
    return m.group(1) if m else "execute-dynamic-code"


def begin(method: str) -> str:
    """記一筆開始，回傳 id 給 end() 用。"""
    call_id = uuid.uuid4().hex[:10]
    _append({
        "ev": "begin", "id": call_id, "session": session_tag(), "argv": _argv_text(),
        "method": method, "t": round(time.time(), 3), "pid": os.getpid(),
    })
    _touch_flag()
    return call_id


def end(call_id: str, ok: bool, summary: str = "") -> None:
    first = ""
    for line in str(summary or "").splitlines():
        if line.strip():
            first = line.strip()
            break
    if len(first) > SUMMARY_CLIP:
        first = first[:SUMMARY_CLIP] + "…"
    _append({"ev": "end", "id": call_id, "t": round(time.time(), 3), "ok": bool(ok),
             "summary": first})
