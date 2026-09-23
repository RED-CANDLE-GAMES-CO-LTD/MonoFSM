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
import hot  # noqa: E402
import memo  # noqa: E402
import progress  # noqa: E402
import session  # noqa: E402
import query  # noqa: E402
import swapscript  # noqa: E402
import unity  # noqa: E402
import usage  # noqa: E402
import verifyskills  # noqa: E402
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
    "progress": "降 -n，或先用 --list 掃標題再用 --at 展開指定條。",
    "verify-skills": "降 -n，或用 --path 限縮到單一 skill 資料夾。",
    "session": "降 -n、加 --grep，或拿掉 --full（原料肥就該派 agent 讀，不要進主線）。",
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
GID_RE = re.compile(
    r"GlobalObjectId_V1-(\d+)-([0-9a-fA-F]{32})-(\d+)-(\d+)")

def _gid_offline(root: str, token: str):
    """不開 Unity 就把 GlobalObjectId 連結解成節點。回 dict，解不出來回 None。

    為什麼有這條路：`targetObjectId` 對「原生在該資產裡」的物件就是 YAML 的 fileID，
    離線索引裡就查得到。Unity 那條路要物件所在的 scene / prefab stage **正開著**
    才解得開（Unity 的限制），實務上最常拿到連結的時機恰好是它沒開著 ——
    那時整條連結等於廢的，這裡就是補這個洞。

    `targetPrefabId != 0` 表示物件在某個 prefab instance 內部，這時 fileID 屬於
    **來源 prefab**、要再套 instance 的 override 才是真值 —— 離線不猜，回 None 交給 Unity。
    """
    m = GID_RE.search(token or "")
    if not m:
        return None
    ident, guid, file_id, prefab_id = m.groups()
    if int(prefab_id) != 0:
        return None
    con = indexer.connect(root)
    row = query.node_by_file_id(con, guid.lower(), int(file_id))
    if not row:
        return None
    asset_path, node_path, name, is_active, comps, rooted = row
    return {
        "gid": m.group(0), "ident": int(ident), "guid": guid.lower(),
        "file_id": int(file_id), "asset": asset_path, "node": node_path,
        "name": name, "active": bool(is_active), "comps": comps,
        "rooted": rooted,
    }


def _gid_offline_text(off: dict) -> str:
    """離線解析的輸出。節點路徑可能是局部的（上層是 stripped instance 時接不回去），
    所以 anchor（`<資產>#<fileID>`）一定要一起印 —— 那個永遠精確，也能直接餵 up find。
    """
    is_prefab = off["asset"].endswith(".prefab")
    lines = [
        f"# gid: {off['gid']}",
        "# 這是離線索引解出來的（Unity 沒開 / 物件所在的 scene・prefab stage 沒開著）",
        f"# owner: {'prefab' if is_prefab else 'scene'} {off['asset']}",
        f"# anchor: {query.anchor(off['asset'], off['file_id'])}",
        f"{off['node'] or off['name']}",
        f"  <{off['comps']}>" if off["comps"] else "  <(無 component 記錄)>",
    ]
    if not off["active"]:
        lines[-1] += "  ~inactive"
    if not off["rooted"]:
        # 局部路徑餵 --node 一定解不開，所以這種情況只給 up find（離線、必中）
        lines.append("# ↑ 這條路徑是局部的（上層是 prefab instance，離線接不回 root）")
        lines.append(f"# 接著用：up find --path '{off['asset']}' --name '{off['name']}'")
    elif is_prefab:
        lines.append(f"# 接著用：up prefab read '{off['asset']}' --node '{off['node']}'")
    else:
        lines.append(f"# 接著用：up scene open '{off['asset']}' 之後 "
                     f"up scene ls --node '{off['node']}'")
    return "\n".join(lines) + "\n"


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
ASSETDEPS = f"{unity.EDIT_NS}.AssetDeps"
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



# ---- asset 路徑預解析 ----
# usage log 裡有 68 次「asset 路徑猜錯 → Unity 回一行 `# 找不到 prefab: X`、零候選」的白跑輪，
# 其中 32 次 basename 在離線 assets 表裡唯一命中。這層在打 Unity 之前先用索引把路徑修好，
# 或至少把候選列出來，讓下一次呼叫就是對的。

_PKG_MAP = None


def _pkg_map(root: str) -> dict:
    """Packages/manifest.json 的 `file:../<dir>` 條目 → {repo 相對資料夾: package 名}。
    索引存的是 repo 相對路徑（MonoFSM-Pro/…），Unity 只認 Packages/<name>/…，兩邊要能互換。"""
    global _PKG_MAP
    if _PKG_MAP is not None:
        return _PKG_MAP
    _PKG_MAP = {}
    manifest = os.path.join(root, "Packages", "manifest.json")
    try:
        with open(manifest, encoding="utf-8") as fh:
            deps = json.load(fh).get("dependencies", {})
        for name, src in deps.items():
            m = re.match(r"file:\.\./([^/]+(?:/[^/]+)*)/?$", str(src))
            if m and not src.endswith(".tgz"):
                _PKG_MAP[m.group(1)] = name
    except (OSError, ValueError):
        pass
    return _PKG_MAP


