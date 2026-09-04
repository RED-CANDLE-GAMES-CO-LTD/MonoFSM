#!/usr/bin/env python3
"""uprefab — Unity serialized data 的離線索引與查詢 CLI。

不需要 Unity Editor 執行中。用法見 `uprefab.py --help`。
"""

from __future__ import annotations

import argparse
import difflib
import json
import os
import re
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import indexer  # noqa: E402
import memo  # noqa: E402
import query  # noqa: E402
import readcache  # noqa: E402
import unity  # noqa: E402
import usage  # noqa: E402
from config import CONFIG_NAME, Config  # noqa: E402


def _emit(text: str) -> None:
    """印一段已經受 C# 端 charBudget 管的輸出。

    刻意不用 print()：HardCap 會截到剛好 charBudget（結尾已經有換行），print() 再補一個
    就變成 budget+1 —— 實測 35001/35000，害 usage 的「budget 超量」統計永遠歸不了零。
    """
    if text is None:
        return
    sys.stdout.write(text if text.endswith("\n") else text + "\n")


# --max-chars 攔截時要附的「怎麼縮小」建議。截斷本身不夠 —— agent 看到「被截斷」
# 而不知道下一步該打什麼，就會原封不動重打一次更貴的指令。
CAP_HINTS = {
    "find": "先看分佈用 --by-asset，或縮小 --comp / --name / --path。",
    "overrides": "先看分佈用 --by-target（一份大場景逐欄位列出是幾十萬字元），"
                 "再對單一 instance 用 -n 下鑽。",
    "catalog": "加關鍵字或 -n 縮小；要單一型別的完整欄位用 --type <型別>。",
    "cat": "加關鍵字或 -n 縮小；要單一型別的完整欄位用 --type <型別>。",
    "prefab read": "用 --node 指定子樹下鑽，或降 --budget。",
    "prefab locate": "縮小 --comp / --name，或降 -n。",
    "scene ls": "用 --node 指定子樹，或加 --structure-only。",
    "refs": "降 -n，或用 --comp 只看一顆 component。",
    "logs": "降 -n，或 --type Error 只看錯誤。",
    "fields": "改用 `up catalog --type <型別>` 只看語意與 tooltip。",
}


def find_root(start: str) -> str:
    """往上找 repo root（有 .uprefab.json 或 .git 的那層）。"""
    cur = os.path.abspath(start)
    while True:
        if os.path.exists(os.path.join(cur, CONFIG_NAME)) or os.path.isdir(
            os.path.join(cur, ".git")
        ):
            return cur
        parent = os.path.dirname(cur)
        if parent == cur:
            return os.path.abspath(start)
        cur = parent


def human(n: int) -> str:
    for unit in ("B", "K", "M", "G"):
        if n < 1024:
            return f"{n:.0f}{unit}"
        n /= 1024
    return f"{n:.0f}T"


def cmd_index(args, root, cfg):
    quiet = args.quiet
    last = [0]

    def progress(msg: str):
        if quiet:
            return
        last[0] += 1
        if msg.startswith("index ") and last[0] % 25:
            return
        print(f"  {msg}", file=sys.stderr)

    stats = indexer.build(root, cfg, incremental=not args.rebuild, progress=progress)
    print(
        f"indexed {stats['scanned']} assets ({stats['skipped']} unchanged) in {stats['seconds']}s\n"
        f"  nodes={stats['nodes']} comps={stats['comps']} "
        f"refs={stats['refs']} mods={stats['mods']}\n"
        f"  db: {os.path.join(root, indexer.DB_NAME)} "
        f"({human(os.path.getsize(os.path.join(root, indexer.DB_NAME)))})"
    )


def cmd_scope(args, root, cfg):
    con = indexer.connect(root)
    if args.action == "init":
        print("wrote", cfg.write_default())
        return
    if args.action == "list":
        print("include (full index):")
        for p in cfg.include:
            print("  ", p)
        print("includeShallow (型別與引用邊):")
        for p in cfg.include_shallow:
            print("  ", p)
        print("exclude:")
        for p in cfg.exclude:
            print("  ", p)
        print(f"scriptOnly: {cfg.script_only}")
        for k, v in cfg.scene_root_filter.items():
            print(f"sceneRootFilter {k}: {v.get('excludeRoots')}")
        return
    if args.action == "stats":
        print(f"{'tier':8} {'kind':8} {'assets':>7} {'bytes':>8} {'nodes':>8}")
        for tier, kind, cnt, size, nodes in query.scope_stats(con):
            print(f"{tier:8} {kind:8} {cnt:>7} {human(size or 0):>8} {nodes or 0:>8}")
        print("\n節點數最多的資產（考慮加進 exclude / sceneRootFilter）：")
        for path, size, nodes in query.biggest(con):
            print(f"  {nodes:>7} nodes  {human(size):>7}  {path}")


INHERIT_WARN = (
    "⚠ 離線索引只含每個檔案自己 YAML 寫出來的節點；prefab variant 繼承來的、"
    "nested prefab 內部的節點不在裡面。"
)
INHERIT_TIP = (
    "→ 要看合併後的真值（含繼承節點）用：up prefab locate <asset.prefab> "
    "--comp <型別> / --name <名稱>"
)


def _inherit_expand(con, args):
    """--path 指到 variant / 含 nested prefab 時，把 base 來源一起拉進查詢範圍。

    回 (paths, layers, notes)：
      paths  —— None = 不展開，維持原本的單一 LIKE 條件
      layers —— asset path → 來源層標籤（空字串 = 查詢對象本檔）
      notes  —— 一定要印出來的說明 / 警告
    """
    notes: list[str] = []
    if not args.path:
        return None, {}, notes
    direct = [row[0] for row in query.assets_matching(con, _like(args.path))]
    if not direct:
        notes.append(f"⚠ --path 沒有比對到任何已索引的資產（索引過期？先跑 up index）")
        return None, {}, notes
    if args.no_inherit:
        if query.has_instances(con, direct):
            notes.append("⚠ --no-inherit：查詢對象是 variant 或含 nested prefab，"
                         "只掃了本檔的節點，繼承節點未計入")
        return None, {}, notes
    if len(direct) > args.inherit_max:
        if query.has_instances(con, direct):
            notes.append(f"⚠ --path 命中 {len(direct)} 個資產（> --inherit-max "
                         f"{args.inherit_max}），未展開繼承鏈。{INHERIT_WARN}")
        return None, {}, notes

    sources = query.prefab_sources(con, direct)
    if not sources:
        return None, {}, notes
    layers = {p: "" for p in direct}
    for src, (depth, via) in sources.items():
        layers[src] = f"[繼承來源 L{depth}] ← {via}"
    notes.append(f"# 已沿 prefab 繼承鏈展開：{len(direct)} 個查詢對象 + "
                 f"{len(sources)} 個 base / nested 來源（--no-inherit 可關閉）")
    return direct + sorted(sources), layers, notes


def _no_match(notes, expanded: bool) -> None:
    """0 筆是最危險的輸出 —— 一定要講清楚「掃的範圍到哪」，不能讓 0 看起來像定論。"""
    print("(no match)")
    for n in notes:
        print(n)
    if not expanded:
        print(INHERIT_WARN)
    print(INHERIT_TIP)


def cmd_find(args, root, cfg):
    con = indexer.connect(root)
    paths, layers, notes = _inherit_expand(con, args)
    where = dict(comp=_like(args.comp), name=_like(args.name), path=_like(args.path),
                 scope=args.scope, paths=paths)

    def scope_note() -> str:
        if args.scope != "full":
            return ""
        all_where = dict(where, scope="all")
        all_total = query.find_count(con, **all_where)
        full_total = query.find_count(con, **where)
        hidden = all_total - full_total
        return (f"；另有 {hidden} 筆 shallow 命中（用 --scope all 顯示）"
                if hidden > 0 else "")

    for n in notes:
        print(n)

    if args.by_asset:
        groups = query.find_by_asset(con, limit=args.limit, **where)
        if not groups:
            _no_match([], bool(paths))
            return
        for apath, count in groups:
            tag = layers.get(apath, "")
            print(f"{count:5d}  {apath}" + (f"  {tag}" if tag else ""))
        total, assets = query.find_totals(con, **where)
        shown = sum(c for _, c in groups)
        more = f"（列出最多的 {len(groups)} 個 = {shown} 筆）" if assets > len(groups) else ""
        print(f"\n{total} match(es) 分佈在 {assets} 個資產{more}{scope_note()}")
        return

    rows = query.find(con, limit=args.limit, **where)
    if not rows:
        _no_match([], bool(paths))
        return

    # 命中被 limit 切掉時一定要講 —— 只印「50 match(es)」會被讀成「總共就這些」，
    # 接著做的分析（「這個 component 只有這幾處用到」）就整個是錯的。
    # **結論印在明細之前**：明細可能被 --max-chars 攔在中途，而表尾那句是唯一
    # 會改變結論的資訊，截掉它換來的是一次錯誤結論的重查。
    total = query.find_count(con, **where) if len(rows) >= args.limit else len(rows)
    cut = total > len(rows)
    if cut:
        print(f"# {len(rows)} / 共 {total} match(es) —— 被 -n {args.limit} 切掉了。"
              f"用 --by-asset 看分佈，或縮小條件{scope_note()}\n")

    resolved = _resolve_anchors(rows) if args.resolve else {}

    for apath, fid, npath, active, comps in rows:
        flag = "" if active else "~"
        anchor = query.anchor(apath, fid)
        tag = layers.get(apath, "")
        print(anchor + (f"   {tag}" if tag else ""))
        print(f"    {flag}{npath}  <{comps or ''}>")
        if args.resolve:
            status, payload, how = resolved.get(
                anchor, ("fail", "Unity 沒有回報這個 anchor", "")
            )
            if status == "ok":
                print(f"    --node {payload}" + (f"   [{how}]" if how else ""))
            else:
                print(f"    ✗ anchor 解不開：{payload}")

    if cut:
        print(f"\n{len(rows)} / 共 {total} match(es)（同上：被 -n {args.limit} 切掉了）")
        return
    print(f"\n{len(rows)} match(es){scope_note()}")


