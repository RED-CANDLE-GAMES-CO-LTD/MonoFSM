#!/usr/bin/env python3
"""uprefab — Unity serialized data 的離線索引與查詢 CLI。

不需要 Unity Editor 執行中。用法見 `uprefab.py --help`。
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import indexer  # noqa: E402
import query  # noqa: E402
import readcache  # noqa: E402
import unity  # noqa: E402
import usage  # noqa: E402
from config import CONFIG_NAME, Config  # noqa: E402


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


def cmd_find(args, root, cfg):
    con = indexer.connect(root)
    rows = query.find(
        con,
        comp=_like(args.comp),
        name=_like(args.name),
        path=_like(args.path),
        limit=args.limit,
    )
    if not rows:
        print("(no match)")
        return

    resolved = _resolve_anchors(rows) if args.resolve else {}

    for apath, fid, npath, active, comps in rows:
        flag = "" if active else "~"
        anchor = query.anchor(apath, fid)
        print(anchor)
        print(f"    {flag}{npath}  <{comps or ''}>")
        if args.resolve:
            status, payload, how = resolved.get(
                anchor, ("fail", "Unity 沒有回報這個 anchor", "")
            )
            if status == "ok":
                print(f"    --node {payload}" + (f"   [{how}]" if how else ""))
            else:
                print(f"    ✗ anchor 解不開：{payload}")
    print(f"\n{len(rows)} match(es)")


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
    print(f"\n{shown} override(s) {tail}")


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


def _ops_text(args) -> str:
    """批次操作的來源：--file、位置參數，或 stdin。"""
    if getattr(args, "file", None):
        with open(args.file, encoding="utf-8") as fh:
            return fh.read()
    # `scene do` 的第一個位置參數會被 `path`（new / open 用的）先吃掉，所以兩邊都撈
    inline = [v for v in (getattr(args, "path", None), *getattr(args, "ops", ())) if v]
    if inline:
        return "\n".join(inline)
    if sys.stdin.isatty():
        raise SystemExit("沒有操作內容：用 -f <檔案>、直接帶參數，或從 stdin 餵進來")
    return sys.stdin.read()


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
        print(unity.call(f"{SCENE}.Export", args.node, args.depth, not args.fold))
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
    elif args.action == "do":
        print(unity.call(f"{PREFAB}.Batch", args.asset, _ops_text(args)))


def _prefab_read(args, root):
    """prefab read 的唯一出入口 —— 中間夾一層以檔案 mtime 為 key 的磁碟快取。

    只有 read 值得快取：它是唯一「純讀、輸出很肥、同一份東西會被反覆問」的 action。
    key 算不出來時（readcache 回 None）就退化成沒有快取的原本行為。
    """
    fold = not args.fold
    params = {"asset": args.asset, "node": args.node, "depth": args.depth,
              "budget": args.budget, "fsm": args.fsm, "fold": fold}
    key = readcache.key_for(root, args.asset, params)

    if key and not args.no_cache:
        cached = readcache.load(root, key)
        if cached is not None:
            usage.note("cache", "hit")
            print(readcache.HIT_NOTE)
            print(cached)
            return

    if not key:
        usage.note("cache", "off")  # key 算不出來（不是 prefab 檔 / 解析失敗）
    else:
        usage.note("cache", "bypass" if args.no_cache else "miss")
    text = unity.call(f"{READER}.Export", args.asset, args.node, args.depth,
                      fold, args.budget, args.fsm)
    print(text)
    if key:
        readcache.store(root, key, text)


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


def cmd_prompt(args, root, cfg):
    """幫一個 VarString 掛一組有條件的 localized 文字提示。

    這件事本來要跨 Localization 條目、value source 節點、條件 / token 子節點、Auto 綁定與
    Rename 四個系統，每次臨時寫 execute-dynamic-code 都要重踩同一批雷（m_KeyId 是 long、
    節點名含 `/`、{token} 沒開 IsSmart 不會展開）。實作在 C# 的 PromptEdit。
    """
    cases = "\n".join(args.case) if args.case else _cases_from_file(args)
    print(unity.call(
        f"{PROMPT}.Apply", args.asset, args.var_node, cases,
        args.locale, args.table, args.prune))


def cmd_loc(args, root, cfg):
    """直接讀寫 string table 條目（文案的持有者是 SO 而不是節點時用）。"""
    print(unity.call(f"{LOC}.Set", args.table, args.key, args.text, args.locale))


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


def cmd_types(args, root, cfg):
    print(unity.call(f"{PROBE}.Types", args.keyword, args.limit))


def cmd_fields(args, root, cfg):
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
    print(unity.call(
        f"{GID}.Peek", token, args.node, args.depth, not args.fold,
        args.budget, args.fsm, args.open, args.select))


def cmd_peek(args, root, cfg):
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


def cmd_logs(args, root, cfg):
    """Console 記錄的精簡版。原生 get-logs 每筆都帶一整份 JSON 欄位，
    實際要看的是「哪一行炸了」——所以只印訊息與（選配）前幾行 stack。"""
    call = ["get-logs", "--max-count", str(args.limit)]
    if args.type != "All":
        call += ["--log-type", args.type]
    if args.stack:
        call += ["--include-stack-trace", "true"]
    data = unity.run(call)
    logs = data.get("Logs") or []
    print(f"# {data.get('TotalCount', len(logs))} 筆（顯示 {len(logs)}），type={args.type}")
    for entry in logs:
        print(f"[{entry.get('Type')}] {entry.get('Message', '').strip()}")
        if args.stack and entry.get("StackTrace"):
            for line in str(entry["StackTrace"]).strip().splitlines()[: args.stack]:
                print(f"    {line.strip()}")


def cmd_clear(args, root, cfg):
    unity.run(["clear-console"])
    print("console 已清空")


def cmd_play(args, root, cfg):
    data = unity.run(["control-play-mode", "--action", args.action])
    print(json.dumps(data, ensure_ascii=False))


def _like(v: str | None) -> str | None:
    """沒帶萬用字元時自動包成 %v%，讓查詢預設是模糊比對。"""
    if v is None:
        return None
    return v if "%" in v else f"%{v}%"


def main() -> None:
    p = argparse.ArgumentParser(prog="uprefab", description=__doc__)
    p.add_argument("--root", default=".", help="repo root（預設往上自動尋找）")
    sub = p.add_subparsers(dest="cmd", required=True)

    pi = sub.add_parser("index", help="建立/更新索引")
    pi.add_argument("--rebuild", action="store_true", help="忽略 mtime，全部重掃")
    pi.add_argument("-q", "--quiet", action="store_true")
    pi.set_defaults(fn=cmd_index)

    ps = sub.add_parser("scope", help="索引範圍管理")
    ps.add_argument("action", choices=["list", "stats", "init"])
    ps.set_defaults(fn=cmd_scope)

    pf = sub.add_parser("find", help="依 component / 名稱 / 路徑定位節點")
    pf.add_argument("--comp", help="component 型別（短名，模糊比對）")
    pf.add_argument("--name", help="GameObject 名稱")
    pf.add_argument("--path", help="資產路徑")
    pf.add_argument("-n", "--limit", type=int, default=50)
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
    po.set_defaults(fn=cmd_overrides)

    # ---- 需要 Unity 開著 ----

    pc = sub.add_parser("scene", help="對當前開著的 scene 讀 / 寫（需要 Unity）")
    pc.add_argument("action",
                    choices=["new", "copy", "open", "save", "ls", "count", "do"])
    pc.add_argument("path", nargs="?", help="new / copy / open 的 scene 路徑")
    pc.add_argument("--template", help="copy：來源模板 scene 路徑")
    pc.add_argument("--defaults", action="store_true", help="new：帶 Camera + Light")
    pc.add_argument("--node", help="ls：子樹路徑（留空只列 root 一層）")
    pc.add_argument("--depth", type=int, default=-1, help="ls：往下幾層")
    pc.add_argument("--fold", action="store_true", help="ls：摺疊已知子樹並排除視覺 component")
    pc.add_argument("--comp", help="count：component 型別（含子類）")
    pc.add_argument("--name", help="count：名稱含這段")
    pc.add_argument("--sample", type=int, default=0, help="count：附幾筆樣本路徑")
    pc.add_argument("-f", "--file", help="do：從檔案讀批次操作")
    pc.add_argument("ops", nargs="*", help="do：直接帶操作（一個參數一行）")
    pc.set_defaults(fn=cmd_scene)

    pp = sub.add_parser("prefab", help="對 prefab asset 讀 / 寫（需要 Unity）")
    pp.add_argument("action", choices=["read", "do", "variant", "copy"])
    pp.add_argument("asset", help="prefab asset path")
    pp.add_argument("--node", help="read：子樹路徑")
    pp.add_argument("--depth", type=int, default=-1,
                    help="read：明確指定往下幾層（給了就不看 --budget）")
    pp.add_argument("--budget", type=int, default=20000,
                    help="read：字元上限，超標自動摺到塞得進的深度；0 = 不限")
    pp.add_argument("--fsm", action="store_true",
                    help="read：附 FSM markdown 段（states / transitions / conditions）")
    pp.add_argument("--fold", action="store_true")
    pp.add_argument("--no-cache", action="store_true",
                    help="read：跳過讀取快取（仍會寫入新結果）")
    pp.add_argument("--out", help="variant / copy：新 prefab 的 asset path")
    pp.add_argument("--name", help="variant / copy：root 名稱（預設用檔名）")
    pp.add_argument("-f", "--file", help="do：從檔案讀批次操作")
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
                    help="VarString 節點路徑（value source 會掛在它底下）")
    pm.add_argument("--case", action="append",
                    help="一條提示，可重複給；順序 = 挑選優先序")
    pm.add_argument("--locale", default="zh-TW", help="要寫文案的 locale（預設 zh-TW）")
    pm.add_argument("--table", default="GameplayUI", help="string table collection（預設 GameplayUI）")
    pm.add_argument("--prune", action="store_true",
                    help="刪掉不在 --case 清單裡的既有 value source")
    pm.add_argument("-f", "--file", help="從檔案讀 case（一行一條）")
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
                     help="明確指定往下幾層（給了就不看 --budget）")
    pob.add_argument("--budget", type=int, default=20000,
                     help="字元上限，超標自動摺到塞得進的深度；0 = 不限")
    pob.add_argument("--fsm", action="store_true", help="附 FSM markdown 段")
    pob.add_argument("--fold", action="store_true", help="摺疊已知子樹、排除視覺 component")
    pob.add_argument("--locate", action="store_true",
                     help="只回節點路徑 + component 清單，不匯出子樹")
    pob.add_argument("--open", action="store_true",
                     help="物件所在 scene 沒開著時幫忙開（會換掉當前 scene；dirty 時拒絕）")
    pob.add_argument("--select", action="store_true", help="順便在 Unity 裡選中並 ping")
    pob.set_defaults(fn=cmd_obj)

    pk = sub.add_parser("peek", help="讀 scene 上某 component 的 runtime 值（需要 Unity）")
    pk.add_argument("node", help="節點路徑（第一段是 root object 名）")
    pk.add_argument("comp", help="component 型別")
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
    pl.add_argument("--type", default="Error",
                    choices=["All", "Error", "Warning", "Log"])
    pl.add_argument("-n", "--limit", type=int, default=10)
    pl.add_argument("--stack", type=int, nargs="?", const=6, default=0,
                    help="附前幾行 stack trace（預設 6）")
    pl.set_defaults(fn=cmd_logs)

    pcl = sub.add_parser("clear", help="清空 Console（需要 Unity）")
    pcl.set_defaults(fn=cmd_clear)

    py = sub.add_parser("play", help="Play Mode 控制（需要 Unity）")
    py.add_argument("action", choices=["play", "stop", "pause"])
    py.set_defaults(fn=cmd_play)

    pu = sub.add_parser("usage", help="使用記錄統計（哪一步最花時間）")
    pu.add_argument("--gap", type=int, default=900,
                    help="間隔超過幾秒視為新的一段調查（預設 900）")
    pu.add_argument("--top", type=int, default=8)

    args = p.parse_args()
    root = find_root(args.root)
    if args.cmd == "usage":
        usage.report(root, args.gap, args.top)
        return

    tee = usage.Tee(sys.stdout) if usage.enabled() else None
    if tee:
        sys.stdout = tee
    t0 = time.time()
    status = "ok"
    try:
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
        if tee:
            sys.stdout = tee._real
            usage.record(root, args, tee.chars,
                         int((time.time() - t0) * 1000), status, tee.head)


if __name__ == "__main__":
    main()