def _to_unity_path(root: str, path: str) -> str:
    for rel, name in _pkg_map(root).items():
        if path.startswith(rel + "/"):
            return f"Packages/{name}/" + path[len(rel) + 1:]
    return path


def _to_disk_path(root: str, path: str) -> str:
    for rel, name in _pkg_map(root).items():
        prefix = f"Packages/{name}/"
        if path.startswith(prefix):
            return rel + "/" + path[len(prefix):]
    return path


def _resolve_asset(root: str, path: str) -> str:
    """回傳可以直接餵給 Unity 的 asset 路徑；修不好就印候選並結束（不打 Unity）。

    順序：磁碟上有（含 repo 相對 ⇄ Packages/ 互換）→ 原樣或換成 Unity 路徑；
    沒有 → 索引 basename 唯一命中就代入；多筆列候選；只有相近名也列候選；
    完全沒有就留一行說明，仍交給 Unity 判（索引範圍見 .uprefab.json，剛建的檔可能還沒 index）。
    """
    if not path:
        return path
    disk = _to_disk_path(root, path)
    if os.path.exists(os.path.join(root, disk)) or os.path.exists(disk):
        unity_path = _to_unity_path(root, disk)
        if unity_path != path:
            print(f"# 已改用 {unity_path}（{path} 是 repo 相對路徑，Unity 只認 Packages/ 路徑）")
        return unity_path

    try:
        con = indexer.connect(root)
        all_paths = [r[0] for r in con.execute("SELECT path FROM assets")]
    except Exception:
        return path
    base = os.path.basename(path.rstrip("/"))
    stem, ext = os.path.splitext(base)
    # 這些指令只吃 prefab；同名的 .asset / .unity 代進去只會換到另一個「找不到」
    # （實測：GameplayUI.prefab 被換成 Localization/GameplayUI.asset），所以只認 .prefab
    want_ext = ".prefab"

    def _base(p):
        return os.path.basename(p)

    all_paths = [p for p in all_paths if p.lower().endswith(want_ext)]
    exact = [p for p in all_paths
             if os.path.splitext(_base(p))[0].lower() == stem.lower()]

    if len(exact) == 1:
        real = _to_unity_path(root, exact[0])
        print(f"# 已改用 {real}（原路徑不存在：{path}）")
        return real
    if len(exact) > 1:
        print(f"# 找不到 {path}，索引裡有 {len(exact)} 個同名資產，挑一個把路徑換掉重跑：")
        for p in exact[:10]:
            print(f"#   {_to_unity_path(root, p)}")
        raise SystemExit(2)

    stems = {}
    for p in all_paths:
        stems.setdefault(os.path.splitext(_base(p))[0].lower(), p)
    near = difflib.get_close_matches(stem.lower(), list(stems), 6, 0.6)
    if near:
        print(f"# 找不到 {path}，索引裡也沒有叫 '{base}' 的資產。名字相近的（挑一個換掉路徑重跑）：")
        for k in near:
            print(f"#   {_to_unity_path(root, stems[k])}")
        print("# 都不是的話：up find --path '<名稱片段>' 或 up guid '<名稱片段>'（剛改過檔名先 up index）")
        raise SystemExit(2)

    print(f"# 注意：磁碟上沒有 {path}，索引裡也沒有叫 '{base}' 的資產"
          "（索引範圍見 .uprefab.json；剛新建的檔先 up index）。"
          "若下面 Unity 也找不到，用 up find --path '<名稱片段>' 或 up guid '<名稱片段>' 找正確路徑")
    return path