def _resolve_anchors(rows) -> dict:
    """anchor → (status, path 或原因, how)。一次來回解完所有命中，見 EditAnchor.Resolve。

    離線索引的節點路徑是局部的（variant 繼承來的父節點在本檔查不到、同名 sibling 沒有
    `[n]`），不能直接餵給 `--node`。要合併後的真值就只能問 Unity。
    """
    lines = []
    for apath, fid, npath, _active, _comps in rows:
        name = (npath or "").rsplit("/", 1)[-1]
        lines.append(f"{query.anchor(apath, fid)}|{name}")

    try:
        out = unity.call(f"{ANCHOR}.Resolve", "\n".join(lines))
    except unity.UnityError as e:
        # Unity 沒開 / 沒編譯過都在這裡 —— 不要吞掉，離線那半的輸出照樣印得出來
        print(f"# --resolve 失敗（Unity 端）：{e}", file=sys.stderr)
        return {}

    table = {}
    for line in out.splitlines():
        parts = line.split("\t")
        if len(parts) < 3:
            continue
        table[parts[0]] = (parts[1], parts[2], parts[3] if len(parts) > 3 else "")
    return table


# Unity 的 asset guid：32 位小寫 hex。也接受直接貼 Editor webhook 連結
# （http://localhost:8888/webhook?asset_guid=<guid>），從中抽出 guid。
GUID_RE = re.compile(r"[0-9a-f]{32}")
GID_RE = re.compile(r"GlobalObjectId_V1-\d+-[0-9a-fA-F]{32}-\d+-\d+")

# 掃 .meta 的 fallback 要跳過的目錄（Library 裡有大量重複的 meta 快取）
META_SKIP_DIRS = {"Library", "Temp", "Obj", "obj", "Build", "Builds", ".git", "node_modules"}


def _grep_meta(root: str, guid: str) -> str | None:
    """索引外的資產：直接掃 .meta 找 `guid: <guid>`，回傳去掉 .meta 的資產路徑。"""
    needle = f"guid: {guid}"
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in META_SKIP_DIRS]
        for fn in filenames:
            if not fn.endswith(".meta"):
                continue
            full = os.path.join(dirpath, fn)
            try:
                with open(full, encoding="utf-8", errors="ignore") as f:
                    # guid 在檔頭前幾行
                    for _ in range(4):
                        line = f.readline()
                        if not line:
                            break
                        if needle in line:
                            return os.path.relpath(full[: -len(".meta")], root)
            except OSError:
                continue
    return None


def cmd_guid(args, root, cfg):
    """guid ⇄ 資產路徑互查。token 可以是 guid、含 guid 的連結，或資產路徑。"""
    con = indexer.connect(root)
    token = args.token

    # scene 物件連結裡的 32 位 hex 是「那個 scene」的 guid，翻出來只會得到 scene 路徑，
    # 而使用者要的是那個節點 —— 直接轉手給 up obj，省一次「怎麼問不到」的來回。
    if GID_RE.search(token):
        print("# 這是 scene 物件連結（globalId），guid 那段只是所在的 scene。"
              "要看節點本身用：up obj '<連結>'", file=sys.stderr)

    m = GUID_RE.search(token.lower())

    if m and not os.path.splitext(token)[1]:
        # guid → path
        guid = m.group(0)
        row = query.asset_by_guid(con, guid)
        if row:
            path, kind, tier = row
            print(path)
            if args.verbose:
                print(f"# kind={kind} tier={tier} guid={guid}", file=sys.stderr)
            return
        print("# 索引裡沒有，掃 .meta…", file=sys.stderr)
        path = _grep_meta(root, guid)
        if path:
            print(path)
            if args.verbose:
                print("# (索引範圍外，見 .uprefab.json)", file=sys.stderr)
            return
        raise SystemExit(f"# 找不到 guid {guid}")

    # path → guid
    rows = query.guid_by_path(con, _like(token), limit=args.limit)
    if rows:
        for path, guid, kind in rows:
            print(f"{guid}  {path}" if args.verbose or len(rows) > 1 else guid)
        return
    meta = os.path.join(root, token + ".meta")
    if os.path.exists(meta):
        with open(meta, encoding="utf-8", errors="ignore") as f:
            for line in f:
                m = GUID_RE.search(line)
                if m and line.strip().startswith("guid:"):
                    print(m.group(0))
                    return
    raise SystemExit(f"# 找不到資產 {token}")


def cmd_overrides(args, root, cfg):
    con = indexer.connect(root)
    if args.by_target:
        _overrides_by_target(args, con)
        return
    # 雜訊會被摺疊成一行計數，所以先多撈一些再過濾
    rows = query.overrides(con, _like(args.asset), limit=args.limit * 20)
    if not rows:
        print("(no overrides)")
        return

    cur_inst = None
    cur_target = object()  # sentinel：讓第一個 target（可能是 None）也會印表頭
    shown = noise = 0
    pending_noise = 0

    def flush_noise():
        nonlocal pending_noise
        if pending_noise and cur_inst is not None:
            print(f"      … +{pending_noise} 個特效/曲線欄位（--all 顯示）")
            pending_noise = 0

    for apath, ifid, src, prop, value, tfid, tpath in rows:
        if not args.all and query.is_noise(prop):
            noise += 1
            pending_noise += 1
            continue
        if shown >= args.limit:
            break
        if (apath, ifid) != cur_inst:
            flush_noise()
            cur_inst, cur_target = (apath, ifid), object()
            print(f"\n{query.anchor(apath, ifid)}  ← {src or '(source 未索引)'}")
        if tpath != cur_target:
            flush_noise()
            cur_target = tpath
            print(f"  @ {tpath or f'fileID:{tfid}'}")
        print(f"      {prop} = {value}")
        shown += 1
    flush_noise()

    tail = f"（另有 {noise} 筆特效/曲線欄位已摺疊）" if noise and not args.all else ""
    total = query.overrides_count(con, _like(args.asset), noise=args.all)
    head = f"{shown} / 共 {total}" if total > shown else str(shown)
    cut = f"（-n {args.limit} 切掉了，用 --by-target 看分佈）" if total > shown else ""
    print(f"\n{head} override(s){cut}{tail}")


def _overrides_by_target(args, con):
    """只看分佈：一份大場景動輒幾千筆 override，逐欄位列出會是幾十萬字元。

    先看「改動集中在哪個 instance / 哪個節點」，再用 -n 對那一個下鑽，才是划算的順序。
    """
    rows = query.overrides_by_target(con, _like(args.asset), limit=args.limit,
                                     noise=args.all)
    if not rows:
        print("(no overrides)")
        return
    # SQL 是照筆數排的（要挑出最熱的那幾組），但同一個 instance 的列要黏在一起才讀得懂，
    # 所以這裡照「該 instance 的最高筆數」重排一次
    hot = {}
    for apath, ifid, _src, _tpath, count in rows:
        key = (apath, ifid)
        hot[key] = max(hot.get(key, 0), count)
    rows = sorted(rows, key=lambda r: (-hot[(r[0], r[1])], r[0], r[1], -r[4]))

    cur_inst = None
    for apath, ifid, src, tpath, count in rows:
        if (apath, ifid) != cur_inst:
            cur_inst = (apath, ifid)
            print(f"\n{query.anchor(apath, ifid)}  ← {src or '(source 未索引)'}")
        print(f"  {count:5d}  @ {tpath or '(root)'}")
    total = query.overrides_count(con, _like(args.asset), noise=args.all)
    print(f"\n共 {total} override(s)"
          f"（顯示 override 數最多的 {len(rows)} 組 instance×節點）")


# ---- 需要 Unity 開著的指令（走 uloop） ----
#
# 這些指令只是 SceneEdit / PrefabEdit / EditProbe 的一行入口。C# 那邊才是實作，
# 這裡的價值是把 execute-dynamic-code 的 JSON envelope 濾掉 —— 一次來回省十幾行雜訊。

