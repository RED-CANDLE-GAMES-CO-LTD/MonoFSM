"""`up progress` —— 讀 GameProgress.md / 各模組 Progress.md 的最新條目。

存在的理由：這些檔是 append-only 的流水帳（最新在最下面），GameProgress.md 已經 90KB。
`tail -c 4000` 有三個問題：會切在句子中間、「檔案末端」不等於「最近寫的」（條目沒日期時
只有順序可靠）、而且每次都要 agent 自己記得那串 magic number。這支把「條目邊界」變成
一級概念，一步拿到 N 條完整紀錄。

刻意不做的：不切成週檔。條目速率不等速（一週可能 0 條也可能 15 條），切週會讓
「某件事在哪一週」變成搜尋問題，比單檔更慢。要縮檔案就用 `--archive N` 照條數搬。
"""

from __future__ import annotations

import os
import re
from dataclasses import dataclass

MAIN = "GameProgress.md"
# 條目邊界：col 0 的 `- ` 流水，或任何層級的 heading（h1 當檔頭不算條目）
_ENTRY_RE = re.compile(r"^(?:(#{2,6})\s+(.*)|-\s+(.*))$")
_DATE_RE = re.compile(r"(20\d\d)[-/](\d\d?)[-/](\d\d?)")
_SKIP_DIRS = {".git", "Library", "Temp", "Logs", "obj", "Build", "Builds", "node_modules"}


@dataclass
class Entry:
    ordinal: int      # 1-based，檔案順序 = 時間順序
    line: int         # 1-based 行號，方便直接跳去編輯
    title: str
    body: str         # 含標題行的完整原文
    date: str | None  # 抓得到才有


def _clean_title(raw: str, limit: int = 52) -> str:
    """把 markdown 記號剝掉當標題用。條目沒有 heading 時就拿首句頂上。"""
    # 不剝底線 —— 專案的序列化欄位一律 `_foo`，剝掉會讓標題認不出來
    t = re.sub(r"[`*\[\]]", "", raw).strip()
    t = re.split(r"[：:。\n]", t, 1)[0].strip()
    return t if len(t) <= limit else t[: limit - 1] + "…"


def parse(path: str) -> tuple[str, list[Entry]]:
    """回傳 (檔頭, 條目列表)。檔頭 = 第一條之前的內容（h1 標題那幾行）。"""
    with open(path, encoding="utf-8") as fh:
        lines = fh.read().splitlines()
    starts: list[int] = []
    for i, ln in enumerate(lines):
        if _ENTRY_RE.match(ln):
            starts.append(i)
    head = "\n".join(lines[: starts[0]]).rstrip() if starts else "\n".join(lines).rstrip()
    entries: list[Entry] = []
    for n, s in enumerate(starts):
        e = starts[n + 1] if n + 1 < len(starts) else len(lines)
        body = "\n".join(lines[s:e]).rstrip()
        m = _ENTRY_RE.match(lines[s])
        raw = m.group(2) if m.group(1) else m.group(3)
        d = _DATE_RE.search(lines[s])
        entries.append(Entry(
            ordinal=n + 1, line=s + 1, title=_clean_title(raw or ""), body=body,
            date=f"{d.group(1)}-{int(d.group(2)):02d}-{int(d.group(3)):02d}" if d else None,
        ))
    return head, entries


def find_files(root: str) -> list[str]:
    """所有 Progress.md（含 GameProgress.md），依修改時間新到舊。"""
    out = []
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in _SKIP_DIRS]
        for fn in filenames:
            if fn == MAIN or fn.lower() == "progress.md":
                out.append(os.path.relpath(os.path.join(dirpath, fn), root))
    out.sort(key=lambda p: -os.path.getmtime(os.path.join(root, p)))
    return out


def resolve(root: str, module: str | None, file: str | None) -> str:
    """把 --module / --file 解成一個實際路徑；歧義時直接把候選印出來當指引。"""
    if file:
        p = file if os.path.isabs(file) else os.path.join(root, file)
        if not os.path.exists(p):
            raise SystemExit(f"# 找不到 {file}\n# 現有的 progress 檔用 `up progress --module` 列")
        return p
    if module is None:
        p = os.path.join(root, MAIN)
        if not os.path.exists(p):
            raise SystemExit(f"# 找不到 {MAIN}；用 --file 指定，或 `up progress --module` 列出模組的")
        return p
    cands = [f for f in find_files(root) if f != MAIN]
    if module:
        hit = [f for f in cands if module.lower() in f.lower()]
        if len(hit) == 1:
            return os.path.join(root, hit[0])
        if not hit:
            raise SystemExit(
                f"# 沒有路徑含 '{module}' 的 Progress.md\n# 候選：\n"
                + "\n".join("#   " + f for f in cands[:30]))
        cands = hit
        print(f"# '{module}' 命中 {len(cands)} 份，要更明確：")
    else:
        print(f"# {len(cands)} 份模組 Progress.md（新到舊）：")
    raise SystemExit("\n".join(f"#   {f}" for f in cands[:40]))