def cmd_prefab(args, root, cfg):
    if args.action != "swap-script":  # swap-script 離線讀磁碟，要的是 repo 相對路徑
        args.asset = _resolve_asset(root, args.asset)
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
        print(unity.call(f"{PROBE}.PeekAsset", args.asset, args.node, args.comp,
                         args.members, args.deep))
    elif args.action == "peek-batch":
        print(unity.call(f"{PROBE}.PeekAssetBatch", args.asset, _probe_text(args), args.deep))
    elif args.action == "locate":
        if not args.comp and not args.name:
            raise SystemExit("locate 至少要 --comp <component> 或 --name <節點名稱>")
        out = unity.call(f"{PROBE}.LocateAsset", args.asset, args.comp,
                         args.name, args.members, args.limit, args.deep)
        print(out)
        # locate 走 LoadPrefabContents，看到的是合併後的真值 —— 明講這件事，
        # 免得 total=0 被拿去跟離線 find 的 (no match) 混為一談
        if "# total=0" in (out or ""):
            print("# total=0 是合併後的結果（已含 variant 繼承與 nested prefab 節點），"
                  "在這個 prefab 內可視為定論")
            if args.name:
                print(f"# --name 只給片段就好（substring），或用 glob：'*{args.name.strip('*')}*'；"
                      "整段比對時 [ ] 等符號都是字面值不用跳脫")
        elif not args.members and re.search(r"# total=[1-9]", out or ""):
            # usage log：locate → 同節點再 peek 有 50 對，而 405 次 locate 只有 17 次帶 --members
            print("# 要看欄位值直接在這條 locate 加 --members <欄位,欄位>（例：--members _note,CurrentValue），"
                  "一次拿完所有命中，不用再逐個 peek")
    elif args.action == "do":
        # --force 只在有帶時才多傳一個參數：沒帶就走舊的 3 參數 overload，
        # C# 端還沒 compile 到新 overload 時不會連一般的 do 都壞掉
        if args.force:
            print(unity.call(f"{PREFAB}.Batch", args.asset, _ops_text(args), args.quiet, True))
        else:
            print(unity.call(f"{PREFAB}.Batch", args.asset, _ops_text(args), args.quiet))
    elif args.action == "swap-script":
        _prefab_swap_script(args, root)


def _prefab_swap_script(args, root):
    """離線把某型別的 MonoBehaviour 換成另一個型別，同名 serialized 欄位原樣保留。

    走文字層而不是 Unity，是因為「C# 已經把欄位搬走」之後那些值在 Unity 端看不見了
    （載入時就被丟掉），只剩檔案裡還有 —— 細節見 swapscript.py 的模組註解。
    """
    if not args.src_type or not args.dst_type:
        raise SystemExit("swap-script 需要 --from <舊型別> --to <新型別>")
    path = os.path.join(root, args.asset) if not os.path.isabs(args.asset) else args.asset
    if not os.path.exists(path):
        raise SystemExit(f"找不到檔案：{args.asset}")

    con = indexer.connect(root)
    def one(name):
        rows = swapscript.lookup_script(con, name)
        if not rows:
            raise SystemExit(f"scripts 表裡沒有型別 {name}（先跑 up index）")
        if len(rows) > 1:
            paths = ", ".join(r[2] for r in rows)
            raise SystemExit(f"型別名 {name} 有多支同名 script：{paths}")
        return rows[0]

    from_guid, _, from_path = one(args.src_type)
    to_guid, to_ns, to_path = one(args.dst_type)
    to_full = f"{to_ns}.{args.dst_type}" if to_ns else args.dst_type
    editor_id = f"{args.assembly}::{to_full}"

    # ---- 前置檢查：Unity 開著這個專案時，離線改文字會被它的記憶體整份蓋掉 ----
    # 實際踩過：swap 完成後使用者在 Editor 存了一次這支 prefab，Unity 用 pre-swap 的
    # 記憶體覆寫檔案，而且因為 C# 上已經沒有那些欄位，序列化時直接不寫出來 = 值永久消失。
    lockfile = os.path.join(root, "Temp", "UnityLockfile")
    if os.path.exists(lockfile) and not args.dry_run and not args.force:
        raise SystemExit(
            "拒絕執行：Unity Editor 正開著這個專案（Temp/UnityLockfile 存在）。\n"
            "  離線改的是磁碟文字，Editor 記憶體裡還是舊的 —— 它下次存這支 prefab 就會整份覆寫，\n"
            "  而且 C# 上已刪掉的欄位會被序列化直接丟棄，值就永久沒了（實際發生過）。\n"
            "  三個選項，由上而下優先：\n"
            "  1) 改走 Unity 端寫入：`up prefab do <asset> 'comp|<node>|<新型別>' 'ref|…' 'set|…' "
            "'delcomp|<node>|<舊型別>'`\n"
            "     —— 記憶體與磁碟一致，之後怎麼存都不會丟。舊值先用 `--dry-run` 讀出來。\n"
            "  2) 關掉 Unity Editor 再跑這條指令。\n"
            "  3) 真的要在 Editor 開著時硬跑：加 --force，然後**立刻**切回 Editor 按 Ctrl+R "
            "reimport，\n"
            "     中間絕對不要在 Editor 裡動或存這支 prefab。")

    only = set(int(x) for x in args.fileid) if args.fileid else None
    drop = tuple(x.strip() for x in (args.drop or "").split(",") if x.strip())

    backup_dir = os.path.join(root, "Temp", "uprefab-swapscript-backup")
    hits = swapscript.swap(path, from_guid, to_guid, editor_id,
                           only_fileids=only, drop_fields=drop,
                           dry_run=args.dry_run, backup_dir=backup_dir)
    tag = "[dry-run] " if args.dry_run else ""
    print(f"# {tag}swap-script {args.src_type} → {args.dst_type}  ({args.asset})")
    if not hits:
        print("# 0 個 document 命中。可能原因："
              "(a) 這個型別在這份檔案裡是繼承來的（override 住在 m_Modifications，換不了，要去 base 改）"
              " (b) --from 型別名打錯 (c) --fileid 過濾掉了")
        return
    for did, go_id, go_name, kept, raw in hits:
        print(f"  &{did}  GameObject &{go_id} {go_name}")
        if args.dry_run:
            # dry-run 順便當「讀孤兒欄位」用 —— C# 已經刪掉的欄位 Unity 端看不到，
            # 值只剩檔案裡有，這是唯一讀得到的地方
            for ln in raw:
                print(f"      {ln}")
        else:
            print(f"      保留欄位: {', '.join(kept) if kept else '(無)'}")
    if drop:
        print(f"# 已刪除欄位: {', '.join(drop)}")
    if not args.dry_run:
        print(f"# {len(hits)} 個 document 已改寫，備份在 Temp/uprefab-swapscript-backup/")
        print("# ⚠ 立刻切回 Unity 按 Ctrl+R reimport —— 在那之前不要在 Editor 裡存這支 prefab，"
              "否則會被舊記憶體整份覆寫、值永久消失")
        print("# 下一步：Unity 端 reimport（切回 Editor 或 Ctrl+R）後用 "
              "`up prefab peek ... --comp {} --members ...` 驗值".format(args.dst_type))