SCENE = f"{unity.EDIT_NS}.SceneEdit"
PREFAB = f"{unity.EDIT_NS}.PrefabEdit"
ASSET = f"{unity.EDIT_NS}.AssetEdit"
PROBE = f"{unity.EDIT_NS}.EditProbe"
TRACE = f"{unity.EDIT_NS}.EffectTrace"
READER = f"{unity.EDIT_NS}.PrefabTextReader"
REFS = f"{unity.EDIT_NS}.EditRefs"
GID = f"{unity.EDIT_NS}.EditGid"
PROMPT = f"{unity.EDIT_NS}.PromptEdit"
LOC = f"{unity.EDIT_NS}.LocEdit"
ANCHOR = f"{unity.EDIT_NS}.EditAnchor"


def _ops_text(args, use_path: bool = True) -> str:
    """批次操作的來源：--file、位置參數，或 stdin。

    use_path=False 給 `asset do` —— 它的 `path` 是 assetPath 本身，被當成一行 op
    就會變成一條看不懂的錯誤。
    """
    if getattr(args, "file", None):
        with open(args.file, encoding="utf-8") as fh:
            return fh.read()
    # `scene do` 的第一個位置參數會被 `path`（new / open 用的）先吃掉，所以兩邊都撈
    inline = [v for v in ((getattr(args, "path", None) if use_path else None),
                          *getattr(args, "ops", ())) if v]
    if inline:
        return "\n".join(inline)
    if sys.stdin.isatty():
        raise SystemExit("沒有操作內容：用 -f <檔案>、直接帶參數，或從 stdin 餵進來")
    return sys.stdin.read()


def _probe_text(args) -> str:
    """peek-batch 的 probe 清單。格式：node|comp|members；-f - 代表 stdin。"""
    if not args.file:
        if sys.stdin.isatty():
            raise SystemExit("peek-batch 要 -f <probes.txt>，或用 -f - 從 stdin 讀")
        return sys.stdin.read()
    if args.file == "-":
        return sys.stdin.read()
    with open(args.file, encoding="utf-8") as fh:
        return fh.read()


def cmd_scene(args, root, cfg):
    a = args.action
    if a == "new":
        print(unity.call(f"{SCENE}.NewScene", args.path, args.defaults))
    elif a == "copy":
        print(unity.call(f"{SCENE}.CopyScene", args.template, args.path))
    elif a == "open":
        print(unity.call(f"{SCENE}.OpenScene", args.path))
    elif a == "save":
        print(unity.call(f"{SCENE}.Save"))
    elif a == "ls":
        _emit(unity.call(f"{SCENE}.Export", args.node, args.depth, args.full,
                         args.budget, args.structure_only))
    elif a == "count":
        print(unity.call(f"{SCENE}.Count", args.comp, args.name, args.sample))
    elif a == "do":
        print(unity.call(f"{SCENE}.Batch", _ops_text(args)))


def cmd_prefab(args, root, cfg):
    if args.action == "variant":
        print(unity.call(f"{PREFAB}.CreateVariant", args.asset, args.out, args.name))
    elif args.action == "copy":
        print(unity.call(f"{PREFAB}.CopyAsset", args.asset, args.out, args.name))
    elif args.action == "read":
        _prefab_read(args, root)
    elif args.action == "peek":
        if not args.comp:
            # 原本只回一行「要 --comp」—— 那趟 Unity 來回完全白跑（usage log 19 次）。
            # 下一步一定是「先看這節點上有什麼」，就順手回答掉。
            print(unity.call(f"{PROBE}.ComponentNames", args.asset, args.node or ""))
            return
        print(unity.call(f"{PROBE}.PeekAsset", args.asset, args.node, args.comp, args.members))
    elif args.action == "peek-batch":
        print(unity.call(f"{PROBE}.PeekAssetBatch", args.asset, _probe_text(args)))
    elif args.action == "locate":
        if not args.comp and not args.name:
            raise SystemExit("locate 至少要 --comp <component> 或 --name <節點名稱>")
        out = unity.call(f"{PROBE}.LocateAsset", args.asset, args.comp,
                         args.name, args.members, args.limit)
        print(out)
        # locate 走 LoadPrefabContents，看到的是合併後的真值 —— 明講這件事，
        # 免得 total=0 被拿去跟離線 find 的 (no match) 混為一談
        if "# total=0" in (out or ""):
            print("# total=0 是合併後的結果（已含 variant 繼承與 nested prefab 節點），"
                  "在這個 prefab 內可視為定論")
    elif args.action == "do":
        print(unity.call(f"{PREFAB}.Batch", args.asset, _ops_text(args), args.quiet))


def _prefab_read(args, root):
    """prefab read 的唯一出入口 —— 中間夾一層以檔案 mtime 為 key 的磁碟快取。

    只有 read 值得快取：它是唯一「純讀、輸出很肥、同一份東西會被反覆問」的 action。
    key 算不出來時（readcache 回 None）就退化成沒有快取的原本行為。

    **預設開啟**（原本要顯式 `--cache`，實測 415 次只有 21 次命中 = 5%，因為沒人記得加）。
    正確性靠 readcache 的 dep mtime + 匯出工具指紋，不是靠使用者記得加旗標；
    真的怕（剛在 Inspector 改過還沒存檔）就 `--no-cache`。
    """
    full_expand = args.full
    params = {"asset": args.asset, "node": args.node, "depth": args.depth,
              "budget": args.budget, "fsm": args.fsm, "fsm_only": args.fsm_only,
              "structure_only": args.structure_only, "full": full_expand}
    use_cache = not args.no_cache
    key = readcache.key_for(root, args.asset, params) if use_cache else None

    if key:
        cached = readcache.load(root, key)
        if cached is not None:
            usage.note("cache", "hit")
            print(readcache.HIT_NOTE)
            _emit(cached)
            return
        # 第二層：同一支 prefab 已經有祖先節點的完整子樹在快取裡 → 本地裁出來
        sliced = readcache.slice_for(root, args.asset, params)
        if sliced is not None:
            text, src = sliced
            usage.note("cache", "slice")
            print(f"# [cache] 本地切片：從已快取的 {src} 子樹裁出（該段沒有任何摺疊標記）。"
                  "唯一與直接 read 的差別：指到這顆子樹外面的引用會是 `@../..` 相對路徑，"
                  "而不是 `res:<asset>#Type` —— 資訊更多不是更少。要重問 Unity 加 --no-cache")
            print(f"# subtree: {args.node}")
            _emit(text)
            # 存成這組參數自己的 key，下次就是第一層命中
            readcache.store(root, key, text, args.asset, params)
            return

    if args.no_cache:
        usage.note("cache", "bypass")
    elif not key:
        usage.note("cache", "unavailable")
    else:
        usage.note("cache", "miss")
    text = unity.call(f"{READER}.Export", args.asset, args.node, args.depth,
                      full_expand, args.budget, args.fsm, args.fsm_only,
                      args.structure_only)
    _emit(text)
    if use_cache and key:
        readcache.store(root, key, text, args.asset, params)


def cmd_asset(args, root, cfg):
    """建立/編輯 ScriptableObject asset —— AssetEdit 的一行入口，理由同 cmd_scene/cmd_prefab：
    把 execute-dynamic-code 的 JSON envelope 濾掉，C# 那邊才是實作。"""
    a = args.asset_action
    if a == "create":
        print(unity.call(f"{ASSET}.CreateAsset", args.type, args.path, args.overwrite))
    elif a == "set":
        print(unity.call(f"{ASSET}.SetField", args.path, args.field, args.value))
    elif a == "set-ref":
        print(unity.call(f"{ASSET}.SetAssetRef", args.path, args.field, args.target))
    elif a == "add-element":
        print(unity.call(f"{ASSET}.AddArrayElement", args.path, args.field, args.elem_type))
    elif a == "invoke":
        print(unity.call(f"{ASSET}.Invoke", args.path, args.method))
    elif a == "fields":
        print(unity.call(f"{ASSET}.ListFields", args.path))
    elif a == "do":
        # 原子性：AssetEdit.Batch 任一行失敗就不 ApplyModifiedProperties，asset 不會半套
        print(unity.call(f"{ASSET}.Batch", args.path, _ops_text(args, use_path=False)))


def cmd_prompt(args, root, cfg):
    """幫一個 VarString 掛一組有條件的 localized 文字提示。

    這件事本來要跨 Localization 條目、value source 節點、條件 / token 子節點、Auto 綁定與
    Rename 四個系統，每次臨時寫 execute-dynamic-code 都要重踩同一批雷（m_KeyId 是 long、
    節點名含 `/`、{token} 沒開 IsSmart 不會展開）。實作在 C# 的 PromptEdit。

    條件與 token 都是「只補不刪」：對既有 source 下 case 不會動到人工掛好的 condition /
    token binding。`if:` 指的條件已存在（同 VarBool + 同 targetValue）就不重建；
    `prompt:` 的 token 名已存在就只更新資產、沒有同名的才新增。要清空重建才給
    --case-replace-conditions / --case-replace-tokens，而且會把移除的節點名印進報告。
    """
    if getattr(args, "check", False):
        # 只驗不改：手工組的（ConditionRef / SmartStringTokenBinding）Apply 蓋不到，
        # 但驗收一樣要看「每顆 source 組出什麼」＋「Token 檢查有沒有 ✗」
        print(unity.call(f"{PROMPT}.Check", args.asset, args.var_node, args.locale,
                         args.var_literal))
        return
    cases = "\n".join(args.case) if args.case else _cases_from_file(args)
    print(unity.call(
        f"{PROMPT}.Apply", args.asset, args.var_node, cases,
        args.locale, args.table, args.prune,
        args.case_replace_conditions, args.case_replace_tokens, args.var_literal))


