"""uloop 橋接層 —— 把 execute-dynamic-code 的 JSON envelope 壓成只剩結果。

為什麼需要這層：`uloop execute-dynamic-code` 每次回傳約 15 行 JSON
（Logs / CompilationErrors / SecurityLevel / Diagnostics …），實際有用的只有 `Result`。
一次 scene 建構要來回十幾次，那些 envelope 的 context 成本會蓋過內容本身。

失敗時才把 CompilationErrors / ErrorMessage 印出來 —— 沒錯就不該佔版面。
"""

from __future__ import annotations

import json
import shutil
import subprocess
import sys
import time

import activity


class UnityError(RuntimeError):
    pass


# 編譯完 Unity 會 Domain Reload，這期間所有呼叫都直接失敗。編輯 C# 之後緊接著呼叫是
# 最常見的流程，所以這裡自己等 —— 不然每次都要人工重跑一次。
RELOAD_HINTS = (
    "Domain Reload",
    "is reloading",
    "is compiling",
    "Compiling",
    "server is starting",
)
RELOAD_RETRIES = 20
RELOAD_WAIT = 3.0

# uloop 有回 JSON、但 Success=false 的「Unity 暫時忙」。上面 RELOAD_HINTS 只管「沒有輸出」那一種，
# 這兩種會直接變成 UnityError —— 2026-09-29 `prefab read` 剛好撞上 domain reload，agent 把
# 「Unity 呼叫失敗」讀成「read 看不到這個節點」，還以為是 prefab 沒存檔，其實重跑一次就好。
#   BUSY_NOT_RUN：程式碼根本沒進 Unity 執行，任何呼叫都能安全重跑
#   BUSY_MAYBE_RAN：runtime 在執行途中被收掉，不知道跑了多少 —— 只對唯讀呼叫重跑（見 READ_ONLY）
BUSY_NOT_RUN = ("Another execution is already in progress",)
BUSY_MAYBE_RAN = (
    "was disposed during a server reset or domain reload",
    "retry the same command shortly",
)
BUSY_RETRIES = 10
BUSY_WAIT = 3.0

# 純讀取的 Unity 端入口（前綴比對 method 名）。寫入類（PrefabEdit / SceneEdit / AssetEdit /
# EditProbe.Poke …）不在這裡：途中被 domain reload 打斷時可能已經存了一半，自動重跑會做兩次。
READ_ONLY = (
    "MonoFSM.Editor.PrefabEditing.PrefabTextReader.",
    "MonoFSM.Editor.PrefabEditing.EditRefs.",
    "MonoFSM.Editor.PrefabEditing.AssetDeps.",
    "MonoFSM.Editor.PrefabEditing.EditBounds.",
    "uprefab.anim.",  # anim.py 的 inline 唯讀 snippet（LoadPrefabContents 讀節點表）
    "MonoFSM.Editor.PrefabEditing.EditProbe.ComponentNames",
    "MonoFSM.Editor.PrefabEditing.EditProbe.DumpAll",
    "MonoFSM.Editor.PrefabEditing.EditProbe.Fields",
    "MonoFSM.Editor.PrefabEditing.EditProbe.LocateAsset",
    "MonoFSM.Editor.PrefabEditing.EditProbe.Peek",
    "MonoFSM.Editor.PrefabEditing.EditProbe.Types",
)


def _uloop() -> str:
    exe = shutil.which("uloop")
    if not exe:
        raise UnityError("找不到 uloop CLI（Unity Editor 要開著，且 uloop 已安裝）")
    return exe


def run(args: list[str], timeout: int = 300) -> dict:
    """跑一個 uloop 子指令，回傳解析後的 JSON。Domain Reload 期間會自己等再重試。

    每次呼叫前後各寫一筆 activity log（見 activity.py）；`control-play-mode` 成功時順便
    寫 / 清「測試中」旗標（play 寫、stop 清）。
    """
    call_id = activity.begin(args[0])
    try:
        data = _run_raw(args, timeout)
    except BaseException as e:
        activity.end(call_id, False, str(e) or type(e).__name__)
        raise
    ok = data.get("Success") is not False
    activity.end(call_id, ok, data.get("ErrorMessage") or data.get("Message") or "")
    if ok and args[0] == "control-play-mode" and "--action" in args:
        action = args[args.index("--action") + 1]
        # play 被 compile error / 未存檔擋下時 Success 仍可能是 true，要看 IsPlaying
        if action == "play" and data.get("IsPlaying"):
            activity.set_testing(True)
        elif action == "stop":
            activity.set_testing(False)
    return data