def _prefab_read(args, root):
    """prefab read 的唯一出入口。

    2026-09-11 拆掉了磁碟快取（readcache）：實測 393 次讀取有 349 組不同參數，
    精確比對的 key 幾乎不會重複命中；而且 hit 與 miss 吐給 agent 的內容一模一樣，
    省的只是 Unity 一趟來回（中位 0.27 s），token 一個都沒省。「同一支 prefab 被
    反覆讀」的解法在 skill 層（`up usage hot`），不在快取層。`--cache` / `--no-cache`
    留成 no-op 免得舊 prompt 噴 argparse 錯誤。
    """
    text = unity.call(f"{READER}.Export", args.asset, args.node, args.depth,
                      args.full, args.budget, args.fsm, args.fsm_only,
                      args.structure_only)
    _emit(text)
    # 常被查卻沒進 skill 的 prefab，在這裡直接提醒 —— 不靠 agent 記得去跑 `up usage hot`
    tip = hot.hint(root, args.asset)
    if tip:
        print(tip)


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
    if getattr(args, "refs", False):
        return _loc_refs(args, root)
    print(unity.call(f"{LOC}.Set", args.table, args.key, args.text, args.locale,
                     bool(getattr(args, "smart", False))))


def _loc_refs(args, root):
    """誰引用這個 loc key。兩段式：
    1. Unity 解 key（前綴自動拆 / 近似候選，跟唯讀查詢同一套）→ 拿到 key id 與 table guid
    2. 離線文字預篩：`git grep -l -F` 在 Assets 的 .prefab/.unity/.asset 找 id 字串（含 override 的
       `value: <id>`），只把候選丟回 Unity 用 SerializedObject 確認並解成節點路徑。
    離線索引沒存 LocalizedString 的值（m_TableCollectionName 是 `GUID:` 字串不是 object ref，
    refs 表收不到），所以這裡用檔案文字預篩；結果一律由 Unity 端確認，不會把文字命中直接當答案。"""
    import subprocess
    res = unity.call(f"{LOC}.ResolveKey", args.table, args.key)
    first, _, rest = res.partition("\n")
    if not first.startswith("OK\t"):
        print(res.rstrip("\n"))
        return
    _, table, key, kid, tguid = first.split("\t")
    if rest.strip():
        print(rest.rstrip("\n"))
    t0 = time.time()
    pats = ["-e", kid, "-e", f"m_Key: {key}"]
    proc = subprocess.run(
        ["git", "grep", "-l", "-F", "--untracked", *pats, "--",
         "Assets/*.prefab", "Assets/*.unity", "Assets/*.asset", ":!Assets/Localization/*"],
        cwd=root, capture_output=True, text=True)
    if proc.returncode not in (0, 1):
        raise SystemExit(f"# git grep 失敗：{proc.stderr.strip()}")
    cands = [c for c in proc.stdout.splitlines() if c]
    grep_ms = int((time.time() - t0) * 1000)
    print(f"# 範圍：Assets/ 下全部 .prefab / .asset / .unity（含沒開的 scene，排除 Assets/Localization/）；"
          f"文字預篩 {grep_ms} ms → {len(cands)} 個候選")
    print(unity.call(f"{LOC}.Refs", table, key, int(kid), tguid, "\n".join(cands)).rstrip("\n"))
    print("# 只列寫下這個值的那一層；繼承它的 variant / nested instance 不另外列")


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
        ext = os.path.splitext(args.asset)[1].lower()
        if ext and ext not in (".prefab", ".unity"):
            raise SystemExit(
                f"# up refs 只查 prefab / scene 裡的節點，{args.asset} 不是。\n"
                f"# 你可能想要：up asset-refs '{args.asset}'（誰引用這顆 asset）"
                f" 或 up why-in-build '{args.asset}'（它為什麼進 build）")
        args.asset = _resolve_asset(root, args.asset)
        print(unity.call(
            f"{REFS}.PrefabRefs", args.asset, args.node, args.comp, args.out, args.limit))
    else:
        print(unity.call(f"{REFS}.SceneRefs", args.node, args.comp, args.out, args.limit))