def _fmt_head(e: Entry) -> str:
    return f"[{e.ordinal}] {e.date or '無日期'} L{e.line} {e.title}"


def cmd(args, root, cfg):
    path = resolve(root, args.module, args.file)
    rel = os.path.relpath(path, root)
    head, entries = parse(path)
    if not entries:
        raise SystemExit(f"# {rel} 沒有可辨識的條目（條目要以 col 0 的 `- ` 或 `## ` 開頭）")

    if args.archive:
        return _archive(path, rel, head, entries, args.archive)

    if args.stat:
        dated = [e for e in entries if e.date]
        size = os.path.getsize(path)
        print(f"# {rel}：{len(entries)} 條，{size:,} 字元，"
              f"平均 {size // len(entries):,} 字元/條")
        print(f"# 有日期的 {len(dated)} 條"
              + (f"（{dated[0].date} → {dated[-1].date}）" if dated else ""))
        print(f"# 最新：{_fmt_head(entries[-1])}")
        if size > 80_000:
            tail = f" --file {rel}" if rel != MAIN else ""
            print(f"# 檔案偏大，可用 `up progress --archive "
                  f"{len(entries) // 2}{tail}` 把最舊的一半搬去 Archive")
        return

    sel = entries
    if args.since:
        sel = [e for e in sel if e.date and e.date >= args.since]
        if not sel:
            raise SystemExit(f"# 沒有日期 >= {args.since} 的條目"
                             f"（{len(entries)} 條裡只有 "
                             f"{sum(1 for e in entries if e.date)} 條有日期）")
    if args.keyword:
        k = args.keyword.lower()
        sel = [e for e in sel if k in e.body.lower()]
        if not sel:
            raise SystemExit(f"# {rel} 裡沒有含 '{args.keyword}' 的條目")

    if args.list:
        print(f"# {rel}：{len(sel)} 條"
              + (f"（共 {len(entries)}）" if len(sel) < len(entries) else "") + "，舊 → 新")
        for e in sel:
            print(_fmt_head(e))
        print("# 展開某一條：`up progress --at <編號>`；展開全部命中：--full")
        return

    if args.at:
        by_ord = {e.ordinal: e for e in entries}
        miss = [n for n in args.at if n not in by_ord]
        if miss:
            raise SystemExit(f"# 沒有第 {miss} 條（{rel} 共 {len(entries)} 條，編號從 1 起）")
        sel = [by_ord[n] for n in args.at]
    elif args.keyword and not args.full:
        # 搜尋預設只回標題 —— 命中十幾條全展開就是幾萬字元
        print(f"# {rel} 含 '{args.keyword}'：{len(sel)} 條（只列標題，加 --full 展開）")
        for e in sel:
            print(_fmt_head(e))
        return
    elif not args.full:
        sel = sel[-args.limit:]

    print(f"# {rel}：第 {sel[0].ordinal}–{sel[-1].ordinal} 條 / 共 {len(entries)}")
    for e in sel:
        print(f"\n<!-- [{e.ordinal}] L{e.line} -->")
        print(e.body)
    if not args.keyword and not args.at and sel[0].ordinal > 1:
        print(f"\n# 更早的 {sel[0].ordinal - 1} 條：加 -n，或用 `up progress <關鍵字>` 搜尋")


def _archive(path: str, rel: str, head: str, entries: list[Entry], n: int):
    """把最舊的 n 條搬去 <stem>-Archive.md。條目順序即時間序，所以照數量切就夠。"""
    if n >= len(entries):
        raise SystemExit(f"# {rel} 只有 {len(entries)} 條，搬 {n} 條會清空整份")
    stem, ext = os.path.splitext(path)
    arch = f"{stem}-Archive{ext}"
    moved, kept = entries[:n], entries[n:]
    old = ""
    if os.path.exists(arch):
        with open(arch, encoding="utf-8") as fh:
            old = fh.read().rstrip() + "\n\n"
    else:
        old = f"# {os.path.basename(stem)} 封存\n\n"
    with open(arch, "w", encoding="utf-8") as fh:
        fh.write(old + "\n\n".join(e.body for e in moved) + "\n")
    with open(path, "w", encoding="utf-8") as fh:
        fh.write((head + "\n\n" if head else "") + "\n\n".join(e.body for e in kept) + "\n")
    print(f"# 搬走最舊的 {n} 條 → {os.path.relpath(arch, os.path.dirname(path))}")
    print(f"# {rel} 剩 {len(kept)} 條，{os.path.getsize(path):,} 字元")
    print(f"# 封存檔不進 `up progress` 的預設讀取；要查用 `up progress --file "
          f"{os.path.relpath(arch, os.path.dirname(path))} <關鍵字>`")
