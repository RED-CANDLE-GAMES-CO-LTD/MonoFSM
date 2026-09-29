"""`up fsm-trace` —— 讀 FsmTrace 的 dump 檔（Library/FsmTrace/<name>.txt，離線）。

dump 由 Unity 端寫：`FsmTrace.Dump(name, entity)`，或選單 Tools/MonoFSM/FSM Trace/Dump All、
Dump Selected Entity（CLI：`up menu "Tools/MonoFSM/FSM Trace/Dump All"`）。
檔案格式：`# ` 開頭是檔頭；之後一行一筆紀錄（舊 → 新）；`## SNAPSHOT` 之後是條件快照。

刻意不做的：不在這裡解析欄位做結構化查詢 —— 一行一筆的純文字用關鍵字過濾就夠，
欄位格式以 C# 端（FsmTrace.Dump.cs 的 AppendEntry）為準，兩邊不要各自維護一份 schema。
"""

from __future__ import annotations

import os

FOLDER = os.path.join("Library", "FsmTrace")
SNAPSHOT_MARKER = "## SNAPSHOT"

USAGE = (
    "用法：up fsm-trace [name] [--entity 關鍵字] [--last N] [--no-snapshot] [--list]\n"
    "  name          dump 檔名（不含 .txt，可只給前綴）；不給 = 最新那份\n"
    "  --entity KW   只留含這段字的紀錄（entity 名 / 節點名都會比對，不分大小寫）\n"
    "  --last N      最後幾筆（預設 50，0 = 全部）\n"
    "  --no-snapshot 不印檔尾的條件快照\n"
    "  --list        列出所有 dump 檔\n"
    "產生 dump：Unity 選單 Tools/MonoFSM/FSM Trace/Enabled 打勾 → 玩 → Dump All / Dump Selected Entity\n"
    "  （CLI：up menu \"Tools/MonoFSM/FSM Trace/Dump All\"）"
)


def _files(folder: str) -> list[str]:
    if not os.path.isdir(folder):
        return []
    out = [os.path.join(folder, f) for f in os.listdir(folder) if f.endswith(".txt")]
    return sorted(out, key=os.path.getmtime)


def _pick(files: list[str], name: str | None) -> str:
    if not name:
        return files[-1]
    stem = name[:-4] if name.endswith(".txt") else name
    exact = [f for f in files if os.path.basename(f)[:-4] == stem]
    if exact:
        return exact[-1]
    low = stem.lower()
    pref = [f for f in files if os.path.basename(f).lower().startswith(low)]
    if pref:
        return pref[-1]
    part = [f for f in files if low in os.path.basename(f).lower()]
    if part:
        return part[-1]
    names = ", ".join(os.path.basename(f)[:-4] for f in files[-10:])
    raise SystemExit(f"# 找不到 dump「{name}」。現有（新的在後）：{names}\n{USAGE}")


def cmd(args, root, cfg):
    folder = os.path.join(root, FOLDER)
    files = _files(folder)
    if not files:
        raise SystemExit(f"# {FOLDER} 底下還沒有 dump 檔。\n{USAGE}")

    if args.list:
        for f in reversed(files):
            size = os.path.getsize(f)
            with open(f, encoding="utf-8", errors="replace") as fh:
                head = fh.readline().strip()
            print(f"{os.path.basename(f)[:-4]}\t{size:,}B\t{head}")
        return

    if args.last is not None and args.last < 0:
        raise SystemExit(f"# --last 要 >= 0（0 = 全部）\n{USAGE}")

    path = _pick(files, args.name)
    with open(path, encoding="utf-8", errors="replace") as fh:
        lines = fh.read().splitlines()

    header, entries, snapshot = [], [], []
    section = "head"
    for ln in lines:
        if ln.startswith(SNAPSHOT_MARKER):
            section = "snap"
            continue
        if section == "snap":
            snapshot.append(ln)
        elif ln.startswith("# "):
            header.append(ln)
        elif ln.strip():
            section = "body"
            entries.append(ln)

    kw = (args.entity or "").lower()
    if kw:
        entries = [e for e in entries if kw in e.lower()]

    total = len(entries)
    n = 50 if args.last is None else args.last
    shown = entries if n == 0 else entries[-n:]

    print(f"# {os.path.relpath(path, root)}")
    for h in header:
        print(h)
    filt = f"，--entity {args.entity}" if kw else ""
    print(f"# 顯示 {len(shown)} / {total} 筆{filt}"
          + ("" if len(shown) == total else f"（要全部加 --last 0）"))
    for e in shown:
        print(e)
    if not shown:
        print("# （沒有符合的紀錄" + ("；--entity 比對的是整行文字，換個關鍵字試試" if kw else "")
              + "。trace 有打開嗎？看檔頭 enabled=）")

    if snapshot and not args.no_snapshot:
        print(SNAPSHOT_MARKER)
        for s in snapshot:
            print(s)


def register(sub) -> None:
    pt = sub.add_parser(
        "fsm-trace",
        help="讀 FsmTrace dump（Library/FsmTrace/*.txt）：state 切換 + effect 命中紀錄（離線）",
        description="FSM state 切換 / Effect HitEnter / HitExit / HitBlocked 的時間軸。\n" + USAGE,
        epilog=USAGE,
    )
    pt.add_argument("name", nargs="?", help="dump 檔名（可只給前綴）；不給 = 最新那份")
    pt.add_argument("--entity", metavar="KW", help="只留含這段字的紀錄")
    pt.add_argument("--last", type=int, metavar="N", help="最後幾筆（預設 50，0 = 全部）")
    pt.add_argument("--no-snapshot", action="store_true", help="不印檔尾的條件快照")
    pt.add_argument("--list", action="store_true", help="列出所有 dump 檔")
    pt.set_defaults(fn=cmd)