def _asset_token(root: str, token: str) -> str:
    """asset-refs / why-in-build 的輸入：guid / webhook 連結 → guid；路徑 → Unity 路徑。
    存不存在交給 Unity 判（fbx / otf 這類不在離線索引範圍內）。"""
    if GID_RE.search(token):
        raise SystemExit("# 這是 scene 物件連結（globalId），不是 asset。要看節點用：up obj '<連結>'")
    m = GUID_RE.search(token.lower())
    if m and not os.path.splitext(token)[1]:
        return m.group(0)
    return _to_unity_path(root, token)


def cmd_asset_refs(args, root, cfg):
    """asset 層級反查：整個 Assets/ + Packages/ 裡誰直接引用它，scene / prefab 再列到欄位。"""
    print(unity.call(f"{ASSETDEPS}.AssetRefs", _asset_token(root, args.token),
                     args.limit, bool(args.all)).rstrip("\n"))


def cmd_why_in_build(args, root, cfg):
    """從 build root（scene / Resources / Preloaded / Addressables）BFS 找到它的最短鏈。"""
    print(unity.call(f"{ASSETDEPS}.WhyInBuild", _asset_token(root, args.token),
                     args.limit, bool(args.all)).rstrip("\n"))


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
    out = unity.call(f"{PROBE}.Fields", args.type, not args.own)
    print(out)
    if (out or "").startswith("# 找不到"):
        # Unity 端只給「名稱含這段」的候選，打錯字（VarFlaot）時一個都撈不到；
        # 這裡用離線 catalog / scripts 表補 near-match，省掉再開一輪 `up types` 猜
        try:
            names = {r[0] for r in con.execute("SELECT class FROM catalog")}
            names |= {r[0] for r in con.execute("SELECT class FROM scripts")}
        except Exception:
            names = set()
        lower = {n.lower(): n for n in names if n}
        near = difflib.get_close_matches(args.type.lower(), list(lower), 5, 0.6)
        if near:
            print(f"# 最接近：{', '.join(lower[n] for n in near)} —— 挑一個重跑 up fields <型別>")
        else:
            print("# 離線索引裡也沒有相近的名字。用 up types <關鍵字> 查型別名，"
                  "或 up catalog <kind> <關鍵字> 找用途相符的")


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

    # Unity 是主路：EditGid 對 prefab 直接掃 imported asset 比對 local fileID，不需要
    # 開 Prefab Stage，一次就把內容匯出。離線索引只在 Unity 根本沒開時當備援，
    # 只能回位置（欄位內容本來就得問 Unity）。
    try:
        if args.locate:
            out = unity.call(f"{GID}.Locate", token, args.open, args.select)
        else:
            out = unity.call(
                f"{GID}.Peek", token, args.node, args.depth, args.full,
                args.budget, args.fsm, args.open, args.select, args.fsm_only,
                args.structure_only)
    except unity.UnityError as e:
        off = _gid_offline(root, token)
        if not off:
            raise
        print(f"# Unity 沒回應（{e}）—— 只能用離線索引定位，欄位內容要等 Unity 開著", file=sys.stderr)
        _emit(_gid_offline_text(off))
        return
    _emit(out)