def cmd_loc(args, root, cfg):
    """直接讀寫 string table 條目（文案的持有者是 SO 而不是節點時用）。"""
    print(unity.call(f"{LOC}.Set", args.table, args.key, args.text, args.locale,
                     bool(getattr(args, "smart", False))))


def _cases_from_file(args) -> str:
    if getattr(args, "file", None):
        with open(args.file, encoding="utf-8") as fh:
            return fh.read()
    if sys.stdin.isatty():
        raise SystemExit("沒有 case：用 --case 一條條給、-f <檔案>，或從 stdin 餵進來")
    return sys.stdin.read()


def cmd_refs(args, root, cfg):
    """引用反查。走 Unity 而不是離線 refs 表 —— 理由見 EditRefs 的類別註解：
    這個專案大量引用是 prefab override，離線 refs 表收不到。"""
    if args.asset:
        print(unity.call(
            f"{REFS}.PrefabRefs", args.asset, args.node, args.comp, args.out, args.limit))
    else:
        print(unity.call(f"{REFS}.SceneRefs", args.node, args.comp, args.out, args.limit))


FSM_KINDS = ("action", "condition", "render", "handler", "getter", "var")

CATALOG_KINDS = {"action": "Action", "condition": "Condition",
                 "render": "RenderBehaviour", "handler": "EventHandler",
                 "getter": "Getter / ValueSource", "var": "Var",
                 "so": "ScriptableObject"}


def _fmt_fields(raw: str, verbose=False) -> list[str]:
    """欄位壓成一行；[Auto] 系列標出來（那些不用在 prefab 上手填）。"""
    try:
        fields = json.loads(raw or "[]")
    except json.JSONDecodeError:
        return []
    if not fields:
        return []
    if verbose:
        out = []
        for f in fields:
            auto = f"[{f['auto']}] " if f["auto"] else ""
            tip = f"  — {f['tip']}" if f["tip"] else ""
            out.append(f"    {auto}{f['name']}: {f['type']}{tip}")
        return out
    parts = []
    for f in fields:
        auto = f"[{f['auto']}]" if f["auto"] else ""
        parts.append(f"{auto}{f['name']}:{f['type']}")
    return ["    " + "  ".join(parts)]


def _first_sentence(text: str, limit=100) -> str:
    """清單模式只給第一句 —— 完整說明用 --type / -v 看。"""
    m = re.search(r"^(.{10,%d}?[。．.！!？?])\s" % limit, text + " ")
    head = m.group(1) if m else text
    if len(head) > limit:
        head = head[:limit].rstrip() + "…"
    return head


def _print_catalog_row(row, verbose=False, show_path=False, compact=False):
    """compact = 大量列的瀏覽模式：有 summary 的只留一行，路徑另外折到表尾。

    欄位行佔了寬清單近半的字元，但對「⚠無說明」的列它是唯一的判斷依據，
    所以砍的是有說明那些的欄位行，不是全部。
    """
    cls, path, kind, bases, is_abs, is_obs, summary, has_doc, fields = row
    head = cls
    if is_abs:
        head += " (abstract)"
    if is_obs:
        head += " ⛔Obsolete"
    if summary:
        mark = "" if has_doc else " ~"  # ~ = 只有 // 註解，不是正式 doc
        body = summary if verbose else _first_sentence(summary, 80 if compact else 100)
        print(f"{head} ─{mark} {body}")
    elif compact and not show_path:
        print(f"{head} ⚠無說明")  # 路徑折到表尾
    else:
        print(f"{head} ⚠無說明  {path}")
    if verbose:
        print(f"    <{bases}>  {path}")
    elif show_path and summary:
        print(f"    {path}")
    if compact and summary:
        return  # 欄位用 --type / -v 取，這裡省掉
    for line in _fmt_fields(fields, verbose):
        print(line)


def _refresh_catalog(con, root):
    """查詢前把 catalog 對齊硬碟上的 .cs。

    離線索引最常見的錯誤回報是「改了 .cs 卻還顯示舊 summary」——
    沒改檔時這裡只花一次 os.walk，改了幾支就只重 parse 那幾支，
    比要求使用者記得 `up index` 可靠得多。壞掉不該擋住查詢，所以吞例外。
    """
    try:
        n = indexer.refresh_catalog(con, root)
        if n:
            print(f"# catalog 已自動更新（{n} 支 .cs 有變動）")
    except Exception as e:
        print(f"# catalog 自動更新失敗，資料可能過期：{e}")


def cmd_catalog(args, root, cfg):
    """列 Action / Condition 等型別的用途與欄位，免得為了挑 component 去讀 .cs。"""
    con = indexer.connect(root)
    _refresh_catalog(con, root)
    if con.execute("SELECT COUNT(*) FROM catalog").fetchone()[0] == 0:
        raise SystemExit("# catalog 是空的 —— 先跑 `up index`")

    if args.type:
        row = query.catalog_one(con, args.type)
        if not row:
            _, rows = query.catalog_list(con, keyword=args.type, include_abstract=True, limit=15)
            if not rows:
                raise SystemExit(f"# 沒有叫 '{args.type}' 的型別（名稱要跟檔名一致）")
            print(f"# 沒有精確叫 '{args.type}' 的型別，名稱相近的：")
            for r in rows:
                _print_catalog_row(r)
            return
        _print_catalog_row(row, verbose=True)
        return

    # `all` = 所有掛在 FSM 節點上的東西；ScriptableObject 數量級差一位數
    # 又多半是第三方 asset，要看得明確指定 `so`
    kinds = None if args.kind != "all" else FSM_KINDS
    kind = None if args.kind == "all" else args.kind
    # 不帶關鍵字的寬清單是最大宗的 token 消耗源（單次上萬字元），
    # 預設收斂成瀏覽用的精簡模式；-v / --path 代表使用者要細節，就不壓。
    compact = not args.keyword and not args.verbose and not args.path
    limit = args.limit if args.limit is not None else (200 if args.keyword else 60)
    total, rows = query.catalog_list(
        con, kind=kind, kinds=kinds, keyword=args.keyword, missing=args.missing,
        include_abstract=args.abstract, include_obsolete=args.obsolete,
        limit=limit)
    label = CATALOG_KINDS.get(kind, "FSM 節點型別")
    scope = f"{label}"
    if args.keyword:
        scope += f" 含 '{args.keyword}'"
    if args.missing:
        scope += "（缺說明）"
    shown = f"，顯示 {len(rows)}" if len(rows) < total else ""
    print(f"# {scope}：{total} 個{shown}")
    for r in rows:
        _print_catalog_row(r, verbose=args.verbose, show_path=args.path,
                           compact=compact)
    if compact:
        n_missing = sum(1 for r in rows if not r[6])
        if n_missing:
            print(f"# {n_missing} 個缺說明，路徑用 `--missing --path` 看")
        print("# 精簡模式：有說明的列已省略欄位，完整欄位用 `--type <型別>` 或 -v")
    if len(rows) < total:
        print(f"# … 還有 {total - len(rows)} 個，用 --limit 或加關鍵字縮小")


def cmd_types(args, root, cfg):
    print(unity.call(f"{PROBE}.Types", args.keyword, args.limit))


def cmd_fields(args, root, cfg):
    """Unity 端的欄位真值，前面補上離線 catalog 的用途說明與欄位 tooltip。

    只有型別名與欄位名時常常還是不知道要填什麼，說明與 [Tooltip] 才是關鍵，
    但那些只存在 .cs 裡 —— 這裡一起吐出來，省掉再去 Read 一次原始碼。
    欄位清單由 Unity 段負責，這裡只補它沒有的語意，不重印一遍。
    """
    try:
        con = indexer.connect(root)
        _refresh_catalog(con, root)
        row = query.catalog_one(con, args.type)
    except Exception:
        row = None
    if row:
        cls, path, kind, bases, is_abs, is_obs, summary, has_doc, fields = row
        print(f"# {cls}{' ⛔Obsolete' if is_obs else ''} <{bases}>  {path}")
        if summary:
            print(f"# {summary}")
        else:
            print("# ⚠ 這個型別沒有 /// summary —— 讀完原始碼後請順手補一行")
        for f in json.loads(fields or "[]"):
            if f["tip"] or f["auto"]:
                auto = f"[{f['auto']}] " if f["auto"] else ""
                tip = f" — {f['tip']}" if f["tip"] else ""
                print(f"#   {auto}{f['name']}{tip}")
        print()
    print(unity.call(f"{PROBE}.Fields", args.type, not args.own))