def _run_raw(args: list[str], timeout: int = 300) -> dict:
    for attempt in range(RELOAD_RETRIES):
        proc = subprocess.run(
            [_uloop(), *args],
            capture_output=True,
            text=True,
            timeout=timeout,
        )
        out = proc.stdout.strip()
        if out:
            break
        blob = proc.stdout + proc.stderr
        if any(h in blob for h in RELOAD_HINTS) and attempt < RELOAD_RETRIES - 1:
            time.sleep(RELOAD_WAIT)
            continue
        raise UnityError(
            f"uloop {args[0]} 沒有輸出（exit={proc.returncode}）\n{proc.stderr.strip()}"
        )
    try:
        return json.loads(out)
    except json.JSONDecodeError:
        # uloop 有時會在 JSON 前後夾雜訊息，撈最外層的那個物件
        start, end = out.find("{"), out.rfind("}")
        if start < 0 or end < start:
            raise UnityError(f"uloop 回傳的不是 JSON：\n{out[:500]}") from None
        return json.loads(out[start : end + 1])


def csharp(code: str, timeout: int = 300, method: str | None = None, track: bool = True) -> str:
    """執行一段 C#，回傳 Result 字串。失敗時拋 UnityError 並附上編譯錯誤。

    track=False 不寫 activity log —— 給輪詢用（`up play play` 等 tick），外層自己記一筆整段的。
    Unity 暫時忙（domain reload / 別的 session 正在跑）會自己等再重跑，規則見 BUSY_NOT_RUN。
    """
    read_only = bool(method) and method.startswith(READ_ONLY)
    for attempt in range(BUSY_RETRIES):
        try:
            return _csharp_tracked(code, timeout, method, track)
        except UnityError as e:
            msg = str(e)
            not_run = any(h in msg for h in BUSY_NOT_RUN)
            maybe_ran = any(h in msg for h in BUSY_MAYBE_RAN)
            if not (not_run or maybe_ran):
                raise
            if (not_run or read_only) and attempt < BUSY_RETRIES - 1:
                # stderr：不進 stdout 的輸出上限與 usage 字數，但人和 agent 都看得到
                print(f"# Unity 暫時忙（{msg.splitlines()[0][:80]}），"
                      f"{BUSY_WAIT:.0f}s 後重跑（{attempt + 1}/{BUSY_RETRIES - 1}）", file=sys.stderr)
                time.sleep(BUSY_WAIT)
                continue
            why = ("重試用完了" if (not_run or read_only)
                   else "這是寫入類呼叫，執行途中被收掉、不知道跑了多少，所以不自動重跑")
            raise UnityError(
                f"{msg}\n# ↑ Unity 暫時忙（domain reload / 別的 session 在跑），不是這條指令的結果——"
                f"不要拿它下「找不到 / 沒存檔」之類的結論。{why}；"
                "等 Unity 編譯完再重跑同一條（寫入類先用唯讀指令確認有沒有已經寫進去）") from None
    raise AssertionError("unreachable")


def _csharp_tracked(code: str, timeout: int, method: str | None, track: bool) -> str:
    if not track:
        return _csharp(code, timeout)
    call_id = activity.begin(method or activity.method_of(code))
    try:
        result = _csharp(code, timeout)
    except BaseException as e:
        activity.end(call_id, False, str(e) or type(e).__name__)
        raise
    activity.end(call_id, True, result)
    return result


def _csharp(code: str, timeout: int) -> str:
    data = _run_raw(["execute-dynamic-code", "--code", code], timeout=timeout)

    if not data.get("Success"):
        parts = []
        for e in data.get("CompilationErrors") or []:
            parts.append(str(e))
        for key in ("ErrorMessage", "Error"):
            if data.get(key):
                parts.append(str(data[key]))
        raise UnityError("\n".join(parts) or json.dumps(data, ensure_ascii=False)[:800])

    result = data.get("Result")
    return "" if result is None else str(result)


def lit(value) -> str:
    """把 Python 值轉成 C# 字面值。None → null，字串走 verbatim 以避開跳脫地獄。"""
    if value is None:
        return "null"
    if value is True:
        return "true"
    if value is False:
        return "false"
    if isinstance(value, (int, float)):
        return repr(value)
    text = str(value)
    # 含換行時不能用 verbatim —— execute-dynamic-code 會把整段程式碼縮排，
    # verbatim 字串裡的換行會連帶吃到縮排空白。改用逐字跳脫的普通字串。
    if "\n" in text or "\r" in text:
        escaped = (text.replace("\\", "\\\\").replace('"', '\\"')
                   .replace("\r", "\\r").replace("\n", "\\n"))
        return '"' + escaped + '"'
    return '@"' + text.replace('"', '""') + '"'


def call(target: str, *args, track: bool = True) -> str:
    """呼叫一個 static 方法並 return 它的字串結果。"""
    joined = ", ".join(lit(a) for a in args)
    return csharp(f"return {target}({joined});", method=target, track=track)


EDIT_NS = "MonoFSM.Editor.PrefabEditing"
