"""`up session` —— 低成本翻閱舊 Claude Code session transcript。

存在的理由：transcript（`~/.claude/projects/<slug>/<uuid>.jsonl`）裡**純對話文字只佔 0–2%**，
其餘是 tool_use input / tool_result 與 meta（2026-09-10 實測三份 550–670KB 的 session，
對話文字 2–17KB）。所以翻舊 session 有兩條路都是錯的：
- `claude --resume`：全有全無，replay 100k+ tokens。
- 派 agent 讀全份：agent 一樣吃那 100k，再加它自己的 system prompt，總 token 比 resume 還貴
  （派 agent 是隔離、不是節省）。
過濾後直讀主線只要幾 k tokens。這支把「只留對話」變成預設。

分層：
- `up session`（= `list`）：列 session（起訖時間／大小／標題）。標題優先用 Claude Code 自己寫的
  `ai-title`，沒有就拿第一句 user prompt。
- `up session <uuid前綴>`：只吐 user text + assistant text + tool_use 一行摘要（名稱 + description）。
  tool 摘要留著是因為它是敘事骨架（「查了 X → 讀了 Y → 改了 Z」），每行 < 100 字元，值得。
- `--files`：只吐 Edit / Write / NotebookEdit 動過的路徑 + Bash 裡看起來是寫入的 `up` 指令。
- `--full`：含 tool_use input 與 tool_result 全文（每塊截到 --clip）。這才是原料，通常該派 agent。
- `--agent <id前綴>`：讀該 session 派出去的 subagent transcript（同一套過濾）。

刻意不做的：不建索引。344 份檔 250MB，list 只解析每行的前綴就夠（ai-title / 第一句 user），
實測 < 2s；建索引要處理 mtime 失效與新 session 追加，不值得。
"""

from __future__ import annotations

import json
import os
import re
from dataclasses import dataclass, field
from datetime import datetime, timezone

PROJECTS_DIR = os.path.expanduser("~/.claude/projects")
_PATH_RE = re.compile(r"[\w./\-\[\]() ]+?\.(?:cs|prefab|unity|asset|md|py|sh|json|asmdef|mat)\b")
# Bash 裡「看起來會寫入」的 up 子指令。read/peek/find 不算。
_UP_WRITE_RE = re.compile(
    r"\bup\s+(?:--root\S*\s+)?(?:prefab|scene|asset|so)\s+"
    r"(?:edit|set|add|remove|rm|mv|create|new|swap|apply|revert|variant|copy|clone)\b")


def slug_for(root: str) -> str:
    """Claude Code 用 cwd 的絕對路徑、`/` 換 `-` 當專案資料夾名。"""
    return os.path.abspath(root).replace("/", "-")


def project_dir(root: str) -> str:
    d = os.path.join(PROJECTS_DIR, slug_for(root))
    if not os.path.isdir(d):
        cands = sorted(os.listdir(PROJECTS_DIR)) if os.path.isdir(PROJECTS_DIR) else []
        hint = "\n".join(f"    {c}" for c in cands[:12])
        raise SystemExit(f"# 找不到 transcript 資料夾 {d}\n# 現有的專案資料夾：\n{hint}")
    return d


def _local(ts: str | None) -> datetime | None:
    if not ts:
        return None
    try:
        return datetime.fromisoformat(ts.replace("Z", "+00:00")).astimezone()
    except ValueError:
        return None


def _fmt_dt(dt: datetime | None, with_date: bool = True) -> str:
    if not dt:
        return "?"
    return dt.strftime("%m-%d %H:%M" if with_date else "%H:%M")


def _human(n: int) -> str:
    for unit in ("B", "K", "M"):
        if n < 1024:
            return f"{n:.0f}{unit}"
        n /= 1024
    return f"{n:.0f}G"


def _one_line(s: str, limit: int) -> str:
    s = " ".join(s.split())
    return s if len(s) <= limit else s[: limit - 1] + "…"


def _strip_reminders(text: str) -> str:
    """去掉 <system-reminder>…</system-reminder> 這類注入區塊，只留使用者真的打的字。"""
    return re.sub(r"<system-reminder>.*?</system-reminder>", "", text, flags=re.S).strip()


# ---------------------------------------------------------------- list