def cmd_peek(args, root, cfg):
    if not args.comp:
        print(unity.call(f"{PROBE}.ComponentNames", "", args.node))
        return
    print(unity.call(f"{PROBE}.Peek", args.node, args.comp, args.members, args.deep))


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
PREFAB_ACTIONS = ("read", "peek", "peek-batch", "locate", "do", "variant", "copy",
                  "swap-script")
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
        exact = []
        for token in bad:
            if not token.startswith("-"):
                continue
            if token in _ALL_OPTS:
                # 參數名對、子指令錯。原本印「不認得 --node。最接近：--node（scene/prefab/refs）」
                # —— 字面上自相矛盾，agent 實測去懷疑全形字元／引號，多打兩次 --help 才發現
                # 是 `up peek` 該換 `up prefab peek`。這種情況要直接說「這個子指令沒有它，
                # 它屬於哪些子指令」。
                owners = [o for o in _ALL_OPTS[token] if o != "全域"][:4]
                exact.append(f"{token} 不是 `{prog}` 的參數，它屬於：{'、'.join(owners)}")
                continue
            for cand in difflib.get_close_matches(token, list(_ALL_OPTS), 2, 0.6):
                owners = _ALL_OPTS[cand][:3]
                hints.append(f"{cand}（{'/'.join(owners)}）")
        if exact:
            # unrecognized arguments 是頂層 parser 丟的，prog 只會是 "uprefab"，
            # 子指令名要從 argv 撈（第一個不是 - 開頭的 token）
            sub = next((a for a in sys.argv[1:] if not a.startswith("-")), None) or prog
            msg = f"{prog} {sub}: " + "；".join(exact).replace(f"`{prog}`", f"`{sub}`")
            if sub == "peek":
                msg += ("。`up peek` 讀的是 scene 上的 runtime 值（node 是 positional）；"
                        "讀 prefab asset 的欄位用 `up prefab peek <asset> --node <路徑> --comp <型別> --deep`")
            return msg
        bare = [t for t in bad if not t.startswith("-")]
        sub = next((a for a in sys.argv[1:] if not a.startswith("-")), None)
        if bare and sub == "find":
            # `up find VerletRope` 這種裸字：find 沒有 positional，agent 實測連續三次撞牆
            # 才去翻 --help。直接把三個 selector 的用法印出來。
            return (f"{prog} find: 沒有 positional 參數，'{bare[0]}' 要指定是哪一種："
                    f"component 型別 → `up find --comp {bare[0]}`；"
                    f"GameObject 名稱 → `--name`；資產路徑片段 → `--path`"
                    f"（加 `--by-asset` 只看分佈）")
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