def cmd_obj(args, root, cfg):
    """吃一條 GlobalObjectId 連結，匯出它指的 scene 物件。

    連結是專案裡指涉 scene 節點的通用格式（BugReportUtility 產、貼給 Unity 就能跳），
    但它不含節點路徑，所以在這之前拿到連結等於什麼都拿不到。--locate 只回路徑與
    component 清單，接著能餵給 up scene ls / up refs 的 --node。
    """
    token = args.token
    if token == "-":
        token = sys.stdin.read()
    if not GID_RE.search(token):
        raise SystemExit(
            "# 這串裡沒有 GlobalObjectId_V1-… —— 期望貼上像\n"
            "#   [[Render] VerletRope](http://localhost:8888/webhook?globalId=GlobalObjectId_V1-2-<guid>-<id>-0)\n"
            "# 的連結（markdown、裸 URL、只有 id 都吃）")

    if args.locate:
        print(unity.call(f"{GID}.Locate", token, args.open, args.select))
        return
    _emit(unity.call(
        f"{GID}.Peek", token, args.node, args.depth, args.full,
        args.budget, args.fsm, args.open, args.select, args.fsm_only,
        args.structure_only))


def cmd_peek(args, root, cfg):
    if not args.comp:
        print(unity.call(f"{PROBE}.ComponentNames", "", args.node))
        return
    print(unity.call(f"{PROBE}.Peek", args.node, args.comp, args.members))


def cmd_effect_trace(args, root, cfg):
    """EffectHit 鏈路一次攤開：detector 偵測 → detectable dict → dealer 配對 → enterNode gate。

    逐段 peek 要十幾次來回，而每一段都可能靜靜地 return（沒有 log），
    所以這條鏈值得一個專用指令。
    """
    print(unity.call(f"{TRACE}.Trace", args.node, args.effect))


def cmd_poke(args, root, cfg):
    """Play Mode 下設一個 Var 的 runtime 值 —— peek 的寫入面，自動測試用。"""
    print(unity.call(f"{PROBE}.Poke", args.node, args.comp, args.value))


# logs 的兩道安全欄。`-n` 從 10 拉到 100（10 筆常常看不到真正的第一個錯），
# 代價是輸出可能爆掉，所以同時加：單則訊息上限 + 整體字元上限 + 相同訊息摺疊。
LOG_MSG_CLIP = 400
LOG_BUDGET = 8000


def cmd_logs(args, root, cfg):
    """Console 記錄的精簡版。原生 get-logs 每筆都帶一整份 JSON 欄位，
    實際要看的是「哪一行炸了」——所以只印訊息與（選配）前幾行 stack。

    **相同訊息摺疊成 xN**：一個 FixedUpdate 裡的 error 會每幀重印，逐筆列出來是同一句話
    幾十次（實測 6 次呼叫 162,625 字元）。要看的是「有幾種錯」，不是「印了幾次」。
    """
    call = ["get-logs", "--max-count", str(args.limit)]
    if args.type != "All":
        call += ["--log-type", args.type]
    if args.stack:
        call += ["--include-stack-trace", "true"]
    data = unity.run(call)
    logs = data.get("Logs") or []
    print(f"# {data.get('TotalCount', len(logs))} 筆（顯示 {len(logs)}），type={args.type}")

    groups = {}
    order = []
    for entry in logs:
        msg = str(entry.get("Message") or "").strip()
        key = (entry.get("Type"), msg[:200])
        if key in groups:
            groups[key]["n"] += 1
            continue
        groups[key] = {"n": 1, "msg": msg, "type": entry.get("Type"),
                       "stack": entry.get("StackTrace")}
        order.append(key)

    spent = 0
    for i, key in enumerate(order):
        row = groups[key]
        msg = row["msg"]
        if len(msg) > LOG_MSG_CLIP:
            msg = msg[:LOG_MSG_CLIP] + f"…（原長 {len(row['msg'])}）"
        count = f" x{row['n']}" if row["n"] > 1 else ""
        chunk = [f"[{row['type']}]{count} {msg}"]
        if args.stack and row["stack"]:
            for line in str(row["stack"]).strip().splitlines()[: args.stack]:
                chunk.append(f"    {line.strip()}")
        text = "\n".join(chunk)
        if spent + len(text) > LOG_BUDGET:
            print(f"# … 還有 {len(order) - i} 種訊息未列出（已到 {LOG_BUDGET} 字元）。"
                  "降 -n、或 --type Error 只看錯誤")
            break
        print(text)
        spent += len(text) + 1
    if len(order) < len(logs):
        print(f"# （{len(logs)} 筆摺成 {len(order)} 種）")


def cmd_clear(args, root, cfg):
    unity.run(["clear-console"])
    print("console 已清空")


def cmd_play(args, root, cfg):
    data = unity.run(["control-play-mode", "--action", args.action])
    print(json.dumps(data, ensure_ascii=False))


# ---- 錯誤路徑（大小寫不敏感 → near-match → 精簡 --help）----
#
# 1,040 次呼叫裡 116 次（11.2%）第一行就是錯誤或 usage，而 argparse 預設的錯誤輸出是
# 「一行訊息 + 一整份 usage」（實測 ~900 字元），那份 usage 幾乎沒有一次幫上忙 ——
# 真正需要的是「你打的那個字最接近哪個合法值」。

SCOPE_ACTIONS = ("list", "stats", "init")
FIND_SCOPES = ("full", "all", "shallow")
SCENE_ACTIONS = ("new", "copy", "open", "save", "ls", "count", "do")
PREFAB_ACTIONS = ("read", "peek", "peek-batch", "locate", "do", "variant", "copy")
CATALOG_KIND_CHOICES = ("action", "condition", "render", "handler", "getter",
                        "var", "so", "all")
LOG_TYPES = ("All", "Error", "Warning", "Log")
PLAY_ACTIONS = ("play", "stop", "pause")


def _ci(*choices):
    """argparse `type=`：把 enum 參數做大小寫不敏感的正規化。

    最便宜的一層修正 —— `up catalog Condition` 只差一個大寫就整條失敗，
    而那是 agent 最自然的寫法（型別名在程式碼裡就是大寫開頭）。
    認不出來的原樣回去，讓 choices 去報「最接近的是什麼」。
    """
    table = {str(c).lower(): c for c in choices}

    def conv(raw):
        return table.get(str(raw).lower(), raw)

    conv.__name__ = "choice"
    return conv


# option 字串 → 有這個旗標的子指令，供 near-match 用
_ALL_OPTS: dict = {}
_CHOICE_ERR = re.compile(
    r"argument ([^:]+): invalid choice: '(.*?)' \(choose from (.*)\)$", re.S)


def _compact_error(prog: str, parser, message: str) -> str:
    m = _CHOICE_ERR.match(message)
    if m:
        arg, bad, rest = m.groups()
        choices = re.findall(r"'([^']*)'", rest)
        near = difflib.get_close_matches(bad.lower(), [c.lower() for c in choices], 3, 0.4)
        tail = (f"最接近：{', '.join(near)}" if near
                else f"合法值：{', '.join(choices)}")
        return f"{prog}: '{bad}' 不是合法的 {arg}。{tail}"

    m = re.match(r"unrecognized arguments: (.*)$", message, re.S)
    if m:
        bad = m.group(1).split()
        hints = []
        for token in bad:
            if not token.startswith("-"):
                continue
            for cand in difflib.get_close_matches(token, list(_ALL_OPTS), 2, 0.6):
                owners = _ALL_OPTS[cand][:3]
                hints.append(f"{cand}（{'/'.join(owners)}）")
        return (f"{prog}: 不認得 {' '.join(bad)}"
                + (f"。最接近：{'、'.join(hints)}" if hints
                   else "。合法參數看 `up <子指令> --help`"))

    m = re.match(r"the following arguments are required: (.*)$", message, re.S)
    if m:
        need = m.group(1)
        return f"{prog}: 少了必填參數 {need}（用法看 `up {prog.split()[-1]} --help`）"

    return f"{prog}: {message}"


class _Parser(argparse.ArgumentParser):
    """把 argparse 的錯誤出口換成「一行訊息 + near-match」，不印整份 usage。"""

    def error(self, message):
        sys.stderr.write("# " + _compact_error(self.prog, self, message) + "\n")
        raise SystemExit(2)


def _pos_token(action) -> str:
    name = action.metavar or action.dest
    if action.nargs == "?":
        return f"[{name}]"
    if action.nargs in ("*", "..."):
        return f"[{name}…]"
    if action.nargs == "+":
        return f"<{name}…>"
    return f"<{name}>"


