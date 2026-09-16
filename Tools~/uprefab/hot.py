"""「常被查的 prefab 有沒有寫進 skill」偵測 —— `up usage hot` 與 `prefab read` 尾端的提示。

背景：readcache（磁碟快取）在 2026-09-11 拆掉了。usage log 顯示同一支 prefab
（PPlayer、Character FSM 子樹…）一週內被十幾段調查反覆讀，但每次參數都不同，
快取接不住；真正的解法是把「入口路徑 + 常查子樹」寫進 skill，讓下一隻 agent
直接 `--node` 精準讀，而不是從 root 一層層摸。

這支只做兩件事：
1. `collect`：從 `.uprefab-usage.jsonl` 撈近 N 天、按 prefab 聚合「幾段調查、幾次讀、
   常查哪些子樹」，再對 skill 目錄做 stem 子字串比對判斷「skill 有沒有提到它」。
2. `hint`：`prefab read` 每次結束後對當下這支 prefab 查一次，夠熱又沒進 skill 就印一行
   提醒（工具層自我糾正 —— 不靠 agent 記得去跑 `up usage hot`）。

判斷用 stem（去掉 .prefab 的檔名）而不是全路徑：skill 裡常只寫檔名或 repo 相對路徑，
Packages/ 與 MonoFSM/ 兩種寫法也會互換。
"""

from __future__ import annotations

import collections
import re
import glob
import json
import os
import time

import usage

SKILL_GLOBS = (".claude/skills/**/*.md", "MonoFSM/skills/**/*.md", ".claude/agents/*.md")
DEFAULT_DAYS = 7
DEFAULT_MIN_SESSIONS = 3
# hint 只看 log 尾端這麼多 bytes，免得 read 每次都整份 2 MB 重 parse
TAIL_BYTES = 600_000
# 每份 skill 都會出現的 MonoFSM 通用節點名，單獨比對沒有鑑別度
_GENERIC_RE = re.compile(r"^(?:\[\w+\] )?(?:Context|StateFolder|VariableFolder|Modules|ViewRoot|"
                         r"ViewMeshRoot|Body|Root|Animator|PlayerInput)$")


def _stem(asset: str) -> str:
    return os.path.splitext(os.path.basename(str(asset)))[0]


def _skill_texts(root: str) -> list[tuple[str, str]]:
    out = []
    for pat in SKILL_GLOBS:
        for p in glob.glob(os.path.join(root, pat), recursive=True):
            try:
                with open(p, encoding="utf-8", errors="ignore") as fh:
                    out.append((os.path.relpath(p, root), fh.read()))
            except OSError:
                continue
    return out


def _mentions(stem: str, skills: list[tuple[str, str]]) -> list[str]:
    return [rel for rel, text in skills if stem and stem in text]


def _short(rel: str) -> str:
    """`.claude/skills/alishan-code-map/references/player.md` → `alishan-code-map/…/player.md`。"""
    parts = rel.split("/")
    return "/".join(parts[2:3] + ["…"] + parts[-1:]) if len(parts) > 3 else rel


def _missing_nodes(d: dict, skills: list[tuple[str, str]], min_count: int = 2) -> list[str]:
    """常查（≥ min_count 次）的子樹裡，末段節點名在任何 skill 都找不到的那些。

    「prefab 名在 skill 裡」不代表「agent 要的那顆子樹有入口」—— PPlayer 被 18 段調查
    讀 96 次就是這樣：skill 提到 PPlayer，但每次都得從 root 摸到 Character FSM 底下。
    """
    out = []
    for node, n in d["nodes"].most_common():
        if n < min_count:
            break
        segs = [re.sub(r"\[\d+\]$", "", x) for x in node.split("/")]  # StateFolder[0] → StateFolder
        leaf = segs[-1]
        if _GENERIC_RE.search(leaf) and len(segs) >= 2:
            # Context / StateFolder 這種名字每份 skill 都有，要跟上一層一起出現在同一份才算有入口
            parent = segs[-2]
            found = any(leaf in text and parent in text for _, text in skills)
        else:
            found = any(leaf in text for _, text in skills)
        if not found:
            out.append(node)
    return out


def _rows(root: str, days: float, tail_only: bool) -> list:
    path = os.path.join(root, usage.LOG_NAME)
    if not os.path.exists(path):
        return []
    since = time.time() - days * 86400
    rows = []
    with open(path, "rb") as fh:
        if tail_only:
            fh.seek(0, os.SEEK_END)
            size = fh.tell()
            fh.seek(max(size - TAIL_BYTES, 0))
            if size > TAIL_BYTES:
                fh.readline()  # 丟掉切在中間的半行
        for line in fh:
            try:
                r = json.loads(line)
            except Exception:
                continue
            if r.get("ts", 0) < since:
                continue
            a = r.get("args") or {}
            asset = a.get("asset") or a.get("path") or ""
            if not str(asset).endswith(".prefab"):
                continue
            rows.append(r)
    return rows