@dataclass
class Info:
    sid: str
    path: str
    size: int
    start: datetime | None = None
    end: datetime | None = None
    title: str = ""
    first_prompt: str = ""
    turns: int = 0
    agents: int = 0


def _tail_timestamp(path: str) -> datetime | None:
    """最後一個帶 timestamp 的行。從尾端讀 64KB 就夠，不必整份掃。"""
    size = os.path.getsize(path)
    with open(path, "rb") as fh:
        fh.seek(max(0, size - 65536))
        chunk = fh.read().decode("utf-8", "replace")
    for m in reversed(list(re.finditer(r'"timestamp":\s*"([^"]+)"', chunk))):
        dt = _local(m.group(1))
        if dt:
            return dt
    return None


def scan(path: str) -> Info:
    """只解析需要的行：ai-title 用字串前綴認，user 行只解析到抓到第一句 prompt 為止。"""
    sid = os.path.basename(path)[:-6]
    info = Info(sid=sid, path=path, size=os.path.getsize(path))
    with open(path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if line.startswith('{"type":"ai-title"') or line.startswith('{"type": "ai-title"'):
                try:
                    info.title = json.loads(line).get("aiTitle", "") or info.title
                except json.JSONDecodeError:
                    pass
                continue
            if '"type": "user"' not in line[:400] and '"type":"user"' not in line[:400]:
                continue
            try:
                o = json.loads(line)
            except json.JSONDecodeError:
                continue
            if o.get("type") != "user":
                continue
            text = _user_text(o)
            if not text:
                continue
            info.turns += 1
            if info.start is None:
                info.start = _local(o.get("timestamp"))
                info.first_prompt = text
    info.end = _tail_timestamp(path)
    agents_dir = os.path.join(os.path.dirname(path), sid, "subagents")
    if os.path.isdir(agents_dir):
        info.agents = sum(1 for n in os.listdir(agents_dir) if n.endswith(".jsonl"))
    return info


def _user_text(o: dict) -> str:
    """使用者親手打的那段。skill 展開、hook 注入、tool_result 都不算。"""
    if o.get("isMeta"):
        return ""
    origin = (o.get("origin") or {}).get("kind")
    c = (o.get("message") or {}).get("content")
    if isinstance(c, str):
        text = c
    elif isinstance(c, list):
        text = "\n".join(b.get("text", "") for b in c if b.get("type") == "text")
    else:
        return ""
    text = _strip_reminders(text)
    # 沒標 origin 的舊格式：靠內容判斷。task-notification / skill 展開都是系統塞的。
    if origin is None and text.startswith(("<task-notification>", "Base directory for this skill",
                                          "<local-command-", "<command-name>")):
        return ""
    if origin is not None and origin != "human":
        return ""
    return text


def cmd_list(args, pdir: str):
    files = [os.path.join(pdir, n) for n in os.listdir(pdir) if n.endswith(".jsonl")]
    files.sort(key=os.path.getmtime, reverse=True)
    rows: list[Info] = []
    for f in files:
        if args.date:
            # 用 mtime 先粗篩，省掉解析明顯不在那天的檔
            m = datetime.fromtimestamp(os.path.getmtime(f)).strftime("%Y-%m-%d")
            if m < args.date:
                continue
        info = scan(f)
        if args.date and not (info.start and info.start.strftime("%Y-%m-%d") == args.date):
            if not (info.end and info.end.strftime("%Y-%m-%d") == args.date):
                continue
        if args.keyword:
            k = args.keyword.lower()
            if k not in info.title.lower() and k not in info.first_prompt.lower():
                continue
        rows.append(info)
        if not args.date and not args.keyword and len(rows) >= args.limit:
            break
    if not rows:
        raise SystemExit("# 沒有符合的 session" + (f"（--date {args.date}）" if args.date else ""))
    rows.sort(key=lambda r: (r.start or datetime.fromtimestamp(0, timezone.utc)), reverse=True)
    print(f"# {os.path.relpath(pdir, PROJECTS_DIR)}：{len(rows)} 個 session，新 → 舊")
    for r in rows:
        title = r.title or _one_line(r.first_prompt, 60) or "(無 user 訊息)"
        span = f"{_fmt_dt(r.start)}→{_fmt_dt(r.end, with_date=False)}"
        extra = f" +{r.agents}agent" if r.agents else ""
        print(f"{r.sid[:8]}  {span}  {_human(r.size):>5}  {r.turns:>2}輪{extra}  {title}")
    print("# 讀一份：`up session <前綴>`；只看動過的檔：--files；含 tool 進出：--full（很肥，考慮派 agent）")


# ---------------------------------------------------------------- read


@dataclass
class Turn:
    n: int
    when: datetime | None
    user: str
    lines: list[str] = field(default_factory=list)  # assistant text / tool 摘要
    files: list[str] = field(default_factory=list)

    def text(self) -> str:
        return self.user + "\n" + "\n".join(self.lines)


def _resolve(pdir: str, prefix: str) -> str:
    hits = [n for n in os.listdir(pdir) if n.endswith(".jsonl") and n.startswith(prefix)]
    if len(hits) == 1:
        return os.path.join(pdir, hits[0])
    if not hits:
        raise SystemExit(f"# 沒有以 '{prefix}' 開頭的 session。先 `up session` 列出來再挑。")
    raise SystemExit("# 前綴不唯一：\n" + "\n".join(f"    {h[:-6]}" for h in hits[:10]))


def _resolve_agent(pdir: str, sid: str, prefix: str) -> tuple[str, str]:
    d = os.path.join(pdir, sid, "subagents")
    if not os.path.isdir(d):
        raise SystemExit(f"# 這個 session 沒有 subagent transcript（{d} 不存在）")
    hits = [n for n in os.listdir(d) if n.endswith(".jsonl") and n[6:].startswith(prefix)]
    if len(hits) != 1:
        raise SystemExit("# --agent 前綴要唯一，現有：\n" + "\n".join(
            f"    {_agent_line(d, n)}" for n in sorted(os.listdir(d)) if n.endswith('.jsonl')))
    return os.path.join(d, hits[0]), hits[0][6:-6]


def _agent_line(d: str, name: str) -> str:
    aid = name[6:-6]
    meta = {}
    mp = os.path.join(d, name[:-6] + ".meta.json")
    if os.path.exists(mp):
        try:
            with open(mp, encoding="utf-8") as fh:
                meta = json.load(fh)
        except (OSError, json.JSONDecodeError):
            pass
    size = _human(os.path.getsize(os.path.join(d, name)))
    return f"{aid}  {size:>5}  {meta.get('agentType', '?')}  {meta.get('description', '')}"


def _tool_summary(b: dict, clip: int) -> str:
    name = b.get("name", "?")
    inp = b.get("input") or {}
    if name in ("Edit", "Write", "NotebookEdit", "Read"):
        detail = inp.get("file_path") or inp.get("notebook_path") or ""
    elif name == "Bash":
        detail = inp.get("description") or inp.get("command", "")
    elif name == "Agent":
        detail = f"{inp.get('subagent_type', '')}: {inp.get('description', '')}"
    elif name == "Skill":
        detail = inp.get("skill", "")
    elif name == "SendMessage":
        detail = f"→{inp.get('to', '')}"
    else:
        detail = inp.get("description") or inp.get("query") or inp.get("pattern") or ""
    return f"  ⚙ {name}: {_one_line(str(detail), clip)}"


def _touched(b: dict) -> list[str]:
    name = b.get("name")
    inp = b.get("input") or {}
    if name in ("Edit", "Write", "NotebookEdit"):
        p = inp.get("file_path") or inp.get("notebook_path")
        return [p] if p else []
    if name == "Bash":
        cmd = inp.get("command", "")
        out = []
        for m in _UP_WRITE_RE.finditer(cmd):
            seg = cmd[m.start(): m.start() + 300]
            out.extend(p.strip() for p in _PATH_RE.findall(seg))
        # heredoc / redirect 寫檔也算
        for m in re.finditer(r"(?:>|>>|tee(?: -a)?)\s+(\S+\.(?:cs|md|py|sh|json))", cmd):
            out.append(m.group(1))
        return out
    return []


def parse_turns(path: str, clip: int, full: bool, root: str) -> list[Turn]:
    turns: list[Turn] = []
    cur: Turn | None = None
    with open(path, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            # assistant 行的 "type" 鍵排在 message 之後（行尾），不能靠前綴篩；
            # 單份檔 < 1MB，整行解析很便宜。meta 行（ai-title / attachment…）不以 parentUuid 開頭。
            if not line.startswith('{"parentUuid"'):
                continue
            try:
                o = json.loads(line)
            except json.JSONDecodeError:
                continue
            kind = o.get("type")
            if kind not in ("user", "assistant"):
                continue
            if kind == "user":
                text = _user_text(o)
                if text:
                    cur = Turn(n=len(turns) + 1, when=_local(o.get("timestamp")), user=text)
                    turns.append(cur)
                    continue
                if full and cur is not None:
                    c = (o.get("message") or {}).get("content")
                    if isinstance(c, list):
                        for b in c:
                            if b.get("type") == "tool_result":
                                cur.lines.append("  ↳ " + _one_line(_result_text(b), clip))
                continue
            if cur is None:
                # 沒有 user turn 就有 assistant（subagent transcript 開頭）：補一個空 turn
                cur = Turn(n=1, when=_local(o.get("timestamp")), user="(agent prompt 見 meta)")
                turns.append(cur)
            for b in (o.get("message") or {}).get("content") or []:
                t = b.get("type")
                if t == "text" and b.get("text", "").strip():
                    cur.lines.append(b["text"].strip())
                elif t == "tool_use":
                    cur.lines.append(_tool_summary(b, clip))
                    if full:
                        cur.lines.append("    " + _one_line(json.dumps(
                            b.get("input") or {}, ensure_ascii=False), clip * 3))
                    for p in _touched(b):
                        rp = os.path.relpath(p, root) if os.path.isabs(p) else p
                        if rp not in cur.files:
                            cur.files.append(rp)
    return turns


def _result_text(b: dict) -> str:
    c = b.get("content")
    if isinstance(c, str):
        return c
    if isinstance(c, list):
        return "\n".join(x.get("text", "") for x in c if isinstance(x, dict))
    return ""


def cmd_read(args, pdir: str, root: str):
    path = _resolve(pdir, args.target)
    sid = os.path.basename(path)[:-6]
    label = sid[:8]
    if args.agent is not None:
        if args.agent == "":
            d = os.path.join(pdir, sid, "subagents")
            if not os.path.isdir(d):
                raise SystemExit(f"# session {label} 沒有 subagent transcript")
            print(f"# session {label} 的 subagent：")
            for n in sorted(os.listdir(d)):
                if n.endswith(".jsonl"):
                    print("  " + _agent_line(d, n))
            print("# 讀一隻：`up session <前綴> --agent <id前綴>`")
            return
        path, aid = _resolve_agent(pdir, sid, args.agent)
        label = f"{label}/agent-{aid}"

    turns = parse_turns(path, args.clip, args.full, root)
    if not turns:
        raise SystemExit(f"# {label} 裡沒有可讀的對話")

    if args.grep:
        k = args.grep.lower()
        turns = [t for t in turns if k in t.text().lower()]
        if not turns:
            raise SystemExit(f"# {label} 沒有含 '{args.grep}' 的輪次")

    if args.files:
        seen: list[str] = []
        for t in turns:
            for f in t.files:
                if f not in seen:
                    seen.append(f)
        print(f"# {label} 動過的檔案（{len(seen)}，依首次出現順序）")
        for f in seen:
            print(f"  {f}")
        if not seen:
            print("  (無 Edit / Write / 寫入類 up 指令)")
        return

    total = len(turns)
    if args.limit and not args.grep and total > args.limit:
        turns = turns[-args.limit:]
    print(f"# {label}：第 {turns[0].n}–{turns[-1].n} 輪 / 共 {total}"
          + ("（--full）" if args.full else "（只留對話與 tool 摘要；原料加 --full）"))
    for t in turns:
        print(f"\n## [{t.n}] {_fmt_dt(t.when)} 👤")
        print(t.user if args.full else _one_line(t.user, args.clip * 6))
        for ln in t.lines:
            print(ln)
    if not args.grep and turns[0].n > 1:
        print(f"\n# 更早的 {turns[0].n - 1} 輪：加 -n，或 --grep <關鍵字>")


def cmd(args, root, cfg):
    pdir = project_dir(root)
    if not args.target or args.target.lower() == "list":
        return cmd_list(args, pdir)
    return cmd_read(args, pdir, root)