def _sub_help_lines(names, sp, one_liner) -> list:
    """一個子指令壓成 1–3 行：名稱 + 必填參數 + enum 合法值 + 旗標名。

    刻意保留 enum 合法值 —— 少列它會逼 agent 為了問「condition 還是 conditions」
    再叫一次 --help，那比多印幾十個字元貴得多。砍掉的是每個旗標的說明文字。
    """
    head = names[0] + (f"|{'|'.join(names[1:])}" if len(names) > 1 else "")
    pos, enums, flags = [], [], []
    for a in sp._actions:
        if a.dest == "help":
            continue
        if not a.option_strings:
            pos.append(_pos_token(a))
            if a.choices:
                enums.append(f"{a.metavar or a.dest}={'|'.join(map(str, a.choices))}")
        else:
            opt = max(a.option_strings, key=len)
            if a.choices:
                enums.append(f"{opt}={'|'.join(map(str, a.choices))}")
            else:
                flags.append(opt)
    out = [f"{head} {' '.join(pos)}".rstrip() + (f"   — {one_liner}" if one_liner else "")]
    if enums:
        out.append("    " + "  ".join(enums))
    if flags:
        out.append("    " + " ".join(flags))
    # asset 是唯一的兩層子指令。不展開的話「asset <asset_action>」等於什麼都沒說，
    # 逼人再叫一次 `up asset --help`（實測 5,312 字元）——那比在這裡多印十行貴。
    for a in sp._actions:
        if not isinstance(a, argparse._SubParsersAction):
            continue
        inner_help = {ca.dest: (ca.help or "") for ca in a._choices_actions}
        for name, isp in a.choices.items():
            ipos = [_pos_token(x) for x in isp._actions
                    if not x.option_strings and x.dest != "help"]
            iflags = [max(x.option_strings, key=len) for x in isp._actions
                      if x.option_strings and x.dest != "help"]
            line = f"      {names[0]} {name} {' '.join(ipos)}".rstrip()
            if iflags:
                line += " " + " ".join(iflags)
            tip = inner_help.get(name, "")
            out.append(line + (f"   — {tip}" if tip else ""))
    return out


def _compact_help(parser) -> str:
    subs = None
    for a in parser._actions:
        if isinstance(a, argparse._SubParsersAction):
            subs = a
    if subs is None:
        return argparse.ArgumentParser.format_help(parser)

    helps = {ca.dest: (ca.help or "") for ca in subs._choices_actions}
    groups, index = [], {}
    for name, sp in subs.choices.items():
        if id(sp) in index:
            groups[index[id(sp)]][0].append(name)
        else:
            index[id(sp)] = len(groups)
            groups.append(([name], sp))

    out = [
        "uprefab — Unity serialized data 的離線索引 / 查詢 / 編輯 CLI（慣例別名 up）。",
        "全域：--root PATH ｜ --max-chars N（輸出攔截，0=不限）｜ --no-memo（關掉 60 秒同指令 memo）",
        "enum 與子指令名大小寫不拘。`<>` 必填、`[]` 選填。每個子指令的完整說明用 "
        "`up <子指令> --help`。",
        "",
    ]
    for names, sp in groups:
        out += _sub_help_lines(names, sp, helps.get(names[0], ""))
    return "\n".join(out) + "\n"


class _TopParser(_Parser):
    def format_help(self):
        return _compact_help(self)


# 頂層帶值的旗標（正規化 argv 時要連值一起跳過）
_VALUE_FLAGS = {"--root", "--max-chars"}
_GLOBAL_FLAGS = {"--root", "--max-chars", "--no-memo"}


def _hoist_globals(argv: list) -> list:
    """把寫在子指令後面的全域旗標搬到最前面。

    argparse 的全域 optional 只認「子指令之前」的位置，而
    `up overrides X --no-memo` 是最自然的寫法 —— 不搬的話它會變成
    「不認得 --no-memo」，那正是這一則在修的錯誤類型。
    """
    head, rest, i = [], [], 0
    while i < len(argv):
        tok = argv[i]
        name = tok.split("=", 1)[0]
        if name in _GLOBAL_FLAGS:
            if "=" in tok or name not in _VALUE_FLAGS:
                head.append(tok)
                i += 1
            else:
                head += argv[i:i + 2]
                i += 2
            continue
        rest.append(tok)
        i += 1
    return head + rest


def _normalize_argv(argv: list, sub_names: dict, asset_names: dict) -> list:
    """子指令名做大小寫不敏感比對。argparse 的 subparsers 沒有這個開關，只能先改 argv。"""
    out = _hoist_globals(argv)
    i = 0
    while i < len(out):
        tok = out[i]
        if tok in _VALUE_FLAGS:
            i += 2
            continue
        if tok.startswith("-") and tok != "-":
            i += 1
            continue
        break
    if i >= len(out):
        return out
    canon = sub_names.get(out[i].lower())
    if canon:
        out[i] = canon
    if out[i] == "asset":
        j = i + 1
        while j < len(out) and out[j].startswith("-") and out[j] != "-":
            j += 1
        if j < len(out):
            canon2 = asset_names.get(out[j].lower())
            if canon2:
                out[j] = canon2
    return out


def _index_options(parser) -> None:
    for a in parser._actions:
        for opt in a.option_strings:
            _ALL_OPTS.setdefault(opt, ["全域"])
        if isinstance(a, argparse._SubParsersAction):
            for name, sp in a.choices.items():
                for sa in sp._actions:
                    for opt in sa.option_strings:
                        owners = _ALL_OPTS.setdefault(opt, [])
                        if name not in owners:
                            owners.append(name)


def _cap_for(args) -> int:
    """實際生效的字元上限。0 = 不限。

    `--budget 0`（Unity 端不限）現在只解除 C# 那一層 —— 要真的無上限得同時 `--max-chars 0`。
    反過來說，明確給了大 budget 的人不該被全域上限攔住，所以 cap 至少放到 budget+2000。
    """
    cap = max(0, int(getattr(args, "max_chars", 0) or 0))
    if cap == 0:
        return 0
    budget = getattr(args, "budget", None)
    if isinstance(budget, int) and budget > 0:
        cap = max(cap, budget + 2000)
    return cap


def _like(v: str | None) -> str | None:
    """沒帶萬用字元時自動包成 %v%，讓查詢預設是模糊比對。"""
    if v is None:
        return None
    return v if "%" in v else f"%{v}%"