def collect(root: str, days: float = DEFAULT_DAYS, gap_sec: int = 900,
            tail_only: bool = False) -> list[dict]:
    rows = _rows(root, days, tail_only)
    if not rows:
        return []
    by_stem: dict[str, dict] = {}
    for sess in usage._sessions(rows, gap_sec):
        seen = set()
        for r in sess:
            a = r.get("args") or {}
            asset = str(a.get("asset") or a.get("path"))
            stem = _stem(asset)
            d = by_stem.setdefault(stem, {
                "stem": stem, "paths": collections.Counter(), "calls": 0,
                "reads": 0, "sessions": 0, "nodes": collections.Counter(),
                "out": 0,
            })
            d["paths"][asset] += 1
            d["calls"] += 1
            d["out"] += r.get("out", 0) or 0
            if r.get("cmd") == "prefab read":
                d["reads"] += 1
                node = (a.get("node") or "").strip("/")
                if node:
                    # 只留前三層當「入口」（PPlayer 的實測是 CharacterModules/Character FSM/<Context|StateFolder|VarFolder>
                    # 這一層才分得出 agent 在找什麼），再深的是該次調查的細節
                    d["nodes"]["/".join(node.split("/")[:3])] += 1
            if stem not in seen:
                seen.add(stem)
                d["sessions"] += 1
    skills = _skill_texts(root)
    out = []
    for d in by_stem.values():
        d["path"] = d["paths"].most_common(1)[0][0]
        d["in_skill"] = _mentions(d["stem"], skills)
        d["missing_nodes"] = _missing_nodes(d, skills) if d["in_skill"] else []
        out.append(d)
    out.sort(key=lambda d: (-d["sessions"], -d["reads"]))
    return out


def report(root: str, days: float = DEFAULT_DAYS, top: int = 15,
           min_sessions: int = DEFAULT_MIN_SESSIONS, gap_sec: int = 900) -> None:
    items = collect(root, days, gap_sec)
    if not items:
        print(f"# 近 {days:g} 天沒有針對 .prefab 的呼叫記錄")
        return
    hot = [d for d in items if d["sessions"] >= min_sessions]
    gaps = [d for d in hot if not d["in_skill"]]
    partial = [d for d in hot if d["in_skill"] and d["missing_nodes"]]
    print(f"# 近 {days:g} 天被 ≥{min_sessions} 段調查碰過的 prefab：{len(hot)} 支；"
          f"skill 完全沒提到 {len(gaps)} 支、提到了但常查子樹沒入口 {len(partial)} 支")
    print("\n| prefab | 段調查 | read 次 | 回到 context 字元 | skill | 缺入口的子樹 |")
    print("|---|---|---|---|---|---|")
    for d in hot[:top]:
        sk = "、".join(_short(s) for s in d["in_skill"][:2]) or "**（無）**"
        miss = len(d["missing_nodes"]) if d["in_skill"] else "-"
        print(f"| {d['stem']} | {d['sessions']} | {d['reads']} | {d['out']:,} | {sk} | {miss} |")
    if not gaps and not partial:
        print("\n# 熱門 prefab 與其常查子樹都已在 skill 裡。")
        return
    if gaps:
        print("\n## skill 完全沒提到（補「入口路徑 + 常查子樹」到 alishan-code-map 對應 reference）")
        for d in gaps[:top]:
            print(f"\n- `{d['path']}`  ← {d['sessions']} 段調查、{d['reads']} 次 read")
            for node, n in d["nodes"].most_common(4):
                print(f"  - 常查子樹 `--node \"{node}\"`（{n} 次）")
    if partial:
        print("\n## 提到了 prefab，但這些常查子樹沒有入口（agent 每次都從 root 往下摸）")
        for d in partial[:top]:
            print(f"\n- `{d['stem']}`（{'、'.join(_short(s) for s in d['in_skill'][:2])}）")
            for node in d["missing_nodes"][:5]:
                print(f"  - `--node \"{node}\"`（{d['nodes'][node]} 次）")
    print("\n# 寫法：只記「入口路徑 + 該子樹是什麼、為什麼會來查」，欄位值不要抄"
          "（`up fields` / `up peek` 查得到的東西不進 skill）。")


def hint(root: str, asset: str, min_sessions: int = DEFAULT_MIN_SESSIONS) -> str | None:
    """給 `prefab read` 收尾用：這支 prefab 近 7 天夠熱又沒進 skill 才回一行字。任何錯誤回 None。"""
    try:
        if not str(asset).endswith(".prefab"):
            return None
        stem = _stem(asset)
        for d in collect(root, DEFAULT_DAYS, tail_only=True):
            if d["stem"] != stem:
                continue
            if d["sessions"] < min_sessions:
                return None
            if not d["in_skill"]:
                return (f"# [hot] `{stem}` 近 {DEFAULT_DAYS} 天已被 {d['sessions']} 段調查讀過"
                        f"（{d['reads']} 次 read），但沒有任何 skill 提到它。這次讀完請把"
                        f"「入口路徑 + 你實際用到的子樹」補進 alishan-code-map 對應 reference；"
                        f"細節與常查子樹看 `up usage hot`")
            if d["missing_nodes"]:
                shown = "、".join(f"`{n}`" for n in d["missing_nodes"][:3])
                return (f"# [hot] `{stem}` 近 {DEFAULT_DAYS} 天被 {d['sessions']} 段調查讀過；"
                        f"skill 有提到它，但常查子樹 {shown} 沒有入口。這次若用到了，"
                        f"請把 `--node` 路徑與它是什麼補進 skill（`up usage hot` 看全表）")
            return None
        return None
    except Exception:
        return None