def _guard_gid_args(args) -> None:
    """GlobalObjectId 連結貼給了不吃連結的子指令 —— 直接說該打什麼，不要讓它跑下去。

    為什麼要攔：那些子指令會把連結當節點路徑 / 型別名 / 資產路徑用，出來的是
    「找不到 root object 'http:'」這種假錯誤，而 `up overrides` 更糟 ——
    靜靜回一句 `(no overrides)`，看起來像結論，其實一個字都沒查到。
    只有 `up obj`（吃連結本體）與 `up guid`（只取其中的資產 guid）例外。
    """
    if args.cmd in ("obj", "gid", "guid"):  # gid 是 obj 的 alias，argparse 存的是使用者打的那個
        return
    for key, val in vars(args).items():
        if isinstance(val, str) and GID_RE.search(val):
            raise SystemExit(
                f"# `up {args.cmd}` 的 {key} 收到的是 GlobalObjectId 連結，"
                "這個子指令吃的不是連結。\n"
                "# 先把連結解成節點（會一併印出接著該下哪一條指令）：\n"
                "#   up obj --locate '<連結>'\n"
                "# 或直接看它的內容：up obj '<連結>'")


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
    pc.add_argument("--name",
                    help="count：名稱篩選，含 * / ? 當 glob 整段比對，否則當 substring")
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
                    help="peek：逗號分隔的欄位名，支援點路徑（_ignoreFilter._ignoreSelfEntity、"
                         "_entries[0]._family）；留空 = 這顆 component 的所有 serialize 欄位")
    pp.add_argument("--deep", nargs="?", type=int, const=2, default=0, metavar="N",
                    help="peek / peek-batch / locate：把巢狀 [Serializable] 類別攤開 N 層"
                         "（不帶數字 = 2）。預設 0 = 只印型別名（輸出小）")
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
    cache_group.add_argument("--cache", action="store_true", help=argparse.SUPPRESS)
    cache_group.add_argument("--no-cache", action="store_true",
                             help="read：相容旗標（快取已於 2026-09-11 移除，無作用）")
    pp.add_argument("--out", help="variant / copy：新 prefab 的 asset path")
    pp.add_argument("--name",
                    help="locate：節點名篩選，含 * / ? 當 glob 整段比對，否則當 substring"
                         "（都忽略大小寫）；variant / copy：新 root 名稱（預設用檔名）")
    pp.add_argument("-n", "--limit", type=int, default=20,
                    help="locate：命中筆數上限")
    pp.add_argument("-f", "--file", help="do：批次操作；peek-batch：probe 清單（- = stdin）")
    pp.add_argument("--quiet", action="store_true",
                    help="do：成功只回摘要、callback、save/verify；錯誤仍完整")
    pp.add_argument("--from", dest="src_type",
                    help="swap-script：舊的 C# 型別名（class name，不含 namespace）")
    pp.add_argument("--to", dest="dst_type",
                    help="swap-script：新的 C# 型別名。同名 serialized 欄位會原樣保留")
    pp.add_argument("--fileid", action="append", metavar="ID",
                    help="swap-script：只換這幾個 MonoBehaviour document（可重複）；"
                         "留空 = 這份檔案裡全部")
    pp.add_argument("--drop", metavar="f1,f2",
                    help="swap-script：順手刪掉這幾個新型別沒有的欄位（不刪也行，"
                         "Unity 下次存檔會自己丟）")
    pp.add_argument("--assembly", default="Assembly-CSharp",
                    help="swap-script：寫進 m_EditorClassIdentifier 的 assembly 名")
    pp.add_argument("--dry-run", action="store_true",
                    help="swap-script：只列會被改到哪幾個 document 與它們的原始欄位值，不動檔案"
                         "（這也是讀「C# 已刪掉的孤兒欄位」的唯一手段）")
    pp.add_argument("--force", action="store_true",
                    help="swap-script：Unity Editor 開著這個專案時仍然硬寫（預設拒絕，"
                         "因為 Editor 一存檔就會整份覆寫且值不可逆地消失）；"
                         "do：目標 prefab 正開在 Prefab Mode 時仍然寫（預設拒絕，stage 之後存檔 / Discard 會蓋掉）")
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
    pl.add_argument("key", help="string table 的 key（不要帶 table 前綴；帶了已存在的 `<table>/` 會自動拆開）。"
                                     "給文案時不存在就建；唯讀查不到只回「找不到」+ 近似 key，不會建")
    pl.add_argument("text", nargs="?", default="", help="文案；留空 = 只讀出既有的")
    pl.add_argument("--locale", default="zh-TW", help="locale（預設 zh-TW）")
    pl.add_argument("--table", default="GameplayUI", help="string table collection（預設 GameplayUI）")
    pl.add_argument("--smart", action="store_true",
                    help="強制開 IsSmart（文案沒有 { 但同一組模板要靠 Smart String 串接時用）")
    pl.add_argument("--refs", action="store_true",
                    help="唯讀：列出引用這個 key 的 prefab / SO / scene（asset + 節點 + component.欄位）")
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

    par = sub.add_parser(
        "asset-refs", help="asset 層級反查：誰直接引用這顆 asset，列到欄位（需要 Unity）",
        description="範圍是整個 Assets/ + Packages/。scene / prefab referrer 會列出節點 [Component.propertyPath]，"
                    "prefab override 會標出來。scene 沒開時非 Play Mode 會暫時 additive 開啟查完關掉。")
    par.add_argument("token", help="asset 路徑 / guid / webhook 連結（跟 up guid 一樣）")
    par.add_argument("-n", "--limit", type=int, default=20, help="referrer 只列前幾個（預設 20）")
    par.add_argument("--all", action="store_true", help="referrer 全部列出")
    par.set_defaults(fn=cmd_asset_refs)

    pwb = sub.add_parser(
        "why-in-build", help="這顆 asset 為什麼進 build：從 build root 的最短引用鏈（需要 Unity）",
        description="root = Active Build Profile（沒 override 就用 EditorBuildSettings）enabled 的 scene"
                    " + Resources + PlayerSettings Preloaded + Addressables。最後一跳列到欄位，"
                    "另外列出 build 內其他直接 referrer（全部斷掉才會出 build）。")
    pwb.add_argument("token", help="asset 路徑 / guid / webhook 連結（跟 up guid 一樣）")
    pwb.add_argument("-n", "--limit", type=int, default=10, help="其他直接 referrer 只列前幾個（預設 10）")
    pwb.add_argument("--all", action="store_true", help="其他直接 referrer 全部列出")
    pwb.set_defaults(fn=cmd_why_in_build)

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
                         help="貼一條 GlobalObjectId 連結，匯出它指的物件（prefab 裡的不用開 Prefab Stage；scene 的要 scene 開著）")
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
    pk.add_argument("--members",
                    help="逗號分隔的欄位/屬性名，支援點路徑（_ignoreFilter._ignoreSelfEntity、"
                         "_entries[0]._family）；留空 = serialize 欄位 + 可查的屬性名清單")
    pk.add_argument("--deep", nargs="?", type=int, const=2, default=0, metavar="N",
                    help="把巢狀 [Serializable] 類別攤開 N 層（不帶數字 = 2）。預設 0 = 只印型別名")
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

    pgp = sub.add_parser(
        "progress", aliases=["prog"],
        help="讀 GameProgress.md / 模組 Progress.md 的最新條目（離線）",
        description="這些檔是 append-only 的流水帳，最新在最下面。預設回最後 5 條完整條目 —— "
                    "取代 `tail -c N`（那會切在句子中間）。"
                    "條目邊界 = col 0 的 `- ` 或 `## `。")
    pgp.add_argument("keyword", nargs="?",
                     help="只回內文含這段的條目（預設只列標題，加 --full 展開）")
    pgp.add_argument("-n", "--limit", type=int, default=5, help="最近幾條（預設 5）")
    pgp.add_argument("--at", type=int, nargs="+", metavar="N",
                     help="展開指定編號的條目（編號從 --list 拿）")
    pgp.add_argument("--list", action="store_true", help="只列標題，一條一行")
    pgp.add_argument("--full", action="store_true", help="展開全部命中的條目，不受 -n 限制")
    pgp.add_argument("--since", metavar="YYYY-MM-DD",
                     help="只看日期 >= 這天的條目（沒寫日期的條目一律不算命中）")
    pgp.add_argument("--module", nargs="?", const="", metavar="KW",
                     help="改讀模組 Progress.md（KW 模糊比對路徑；不給 KW = 列出所有）")
    pgp.add_argument("-f", "--file", help="直接指定 progress 檔路徑（含封存檔）")
    pgp.add_argument("--stat", action="store_true", help="條數 / 大小 / 日期範圍")
    pgp.add_argument("--archive", type=int, metavar="N",
                     help="把最舊的 N 條搬到 <檔名>-Archive.md（會改檔）")
    pgp.set_defaults(fn=progress.cmd)

    pvs = sub.add_parser(
        "verify-skills", aliases=["vs"],
        help="檢查 skill / agent / CLAUDE.md 引用的型別、欄位、路徑是否還存在（離線）",
        description="只驗機械可查的部分。第一次跑完用 --baseline 把既有雜訊寫進 "
                    ".uprefab-skillignore，之後只顯示新漂移。"
                    "語意層的過期改用 --changed 挑出該重讀的段落。")
    pvs.add_argument("--path", metavar="KW", help="只掃路徑含這段的文件")
    pvs.add_argument("--changed", nargs="?", const="1.week", metavar="SINCE",
                     help="改成 diff-driven：列出提到「近期改過的 .cs」的 skill 段落"
                          "（預設 1.week，吃 git --since 的格式）")
    pvs.add_argument("--loose", action="store_true",
                     help="連「查不到又沒有相近型別」的 token 也列（預設只報疑似改名的，"
                          "因為散文裡的 PascalCase 大多不是專案型別）")
    pvs.add_argument("--baseline", action="store_true",
                     help="把目前所有型別／欄位誤判寫進 .uprefab-skillignore（路徑類不寫）")
    pvs.add_argument("-n", "--limit", type=int, default=12, help="每份文件最多印幾條")
    pvs.set_defaults(fn=verifyskills.cmd)

    pse = sub.add_parser(
        "session", aliases=["sess"],
        help="翻舊 Claude Code session transcript，只留對話（離線）",
        description="transcript 裡對話文字只佔 0–2%%，其餘是 tool 進出。這支預設只吐 "
                    "user / assistant 文字 + tool 一行摘要，幾 k tokens 就能翻完一份 —— "
                    "取代 `claude --resume`（全有全無）與派 agent 讀全份（比 resume 還貴）。")
    pse.add_argument("target", nargs="?",
                     help="session uuid 前綴；不給或給 `list` = 列出 session")
    pse.add_argument("-n", "--limit", type=int, default=10,
                     help="list：列幾個（預設 10）；讀：最後幾輪（預設 10）")
    pse.add_argument("--date", metavar="YYYY-MM-DD", help="list：只列那天有活動的 session")
    pse.add_argument("-k", "--keyword", help="list：標題／首句含這段")
    pse.add_argument("--grep", metavar="KW", help="讀：只印含這段的輪次（不受 -n 限制）")
    pse.add_argument("--files", action="store_true",
                     help="讀：只列 Edit / Write / 寫入類 up 指令動過的路徑")
    pse.add_argument("--full", action="store_true",
                     help="讀：含 tool_use input 與 tool_result（每塊截到 --clip×3）")
    pse.add_argument("--agent", nargs="?", const="", metavar="ID",
                     help="讀該 session 派出的 subagent transcript（不給 ID = 列出）")
    pse.add_argument("--clip", type=int, default=100, help="tool 摘要每行字元數（預設 100）")
    pse.set_defaults(fn=session.cmd)

    pu = sub.add_parser("usage", help="使用記錄統計（哪一步最花時間）；`usage hot` 列常查卻沒進 skill 的 prefab")
    pu.add_argument("what", nargs="?", choices=["hot"],
                    help="hot：近幾天被多段調查反覆碰的 prefab，對照 skill 有沒有提到")
    pu.add_argument("--days", type=float, default=hot.DEFAULT_DAYS,
                    help="hot：往回看幾天（預設 7）")
    pu.add_argument("--min-sessions", type=int, default=hot.DEFAULT_MIN_SESSIONS,
                    help="hot：至少幾段調查碰過才算熱（預設 3）")
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
    _guard_gid_args(args)
    if args.cmd == "usage":
        if args.what == "hot":
            hot.report(root, args.days, args.top, args.min_sessions, args.gap)
        else:
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