def main() -> None:
    p = _TopParser(prog="uprefab", description=__doc__)
    p.add_argument("--root", default=".", help="repo root（預設往上自動尋找）")
    p.add_argument("--max-chars", type=int, default=30000,
                   help="整趟輸出的 hard cap（第二道網，攔截時會附原長與縮小建議）；0 = 不限")
    p.add_argument("--no-memo", action="store_true",
                   help="關掉「同 argv 60 秒內直接回上次結果」的 memo")
    sub = p.add_subparsers(dest="cmd", required=True, parser_class=_Parser)

    pi = sub.add_parser("index", help="建立/更新索引")
    pi.add_argument("--rebuild", action="store_true", help="忽略 mtime，全部重掃")
    pi.add_argument("-q", "--quiet", action="store_true")
    pi.set_defaults(fn=cmd_index)

    ps = sub.add_parser("scope", help="索引範圍管理")
    ps.add_argument("action", choices=SCOPE_ACTIONS, type=_ci(*SCOPE_ACTIONS))
    ps.set_defaults(fn=cmd_scope)

    pf = sub.add_parser("find", help="依 component / 名稱 / 路徑定位節點")
    pf.add_argument("--comp", help="component 型別（短名，模糊比對）")
    pf.add_argument("--name", help="GameObject 名稱")
    pf.add_argument("--path", help="資產路徑")
    pf.add_argument("--scope", choices=FIND_SCOPES, type=_ci(*FIND_SCOPES), default="full",
                    help="索引 tier；預設 full（--scope all 才包含供 override 解析的 shallow）")
    pf.add_argument("-n", "--limit", type=int, default=50)
    pf.add_argument("--no-inherit", action="store_true",
                    help="--path 指到 variant 時，不要把 base / nested prefab 來源"
                         "一起納入（預設會納入，並標示每筆命中來自哪一層）")
    pf.add_argument("--inherit-max", type=int, default=30,
                    help="--path 命中超過幾個資產就不展開繼承鏈（預設 30）")
    pf.add_argument("--by-asset", action="store_true",
                    help="只回「哪個資產各幾筆」的分佈，不逐節點列出")
    pf.add_argument(
        "--resolve",
        action="store_true",
        help="要 Unity 開著：把 anchor 解成合併後、可直接餵給 --node 的完整路徑",
    )
    pf.set_defaults(fn=cmd_find)

    pg = sub.add_parser("guid", help="guid ⇄ 資產路徑互查（吃 guid、webhook 連結或路徑）")
    pg.add_argument("token", help="guid / 含 guid 的連結 / 資產路徑（模糊比對）")
    pg.add_argument("-v", "--verbose", action="store_true", help="附 kind / tier / 路徑")
    pg.add_argument("-n", "--limit", type=int, default=20, help="路徑→guid 時的筆數上限")
    pg.set_defaults(fn=cmd_guid)

    po = sub.add_parser("overrides", help="prefab override 稽核")
    po.add_argument("asset", help="資產路徑（模糊比對）")
    po.add_argument("-n", "--limit", type=int, default=200)
    po.add_argument("--all", action="store_true", help="不摺疊特效/曲線等雜訊欄位")
    po.add_argument("--by-target", action="store_true",
                    help="只回「哪個 instance / 哪個節點各幾筆 override」的分佈，不列出欄位")
    po.set_defaults(fn=cmd_overrides)

    # ---- 需要 Unity 開著 ----

    pc = sub.add_parser("scene", help="對當前開著的 scene 讀 / 寫（需要 Unity）")
    pc.add_argument("action", choices=SCENE_ACTIONS, type=_ci(*SCENE_ACTIONS))
    pc.add_argument("path", nargs="?", help="new / copy / open 的 scene 路徑")
    pc.add_argument("--template", help="copy：來源模板 scene 路徑")
    pc.add_argument("--defaults", action="store_true", help="new：帶 Camera + Light")
    pc.add_argument("--node", help="ls：子樹路徑（留空只列 root 一層）")
    pc.add_argument("--depth", type=int, default=-1, help="ls：往下幾層")
    pc.add_argument("--budget", type=int, default=20000,
                    help="ls：總輸出 hard cap；0 = 明確允許無上限")
    pc.add_argument("--structure-only", action="store_true",
                    help="ls：只列結構與 component 名，不列 serialized 欄位")
    pc.add_argument("--full", action="store_true", help="ls：保留 Renderer/ParticleSystem/AudioSource/Light 與完整欄位、不摺疊已知子樹（預設會摺、會排除，省 token）")
    pc.add_argument("--comp", help="count：component 型別（含子類）")
    pc.add_argument("--name", help="count：名稱含這段")
    pc.add_argument("--sample", type=int, default=0, help="count：附幾筆樣本路徑")
    pc.add_argument("-f", "--file", help="do：從檔案讀批次操作")
    pc.add_argument("ops", nargs="*", help="do：直接帶操作（一個參數一行）")
    pc.set_defaults(fn=cmd_scene)

    pp = sub.add_parser("prefab", help="對 prefab asset 讀 / 寫（需要 Unity）")
    pp.add_argument("action", choices=PREFAB_ACTIONS, type=_ci(*PREFAB_ACTIONS))
    pp.add_argument("asset", help="prefab asset path")
    pp.add_argument("--node", help="read / peek：子樹路徑（peek 留空 = root）")
    pp.add_argument("--comp", help="peek：component 型別")
    pp.add_argument("--members",
                    help="peek：逗號分隔的欄位名；留空 = 這顆 component 的所有 serialize 欄位")
    pp.add_argument("--depth", type=int, default=-1,
                    help="read：最多往下幾層；仍受 --budget hard cap")
    pp.add_argument("--budget", type=int, default=20000,
                    help="read：字元上限，超標自動摺到塞得進的深度；0 = 不限")
    pp.add_argument("--fsm", action="store_true",
                    help="read：附 FSM markdown 段（states / transitions / conditions）")
    pp.add_argument("--fsm-only", action="store_true",
                    help="read：只輸出 FSM markdown，不輸出 hierarchy")
    pp.add_argument("--structure-only", action="store_true",
                    help="read：只列結構與 component 名，不列 serialized 欄位")
    pp.add_argument("--full", action="store_true", help="read：保留 Renderer/ParticleSystem/AudioSource/Light 與完整欄位、不摺疊已知子樹（預設會摺、會排除，省 token）")
    cache_group = pp.add_mutually_exclusive_group()
    cache_group.add_argument("--cache", action="store_true",
                             help="read：相容旗標；快取現在是預設開啟，不用加")
    cache_group.add_argument("--no-cache", action="store_true",
                             help="read：完全不讀也不寫快取（剛在 Inspector 改過還沒存檔時用）")
    pp.add_argument("--out", help="variant / copy：新 prefab 的 asset path")
    pp.add_argument("--name", help="variant / copy：root 名稱（預設用檔名）")
    pp.add_argument("-n", "--limit", type=int, default=20,
                    help="locate：命中筆數上限")
    pp.add_argument("-f", "--file", help="do：批次操作；peek-batch：probe 清單（- = stdin）")
    pp.add_argument("--quiet", action="store_true",
                    help="do：成功只回摘要、callback、save/verify；錯誤仍完整")
    pp.add_argument("ops", nargs="*", help="do：直接帶操作（一個參數一行）")
    pp.set_defaults(fn=cmd_prefab)

    pa = sub.add_parser("asset", help="建立/編輯 ScriptableObject asset（需要 Unity）")
    asub = pa.add_subparsers(dest="asset_action", required=True)

    pac = asub.add_parser("create", help="建一個 ScriptableObject asset")
    pac.add_argument("type", help="ScriptableObject 型別名（短名或 FullName）")
    pac.add_argument("path", help="assetPath，要以 Assets/ 開頭、.asset 結尾")
    pac.add_argument("--overwrite", action="store_true", help="已存在時覆蓋")

    pas = asub.add_parser("set", help="設定 asset 上 serialized 欄位的值（非物件引用）")
    pas.add_argument("path", help="assetPath")
    pas.add_argument("field", help="fieldPath，支援巢狀（如 _entries.Array.data[0]._family）")
    pas.add_argument("value")

    par = asub.add_parser("set-ref", help="欄位指向另一個 asset（SO / prefab / Texture2D / Sprite）")
    par.add_argument("path", help="assetPath")
    par.add_argument("field", help="fieldPath")
    par.add_argument("target", help="目標 asset 的路徑")

    paa = asub.add_parser("add-element", help="在陣列/List 欄位尾端加一個元素，回傳它的 index")
    paa.add_argument("path", help="assetPath")
    paa.add_argument("field", help="fieldPath")
    paa.add_argument("--type", dest="elem_type", default=None,
                     help="[SerializeReference] 陣列專用：新元素要塞的具體實作型別"
                          "（不給就是 null 元素）")

    pai = asub.add_parser("invoke", help="呼叫 asset 上一個無參數的 public 方法（按 Odin Button 用）")
    pai.add_argument("path", help="assetPath")
    pai.add_argument("method", help="方法名，例如 FindAllFlagsInProject")
    pai.set_defaults(fn=cmd_asset)

    paf = asub.add_parser("fields", help="列出 asset 上的 serialized 欄位（名稱 + 型別）")
    paf.add_argument("path", help="assetPath")

    pad = asub.add_parser(
        "do", help="一次跑多行欄位操作；任一行失敗就整批不套用（asset 完全不變）",
        description="一行一個操作，`#` 是註解。asset 沒有節點概念，第一個參數就是 fieldPath：\n"
                    "  set|<field>|<value>          設值\n"
                    "  aref|<field>|<assetPath>     欄位指向另一個 asset\n"
                    "  addel|<field>[|<type>]       陣列尾端加元素（type 只給 [SerializeReference]）\n"
                    "不收 invoke —— 那是反射呼叫方法、失敗回不去，放進批次是假的原子性。")
    pad.add_argument("path", help="assetPath")
    pad.add_argument("-f", "--file", help="從檔案讀（- 以外的路徑）")
    pad.add_argument("ops", nargs="*", help="直接帶操作（一個參數一行）")

    pa.set_defaults(fn=cmd_asset)

    pm = sub.add_parser(
        "prompt",
        help="幫 VarString 掛一組有條件的 localized 文字提示（需要 Unity）",
        description="case 格式：key|文案|spec;spec。"
                    "spec 是 `if:節點路徑=true|false` 或 `prompt:token=RMB`（token 可省，預設 key）。"
                    "文案留空 = 沿用 table 裡既有的。含 { 會自動開 IsSmart。"
                    "順序就是 sibling 順序 —— 有條件的排前面、無條件的墊底。")
    pm.add_argument("asset", help="prefab asset path")
    pm.add_argument("--var", dest="var_node", required=True,
                    help="VarString 節點路徑（value source 會掛在它底下）。"
                         "逃逸規則同 prefab read/do：`\\/` = 名字裡的斜線、`\\n` = 換行、"
                         "`\\\\` = 字面反斜線")
    pm.add_argument("--var-literal", action="store_true",
                    help="--var / if: 的路徑照字面比對，不做逃逸還原（名字裡有反斜線又懶得逃逸時用）。"
                         "代價：名字含 `/` 的節點在這個模式下指不到")
    pm.add_argument("--case", action="append",
                    help="一條提示，可重複給；順序 = 挑選優先序")
    pm.add_argument("--locale", default="zh-TW", help="要寫文案的 locale（預設 zh-TW）")
    pm.add_argument("--table", default="GameplayUI", help="string table collection（預設 GameplayUI）")
    pm.add_argument("--prune", action="store_true",
                    help="刪掉不在 --case 清單裡的既有 value source")
    pm.add_argument("--case-replace-conditions", action="store_true",
                    help="清空 source 底下既有的 VarBoolCompareCondition 再照 `if:` 重建。"
                         "預設是只補不刪（既有條件一律保留，if: 指的條件已存在就不動）")
    pm.add_argument("--case-replace-tokens", action="store_true",
                    help="清空 source 底下既有的 InputPromptTokenBinding 再照 `prompt:` 重建。"
                         "預設是只補不刪（同名的更新資產、沒有同名的才新增）")
    pm.add_argument("-f", "--file", help="從檔案讀 case（一行一條）")
    pm.add_argument("--check", action="store_true",
                    help="只驗不改：印出每顆 value source 組出的字串與 Token 檢查報告")
    pm.set_defaults(fn=cmd_prompt)

    pl = sub.add_parser(
        "loc",
        help="讀寫 string table 條目（需要 Unity）",
        description="文案持有者不是節點而是 ScriptableObject 時用這個（節點的走 up prompt）。"
                    "文案留空 = 只讀不寫。含 { 會自動開 IsSmart。")
    pl.add_argument("key", help="string table 的 key，不存在就建")
    pl.add_argument("text", nargs="?", default="", help="文案；留空 = 只讀出既有的")
    pl.add_argument("--locale", default="zh-TW", help="locale（預設 zh-TW）")
    pl.add_argument("--table", default="GameplayUI", help="string table collection（預設 GameplayUI）")
    pl.add_argument("--smart", action="store_true",
                    help="強制開 IsSmart（文案沒有 { 但同一組模板要靠 Smart String 串接時用）")
    pl.set_defaults(fn=cmd_loc)

    pr = sub.add_parser("refs", help="誰指向這個節點 / 它指向誰（需要 Unity）")
    pr.add_argument("asset", nargs="?",
                    help="prefab asset path；省略 = 對當前開著的 scene")
    pr.add_argument("--node", default="",
                    help="目標節點路徑（prefab 留空 = root；scene 第一段是 root object 名稱）")
    pr.add_argument("--comp", help="只看目標節點上的這個 component")
    pr.add_argument("--out", action="store_true",
                    help="反向：列出目標指向誰（預設是誰指向目標）")
    pr.add_argument("-n", "--limit", type=int, default=60)
    pr.set_defaults(fn=cmd_refs)

    pcat = sub.add_parser(
        "catalog", aliases=["cat"],
        help="Action / Condition 等型別的用途與 serialized 欄位（離線）")
    pcat.add_argument("kind", nargs="?", default="action",
                      choices=CATALOG_KIND_CHOICES, type=_ci(*CATALOG_KIND_CHOICES),
                      help="預設 action")
    pcat.add_argument("keyword", nargs="?", help="過濾型別名或說明")
    pcat.add_argument("--type", help="只看某一個型別（完整欄位 + tooltip）")
    pcat.add_argument("--missing", action="store_true", help="只列缺 /// summary 的（待補清單）")
    pcat.add_argument("--abstract", action="store_true", help="連 abstract 基底也列出")
    pcat.add_argument("--obsolete", action="store_true",
                      help="連 [Obsolete] 的也列出（預設隱藏，別挑到廢棄的）")
    pcat.add_argument("--path", action="store_true", help="每一列都附檔案路徑")
    pcat.add_argument("-v", "--verbose", action="store_true", help="展開每個欄位與 tooltip")
    pcat.add_argument("-n", "--limit", type=int, default=None,
                      help="預設：有 keyword 200，無 keyword 60")
    pcat.set_defaults(fn=cmd_catalog)

    pt = sub.add_parser("types", help="名稱含關鍵字的 Component 型別（需要 Unity）")
    pt.add_argument("keyword")
    pt.add_argument("-n", "--limit", type=int, default=40)
    pt.set_defaults(fn=cmd_types)

    pd = sub.add_parser("fields", help="某型別的可 serialize 欄位（需要 Unity）")
    pd.add_argument("type")
    pd.add_argument("--own", action="store_true", help="只看自己宣告的，不含繼承")
    pd.set_defaults(fn=cmd_fields)

    pob = sub.add_parser("obj", aliases=["gid"],
                         help="貼一條 GlobalObjectId 連結，匯出它指的 scene 物件（需要 Unity）")
    pob.add_argument("token",
                     help="含 GlobalObjectId 的文字：markdown 連結 / URL / 裸 id；`-` = 讀 stdin")
    pob.add_argument("--node", help="從命中的物件再往下鑽的相對路徑")
    pob.add_argument("--depth", type=int, default=-1,
                     help="最多往下幾層；仍受 --budget hard cap")
    pob.add_argument("--budget", type=int, default=20000,
                     help="字元上限，超標自動摺到塞得進的深度；0 = 不限")
    pob.add_argument("--fsm", action="store_true", help="附 FSM markdown 段")
    pob.add_argument("--fsm-only", action="store_true", help="只輸出 FSM markdown")
    pob.add_argument("--structure-only", action="store_true",
                     help="只列結構與 component 名，不列 serialized 欄位")
    pob.add_argument("--full", action="store_true", help="保留 Renderer/ParticleSystem/AudioSource/Light 與完整欄位、不摺疊已知子樹（預設會摺、會排除，省 token）")
    pob.add_argument("--locate", action="store_true",
                     help="只回節點路徑 + component 清單，不匯出子樹")
    pob.add_argument("--open", action="store_true",
                     help="物件所在 scene 沒開著時幫忙開（會換掉當前 scene；dirty 時拒絕）")
    pob.add_argument("--select", action="store_true", help="順便在 Unity 裡選中並 ping")
    pob.set_defaults(fn=cmd_obj)

    pk = sub.add_parser("peek", help="讀 scene 上某 component 的 runtime 值（需要 Unity）")
    pk.add_argument("node", help="節點路徑（第一段是 root object 名）")
    pk.add_argument("comp", nargs="?",
                    help="component 型別；留空 = 只列這個節點上有哪些 component")
    pk.add_argument("--members", help="逗號分隔的欄位/屬性名；留空 = 所有 public 屬性")
    pk.set_defaults(fn=cmd_peek)

    et = sub.add_parser("effect-trace",
                        help="診斷某個 EffectReceiver 為什麼沒觸發（需要 Unity，Play Mode 最有用）")
    et.add_argument("node", help="receiver 節點路徑，或它的任一祖先（會往下找 receiver）")
    et.add_argument("--effect", help="只看 effectType 名稱含這段的 receiver")
    et.set_defaults(fn=cmd_effect_trace)

    pke = sub.add_parser("poke", help="Play Mode 下設某個 Var 的 runtime 值（需要 Unity）")
    pke.add_argument("node", help="節點路徑（第一段是 root object 名）")
    pke.add_argument("comp", help="component 型別，例如 VarBool / VarFloat / VarInt")
    pke.add_argument("value", help="要設的值")
    pke.set_defaults(fn=cmd_poke)

    pl = sub.add_parser("logs", help="Console 記錄（精簡；需要 Unity）")
    pl.add_argument("--type", default="Error", choices=LOG_TYPES,
                    type=_ci(*LOG_TYPES))
    pl.add_argument("-n", "--limit", type=int, default=100,
                    help="抓最近幾筆（相同訊息會摺疊，所以 100 不等於 100 行）")
    pl.add_argument("--stack", type=int, nargs="?", const=6, default=0,
                    help="附前幾行 stack trace（預設 6）")
    pl.set_defaults(fn=cmd_logs)

    pcl = sub.add_parser("clear", help="清空 Console（需要 Unity）")
    pcl.set_defaults(fn=cmd_clear)

    py = sub.add_parser("play", help="Play Mode 控制（需要 Unity）")
    py.add_argument("action", choices=PLAY_ACTIONS, type=_ci(*PLAY_ACTIONS))
    py.set_defaults(fn=cmd_play)

    pu = sub.add_parser("usage", help="使用記錄統計（哪一步最花時間）")
    pu.add_argument("--gap", type=int, default=900,
                    help="間隔超過幾秒視為新的一段調查（預設 900）")
    pu.add_argument("--top", type=int, default=8)
    pu.add_argument("--since", type=float, metavar="HOURS",
                    help="只統計最近幾小時，避免舊版行為掩蓋新資料")

    _index_options(p)
    asset_names = {k.lower(): k for k in asub.choices}
    sub_names = {k.lower(): k for k in sub.choices}
    argv = sys.argv[1:]
    args = p.parse_args(_normalize_argv(argv, sub_names, asset_names))
    root = find_root(args.root)
    if args.cmd == "usage":
        usage.report(root, args.gap, args.top, args.since)
        return

    sub_cmd = usage._sub_cmd(args)
    cap = _cap_for(args)
    # memo：同 argv 60 秒內直接回上次結果。寫入類指令會 bump epoch 讓整批失效。
    no_memo = args.no_memo or os.environ.get("UPREFAB_NO_MEMO") == "1"
    memoizable = not no_memo and sub_cmd in memo.MEMOIZABLE
    if sub_cmd not in memo.MEMOIZABLE and sub_cmd not in memo.NEUTRAL:
        memo.bump(root)  # 寫入類：跑之前就失效，中途炸掉也不會留下可疑的 memo
    replay = memo.load(root, argv) if memoizable else None

    tee = usage.Tee(sys.stdout, cap, CAP_HINTS.get(sub_cmd, ""))
    if memoizable:
        tee.capture()
    sys.stdout = tee
    t0 = time.time()
    status = "ok"
    try:
        if replay is not None:
            usage.note("memo", "hit")
            tee.replay(replay)
        else:
            if cap and getattr(args, "budget", None) == 0:
                print(f"# --budget 0 只解除 Unity 端的上限；輸出仍會被 --max-chars "
                      f"{cap:,} 攔截。真的要無上限請同時給 --max-chars 0")
            args.fn(args, root, Config.load(root))
    except unity.UnityError as e:
        status = "unity-error"
        raise SystemExit(f"# Unity 呼叫失敗：{e}")
    except SystemExit as e:
        status = f"exit:{e.code}"
        raise
    except Exception:
        status = "error"
        raise
    finally:
        tee.finish()
        sys.stdout = tee._real
        usage.record(root, args, tee.chars,
                     int((time.time() - t0) * 1000), status, tee.head)
        if memoizable and replay is None and status == "ok":
            memo.store(root, argv, tee.text())


if __name__ == "__main__":
    main()
