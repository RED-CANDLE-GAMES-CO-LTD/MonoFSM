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
import time


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


def _uloop() -> str:
    exe = shutil.which("uloop")
    if not exe:
        raise UnityError("找不到 uloop CLI（Unity Editor 要開著，且 uloop 已安裝）")
    return exe


def run(args: list[str], timeout: int = 300) -> dict:
    """跑一個 uloop 子指令，回傳解析後的 JSON。Domain Reload 期間會自己等再重試。"""
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


def csharp(code: str, timeout: int = 300) -> str:
    """執行一段 C#，回傳 Result 字串。失敗時拋 UnityError 並附上編譯錯誤。"""
    data = run(["execute-dynamic-code", "--code", code], timeout=timeout)

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


def call(target: str, *args) -> str:
    """呼叫一個 static 方法並 return 它的字串結果。"""
    joined = ", ".join(lit(a) for a in args)
    return csharp(f"return {target}({joined});")


EDIT_NS = "MonoFSM.Editor.PrefabEditing"
