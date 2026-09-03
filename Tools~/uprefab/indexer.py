"""SQLite 索引的 schema 與建置。"""

from __future__ import annotations

import json
import os
import re
import sqlite3
import time

import catalog as catalog_mod
import scripts as scripts_mod
import uyaml
from config import Config

DB_NAME = ".uprefab.db"

SCHEMA = """
CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT);

CREATE TABLE IF NOT EXISTS scripts (
  guid TEXT PRIMARY KEY, class TEXT, ns TEXT, path TEXT
);
CREATE INDEX IF NOT EXISTS ix_scripts_class ON scripts(class);

CREATE TABLE IF NOT EXISTS assets (
  id INTEGER PRIMARY KEY, path TEXT UNIQUE, guid TEXT, kind TEXT,
  tier TEXT, mtime REAL, size INTEGER
);
CREATE INDEX IF NOT EXISTS ix_assets_guid ON assets(guid);

-- GameObject 節點。path 是從 root 算起的階層路徑，建完索引後回填。
CREATE TABLE IF NOT EXISTS nodes (
  asset_id INTEGER, file_id INTEGER, name TEXT, parent_file_id INTEGER,
  is_active INTEGER, tag TEXT, layer INTEGER, path TEXT,
  -- prefab instance 成員：指向來源 prefab 的 guid 與其中的 fileID
  src_guid TEXT, src_file_id INTEGER,
  PRIMARY KEY (asset_id, file_id)
);
CREATE INDEX IF NOT EXISTS ix_nodes_name ON nodes(name);

-- 掛在節點上的 component。type 是短名（MonoBehaviour 會換成實際 class）。
CREATE TABLE IF NOT EXISTS comps (
  asset_id INTEGER, file_id INTEGER, go_file_id INTEGER, class_id INTEGER,
  type TEXT, ns TEXT, script_guid TEXT, enabled INTEGER,
  PRIMARY KEY (asset_id, file_id)
);
CREATE INDEX IF NOT EXISTS ix_comps_type ON comps(type);
CREATE INDEX IF NOT EXISTS ix_comps_guid ON comps(script_guid);

-- 反向引用用的邊。to_guid 非空代表指向其他資產。
CREATE TABLE IF NOT EXISTS refs (
  asset_id INTEGER, from_file_id INTEGER, field TEXT,
  to_file_id INTEGER, to_guid TEXT
);
CREATE INDEX IF NOT EXISTS ix_refs_toguid ON refs(to_guid);
CREATE INDEX IF NOT EXISTS ix_refs_tofid ON refs(asset_id, to_file_id);

-- prefab instance 與它的 override（m_Modifications）。
CREATE TABLE IF NOT EXISTS instances (
  asset_id INTEGER, file_id INTEGER, source_guid TEXT,
  parent_file_id INTEGER, mod_count INTEGER,
  PRIMARY KEY (asset_id, file_id)
);
CREATE TABLE IF NOT EXISTS mods (
  asset_id INTEGER, instance_file_id INTEGER, target_file_id INTEGER,
  target_guid TEXT, prop TEXT, value TEXT
);
CREATE INDEX IF NOT EXISTS ix_mods_asset ON mods(asset_id, instance_file_id);

-- stripped document（prefab instance / variant 繼承來的物件佔位）。
-- override 的 target 常常指到這種佔位，要靠它往來源 prefab 再跳一層。
CREATE TABLE IF NOT EXISTS stubs (
  asset_id INTEGER, file_id INTEGER, src_guid TEXT, src_file_id INTEGER,
  PRIMARY KEY (asset_id, file_id)
);
CREATE INDEX IF NOT EXISTS ix_stubs_lookup ON stubs(asset_id, file_id);

-- 掛在 stripped Transform 底下的節點：本檔內解不出 parent GameObject
-- （stripped Transform 沒有 m_GameObject 欄位），要等全庫索引完跨檔回推。
CREATE TABLE IF NOT EXISTS pending_parent (
  asset_id INTEGER, go_file_id INTEGER, father_tf_file_id INTEGER,
  PRIMARY KEY (asset_id, go_file_id)
);

-- C# 型別目錄：class → 用途說明 + serialized 欄位（給 up catalog / up fields 用）
CREATE TABLE IF NOT EXISTS catalog (
  class TEXT PRIMARY KEY, path TEXT, kind TEXT, bases TEXT,
  is_abstract INTEGER, is_obsolete INTEGER, summary TEXT, has_doc INTEGER,
  fields TEXT, self_obsolete INTEGER
);
CREATE INDEX IF NOT EXISTS ix_catalog_kind ON catalog(kind);

-- catalog 的增量依據：每支 .cs 的 mtime/size，加上該檔所有 class 宣告的 base 清單
-- （kind 要沿全庫繼承鏈解，中繼 class 的 base 不能只留在被改動的那幾支檔案裡）
CREATE TABLE IF NOT EXISTS cs_files (
  path TEXT PRIMARY KEY, mtime REAL, size INTEGER, bases TEXT
);

-- (guid, fileID) → 人類可讀標籤的解析結果快取，全庫索引完才算得出來
CREATE TABLE IF NOT EXISTS target_labels (
  guid TEXT, file_id INTEGER, label TEXT, PRIMARY KEY (guid, file_id)
);
"""

# Unity 內建 class id：只有這幾種會影響階層與節點識別
CID_GAMEOBJECT = 1
CID_TRANSFORM = 4
CID_RECTTRANSFORM = 224
CID_MONOBEHAVIOUR = 114
CID_PREFAB_INSTANCE = 1001

# 沿 variant / nested prefab 鏈往上找原始物件時的最大跳數
MAX_STUB_HOPS = 6

# 不需要進 refs 的欄位（純結構關聯，會把 refs 表撐爆卻沒有查詢價值）
STRUCTURAL_FIELDS = {
    "m_GameObject",
    "m_Father",
    "m_Children",
    "m_Component",
    "component",
    "m_CorrespondingSourceObject",
    "m_PrefabInstance",
    "m_PrefabAsset",
    "m_Script",
    "target",
    "m_TransformParent",
}

# 單一 document 最多收多少條引用邊（防序列化 blob 撐爆索引）
MAX_REFS_PER_DOC = 64


def connect(root: str) -> sqlite3.Connection:
    con = sqlite3.connect(os.path.join(root, DB_NAME))
    con.executescript(SCHEMA)
    return con


def asset_guid(root: str, rel: str) -> str | None:
    """從 .meta 讀資產自己的 guid。"""
    try:
        head = open(os.path.join(root, rel + ".meta"), encoding="utf-8", errors="replace").read(400)
    except OSError:
        return None
    m = scripts_mod.GUID_RE.search(head)
    return m.group(1) if m else None


def build(root: str, cfg: Config, incremental: bool = True, progress=None) -> dict:
    con = connect(root)
    stats = {"scanned": 0, "skipped": 0, "nodes": 0, "comps": 0, "refs": 0, "mods": 0}
    t0 = time.time()

    _build_script_table(con, root, progress)
    _build_catalog_table(con, root, progress)
    guid2class = {g: (c, n) for g, c, n in con.execute("SELECT guid, class, ns FROM scripts")}

    known = {p: (m, s) for p, m, s in con.execute("SELECT path, mtime, size FROM assets")}
    seen: set[str] = set()

    for rel, tier in cfg.iter_assets():
        seen.add(rel)
        full = os.path.join(root, rel)
        try:
            st = os.stat(full)
        except OSError:
            continue
        if incremental and rel in known and known[rel] == (st.st_mtime, st.st_size):
            stats["skipped"] += 1
            continue
        if progress:
            progress(f"index {rel}")
        _index_asset(con, root, cfg, rel, tier, st, guid2class, stats)
        stats["scanned"] += 1

    # 清掉已刪除的資產
    for path in set(known) - seen:
        _purge(con, path)

    _resolve_stripped_parents(con, progress)
    _resolve_cross_asset_names(con, progress)
    _resolve_target_labels(con, progress)
    con.execute(
        "INSERT OR REPLACE INTO meta VALUES ('built_at', ?)", (str(int(time.time())),)
    )
    con.commit()
    con.execute("ANALYZE")
    con.close()
    stats["seconds"] = round(time.time() - t0, 1)
    return stats


def _resolve_stripped_parents(con: sqlite3.Connection, progress) -> None:
    """把「父是 stripped Transform」的節點接回本檔的 stripped GameObject。

    prefab variant / nested prefab 裡，繼承來的物件只留 stripped 佔位，
    stripped Transform 沒有 m_GameObject，所以索引當下算不出 parent。
    但它的 m_CorrespondingSourceObject 指向來源 prefab 的 Transform，
    而來源 prefab 的 comps 表知道那個 Transform 屬於哪個 GameObject，
    再用 (src_guid, src_go_file_id) 回頭找本檔對應的 stripped 節點即可。
    """
    if progress:
        progress("resolve stripped parents …")

    guid2asset = {g: a for g, a in con.execute(
        "SELECT guid, id FROM assets WHERE guid IS NOT NULL")}
    rows = con.execute(
        "SELECT asset_id, go_file_id, father_tf_file_id FROM pending_parent"
    ).fetchall()

    fixed, touched = [], set()
    for aid, go_fid, tf_fid in rows:
        parent = _go_of_transform(con, guid2asset, aid, tf_fid)
        if parent:
            fixed.append((parent, aid, go_fid))
            touched.add(aid)

    con.executemany(
        "UPDATE nodes SET parent_file_id=? WHERE asset_id=? AND file_id=?", fixed
    )
    con.commit()
    if progress:
        progress(f"stripped parents: {len(fixed)}/{len(rows)} 接回階層")


def _go_of_transform(con, guid2asset, aid, tf_fid, hops=MAX_STUB_HOPS):
    """在 asset `aid` 裡問「這個 Transform 屬於哪個 GameObject」。

    一般情況 comps 表直接答得出來。Transform 是 stripped 佔位時就要往上一層
    來源 prefab 問同一個問題，拿到來源裡的 GameObject fileID 之後，再回頭在
    本檔找「src 指向它」的 stripped 節點 —— 那才是本檔的 parent。
    """
    if hops <= 0:
        return None
    row = con.execute(
        "SELECT go_file_id FROM comps WHERE asset_id=? AND file_id=?", (aid, tf_fid)
    ).fetchone()
    if row and row[0]:
        return row[0]
    src = con.execute(
        "SELECT src_guid, src_file_id FROM stubs WHERE asset_id=? AND file_id=?",
        (aid, tf_fid),
    ).fetchone()
    if not src or not src[0]:
        return None
    src_aid = guid2asset.get(src[0])
    if src_aid is None:
        return None
    up_go = _go_of_transform(con, guid2asset, src_aid, src[1], hops - 1)
    if not up_go:
        return None
    local = con.execute(
        "SELECT file_id FROM nodes WHERE asset_id=? AND src_guid=? AND src_file_id=?",
        (aid, src[0], up_go),
    ).fetchone()
    return local[0] if local else None


def _resolve_cross_asset_names(con: sqlite3.Connection, progress) -> None:
    """沒有 m_Name override 的 prefab instance 成員，去來源 prefab 查原名。

    必須等所有資產都索引完才做得到（來源 prefab 可能比引用它的 scene 晚掃）。
    來源本身不在索引範圍內時，退而顯示 `(來源檔名)`。
    """
    if progress:
        progress("resolve prefab instance names …")
    con.execute(
        """
        UPDATE nodes SET name = COALESCE((
            SELECT s.name FROM nodes s JOIN assets sa ON sa.id = s.asset_id
             WHERE sa.guid = nodes.src_guid AND s.file_id = nodes.src_file_id
        ), '') WHERE name = '' AND src_guid IS NOT NULL
        """
    )
    con.execute(
        """
        UPDATE nodes SET name = COALESCE((
            SELECT '(' || replace(sa.path, rtrim(sa.path, replace(sa.path, '/', '')), '') || ')'
              FROM assets sa WHERE sa.guid = nodes.src_guid
        ), '(prefab)') WHERE name = '' AND src_guid IS NOT NULL
        """
    )
    # 名稱變動後，受影響資產的階層路徑要重算
    rows = con.execute(
        "SELECT DISTINCT asset_id FROM nodes WHERE src_guid IS NOT NULL"
    ).fetchall()
    for (aid,) in rows:
        nodes = {
            r[1]: list(r)
            for r in con.execute(
                "SELECT asset_id, file_id, name, parent_file_id, is_active, tag,"
                " layer, path, src_guid, src_file_id FROM nodes WHERE asset_id=?",
                (aid,),
            )
        }
        _fill_paths(nodes)
        con.executemany(
            "UPDATE nodes SET path=? WHERE asset_id=? AND file_id=?",
            [(r[7], r[0], r[1]) for r in nodes.values()],
        )
    con.commit()


def _resolve_target_labels(con: sqlite3.Connection, progress) -> None:
    """把 override 的 target (guid, fileID) 解析成可讀標籤。

    target 常常指向 variant 繼承來的 stripped 佔位物件，此時要沿
    m_CorrespondingSourceObject 往上一層來源 prefab 再查一次，直到查到
    真正的 GameObject / component 為止。
    """
    if progress:
        progress("resolve override targets …")

    guid2asset = {g: a for g, a in con.execute(
        "SELECT guid, id FROM assets WHERE guid IS NOT NULL")}
    targets = con.execute(
        "SELECT DISTINCT target_guid, target_file_id FROM mods"
        " WHERE target_guid IS NOT NULL"
    ).fetchall()

    guid2path = {g: p for g, p in con.execute(
        "SELECT guid, path FROM assets WHERE guid IS NOT NULL")}

    out = []
    exact = 0
    for guid, fid in targets:
        label = _label_for(con, guid2asset, guid, fid)
        if label:
            exact += 1
        elif guid in guid2path:
            # 沒解析到具體物件，至少標出它屬於哪個資產，比裸 fileID 有用
            label = f"fileID:{fid} @{os.path.basename(guid2path[guid])}"
        out.append((guid, fid, label))
    con.executemany("INSERT OR REPLACE INTO target_labels VALUES (?,?,?)", out)
    con.commit()
    if progress:
        progress(f"targets: {exact}/{len(out)} 解析到物件")


def _label_for(con, guid2asset, guid, fid) -> str | None:
    """沿 variant 鏈往上找，回傳具體物件的標籤；找不到回 None。

    找不到的主因有二：物件被 scriptOnly 濾掉，或是多層 variant 的合成
    fileID 在任何一個檔案裡都不存在（需要完整的 prefab 實例化演算法才能還原）。
    """
    for _ in range(MAX_STUB_HOPS):
        aid = guid2asset.get(guid)
        if aid is None:
            return None
        row = con.execute(
            "SELECT path FROM nodes WHERE asset_id=? AND file_id=?", (aid, fid)
        ).fetchone()
        if row:
            return row[0]
        row = con.execute(
            "SELECT n.path, c.type FROM comps c"
            " LEFT JOIN nodes n ON n.asset_id=c.asset_id AND n.file_id=c.go_file_id"
            " WHERE c.asset_id=? AND c.file_id=?",
            (aid, fid),
        ).fetchone()
        if row:
            return f"{row[0] or '?'} <{row[1]}>"
        row = con.execute(
            "SELECT src_guid, src_file_id FROM stubs WHERE asset_id=? AND file_id=?",
            (aid, fid),
        ).fetchone()
        if not row or not row[0]:
            return None
        guid, fid = row  # 往來源 prefab 再跳一層
    return None


def _build_script_table(con: sqlite3.Connection, root: str, progress) -> None:
    if progress:
        progress("scan .cs.meta …")
    rows = list(scripts_mod.build_table(root))
    con.executemany("INSERT OR REPLACE INTO scripts VALUES (?,?,?,?)", rows)
    con.commit()
    if progress:
        progress(f"scripts: {len(rows)}")


def _build_catalog_table(con: sqlite3.Connection, root: str, progress=None,
                         incremental: bool = True) -> int:
    """重建 C# 型別目錄，回傳實際重新 parse 的檔案數。

    走 mtime/size 增量：全庫 walk 只要 0.2 秒，重 parse 才是那 4 秒的來源，
    所以沒改檔時刷新幾乎免費 —— 這是 `up catalog` 每次呼叫都能自動對齊原始碼的前提。
    kind / obsolete 的繼承鏈仍每次整批重解（靠 cs_files 存下的 base 表），
    改一支 base class 才不會漏更新底下的子類。
    """
    if progress:
        progress("scan .cs catalog …")

    known = {p: (m, s, b) for p, m, s, b in
             con.execute("SELECT path, mtime, size, bases FROM cs_files")}
    has_rows = con.execute("SELECT COUNT(*) FROM catalog").fetchone()[0] > 0
    if not incremental or not known or not has_rows:
        per_file: dict[str, dict] = {}
        rows = catalog_mod.build_rows(root, per_file)
        con.execute("DELETE FROM cs_files")
        files = _cs_file_stats(root)
        con.executemany(
            "INSERT OR REPLACE INTO cs_files VALUES (?,?,?,?)",
            [(rel, mt, sz, json.dumps(per_file.get(rel, {}), ensure_ascii=False))
             for rel, (mt, sz) in files.items()],
        )
        _write_catalog(con, rows)
        if progress:
            progress(f"catalog: {len(rows)}（全建）")
        return len(files)

    files = _cs_file_stats(root)
    changed = [rel for rel, (mt, sz) in files.items()
               if rel not in known or known[rel][:2] != (mt, sz)]
    removed = set(known) - set(files)
    if not changed and not removed:
        return 0

    # 未變動的檔案沿用 catalog / cs_files 裡的既有結果
    all_bases: dict[str, list[str]] = {}
    stale_paths = set(changed) | removed
    for rel, (_, _, raw) in known.items():
        if rel in stale_paths:
            continue
        for name, bs in json.loads(raw or "{}").items():
            if bs or name not in all_bases:
                all_bases[name] = bs

    rows: dict[str, dict] = {}
    for r in con.execute(
            "SELECT class, path, bases, is_abstract, summary, has_doc, fields, self_obsolete "
            "FROM catalog"):
        if r[1] in stale_paths:
            continue
        rows[r[0]] = {
            "class": r[0], "path": r[1], "bases": (r[2] or "").split(",") if r[2] else [],
            "abstract": bool(r[3]), "summary": r[4], "has_doc": bool(r[5]),
            "fields": json.loads(r[6] or "[]"), "self_obsolete": bool(r[7]),
        }

    for rel in changed:
        info, bases = catalog_mod.parse_one(root, rel)
        for name, bs in bases.items():
            if bs or name not in all_bases:
                all_bases[name] = bs
        mt, sz = files[rel]
        con.execute("INSERT OR REPLACE INTO cs_files VALUES (?,?,?,?)",
                    (rel, mt, sz, json.dumps(bases, ensure_ascii=False)))
        if info:
            info["path"] = rel
            info["self_obsolete"] = info["obsolete"]
            rows[info["class"]] = info
    for rel in removed:
        con.execute("DELETE FROM cs_files WHERE path=?", (rel,))

    _write_catalog(con, catalog_mod.rows_to_tuples(rows, all_bases))
    if progress:
        progress(f"catalog: {len(rows)}（增量 {len(changed)} 改 / {len(removed)} 刪）")
    return len(changed) + len(removed)


def _write_catalog(con: sqlite3.Connection, rows: list[tuple]) -> None:
    # 這張表每次整批重寫，欄位加減時直接重來，省掉 migration
    con.execute("DROP TABLE IF EXISTS catalog")
    con.executescript(SCHEMA)
    con.executemany(
        "INSERT OR REPLACE INTO catalog VALUES (?,?,?,?,?,?,?,?,?,?)", rows)
    con.commit()


def _cs_file_stats(root: str) -> dict[str, tuple[float, int]]:
    out = {}
    for rel in catalog_mod.iter_cs_paths(root):
        try:
            st = os.stat(os.path.join(root, rel))
        except OSError:
            continue
        out[rel] = (st.st_mtime, st.st_size)
    return out


def refresh_catalog(con: sqlite3.Connection, root: str, progress=None) -> int:
    """查詢前對齊原始碼。沒有 .cs 變動時只花一次 os.walk（約 0.2 秒）。

    離線索引最容易踩的坑就是「改了 .cs 但沒重跑 up index」，
    於是 `up catalog` 一直回舊的 summary / 欄位。與其靠人記得重建，不如查詢時自動補。
    """
    return _build_catalog_table(con, root, progress, incremental=True)


def _purge(con: sqlite3.Connection, path: str) -> None:
    row = con.execute("SELECT id FROM assets WHERE path=?", (path,)).fetchone()
    if not row:
        return
    aid = row[0]
    for t in ("nodes", "comps", "refs", "instances", "mods", "stubs", "pending_parent"):
        con.execute(f"DELETE FROM {t} WHERE asset_id=?", (aid,))
    con.execute("DELETE FROM assets WHERE id=?", (aid,))


def _index_asset(con, root, cfg: Config, rel, tier, st, guid2class, stats) -> None:
    _purge(con, rel)
    kind = os.path.splitext(rel)[1].lstrip(".")
    con.execute(
        "INSERT INTO assets (path, guid, kind, tier, mtime, size) VALUES (?,?,?,?,?,?)",
        (rel, asset_guid(root, rel), kind, tier, st.st_mtime, st.st_size),
    )
    aid = con.execute("SELECT id FROM assets WHERE path=?", (rel,)).fetchone()[0]

    shallow = tier == "shallow"
    nodes: dict[int, list] = {}      # go fileID -> row
    transforms: dict[int, int] = {}  # go fileID -> parent transform fileID
    tf_owner: dict[int, int] = {}    # transform fileID -> go fileID
    comps: list[tuple] = []
    refs: list[tuple] = []
    inst: list[tuple] = []
    mods: list[tuple] = []
    go_has_script: set[int] = set()
    stripped_go: dict[int, tuple[str | None, int, int]] = {}
    stubs: list[tuple] = []

    for doc in uyaml.scan(os.path.join(root, rel)):
        cid = doc.class_id

        if doc.stripped:
            # 不管是 GameObject 還是 component，都記一筆給 override 解析用
            s = doc.top_ref("m_CorrespondingSourceObject")
            if s:
                stubs.append((aid, doc.file_id, s[1], s[0]))

        if cid == CID_GAMEOBJECT and doc.stripped:
            # prefab instance 成員：本身沒有欄位，名稱與階層要靠
            # m_CorrespondingSourceObject（來源 prefab 內的 fileID）回推
            src = doc.top_ref("m_CorrespondingSourceObject")
            pi = doc.top_ref("m_PrefabInstance")
            stripped_go[doc.file_id] = (
                src[1] if src else None,
                src[0] if src else 0,
                pi[0] if pi else 0,
            )
            nodes[doc.file_id] = [
                aid, doc.file_id, "", None, 1, "", 0, None,
                src[1] if src else None, src[0] if src else 0,
            ]
            # prefab instance 本身就是 override 稽核的對象，一定要保留
            go_has_script.add(doc.file_id)

        elif cid == CID_GAMEOBJECT:
            nodes[doc.file_id] = [
                aid, doc.file_id, _unquote(doc.top("m_Name") or ""), None,
                1 if doc.top("m_IsActive") == "1" else 0,
                doc.top("m_TagString") or "", int(doc.top("m_Layer") or 0), None,
                None, 0,
            ]

        elif cid in (CID_TRANSFORM, CID_RECTTRANSFORM):
            go = doc.top_ref("m_GameObject")
            father = doc.top_ref("m_Father")
            if go:
                tf_owner[doc.file_id] = go[0]
                transforms[go[0]] = father[0] if father else 0
                # Transform 也要進 comps：override 最常改的就是它的
                # m_LocalPosition / m_LocalRotation，沒有它就解析不出 target
                comps.append((aid, doc.file_id, go[0], cid, doc.type_name, "", None, 1))

        elif cid == CID_MONOBEHAVIOUR:
            # stripped 是 prefab instance 的佔位 document，沒有實際欄位資料，
            # 真值在來源 prefab + m_Modifications 裡，收進來只會灌水
            if doc.stripped:
                continue
            go = doc.top_ref("m_GameObject")
            sref = doc.top_ref("m_Script")
            sguid = sref[1] if sref else None
            cls, ns = scripts_mod.class_from_editor_id(doc.top("m_EditorClassIdentifier") or "")
            if not cls and sguid:
                cls, ns = guid2class.get(sguid, ("", ""))
            if go:
                go_has_script.add(go[0])
            comps.append((
                aid, doc.file_id, go[0] if go else 0, cid, cls or "?", ns, sguid,
                1 if doc.top("m_Enabled") != "0" else 0,
            ))
            if not shallow:
                _collect_refs(aid, doc, refs)

        elif cid == CID_PREFAB_INSTANCE:
            src = doc.any_ref("m_SourcePrefab")
            parent = doc.any_ref("m_TransformParent")
            # shallow 層的資產只是「被查詢的對象」（提供節點名與型別給
            # override target 解析），它自己的 override 不需要存
            m = [] if shallow else uyaml.parse_modifications(doc.block("m_Modifications"))
            inst.append((
                aid, doc.file_id, src[1] if src else None,
                parent[0] if parent else 0, len(m),
            ))
            mods.extend(_compact_mods(aid, doc.file_id, m))

        elif not shallow and not doc.stripped:
            # 內建 component（Collider、Renderer …）
            go = doc.top_ref("m_GameObject")
            if go:
                comps.append((
                    aid, doc.file_id, go[0], cid, doc.type_name, "", None,
                    1 if doc.top("m_Enabled") != "0" else 0,
                ))

    # 回填階層：Transform 的 m_Father 換算成 GameObject 的 parent
    # 父是 stripped Transform 時本檔內解不出 GameObject（stripped 沒有
    # m_GameObject 欄位），記進 pending_parent 等跨檔 post-pass 回推
    pending: list[tuple] = []
    for go_fid, row in nodes.items():
        father_tf = transforms.get(go_fid, 0)
        row[3] = tf_owner.get(father_tf, 0)
        if not row[3] and father_tf:
            pending.append((aid, go_fid, father_tf))

    _resolve_stripped(nodes, stripped_go, inst, mods, tf_owner)
    _fill_paths(nodes)

    # scriptOnly：只留下「自己或後代掛有自家 script」的節點
    if cfg.script_only and not shallow:
        keep = _keep_set(nodes, go_has_script)
        nodes = {k: v for k, v in nodes.items() if k in keep}
        comps = [c for c in comps if c[2] in keep or c[2] == 0]
        # refs 是在知道 keep set 之前收集的，這裡補上同樣的過濾
        live_comps = {c[1] for c in comps}
        refs = [r for r in refs if r[1] in live_comps]

    # scene root 黑名單
    for bad in cfg.excluded_roots(rel.replace(os.sep, "/")):
        drop = {k for k, v in nodes.items() if v[7] == bad or (v[7] or "").startswith(bad + "/")}
        if drop:
            nodes = {k: v for k, v in nodes.items() if k not in drop}
            comps = [c for c in comps if c[2] not in drop]

    con.executemany("INSERT OR REPLACE INTO nodes VALUES (?,?,?,?,?,?,?,?,?,?)", nodes.values())
    con.executemany("INSERT OR REPLACE INTO comps VALUES (?,?,?,?,?,?,?,?)", comps)
    con.executemany("INSERT INTO refs VALUES (?,?,?,?,?)", refs)
    con.executemany("INSERT OR REPLACE INTO instances VALUES (?,?,?,?,?)", inst)
    con.executemany("INSERT INTO mods VALUES (?,?,?,?,?,?)", mods)
    con.executemany("INSERT OR REPLACE INTO stubs VALUES (?,?,?,?)", stubs)
    con.executemany(
        "INSERT OR REPLACE INTO pending_parent VALUES (?,?,?)",
        [p for p in pending if p[1] in nodes],
    )
    stats["nodes"] += len(nodes)
    stats["comps"] += len(comps)
    stats["refs"] += len(refs)
    stats["mods"] += len(mods)


# 會被合併成單列的向量式 override：每個擺放過的物件都會有這幾筆，
# 拆成 x/y/z/w 各一列只是灌大索引，看的人也只想知道「位置被改了、改成多少」
VECTOR_PROPS = ("m_LocalPosition", "m_LocalRotation", "m_LocalScale", "m_AnchoredPosition")


def _compact_mods(aid: int, inst_fid: int, mods: list[dict]) -> list[tuple]:
    """把同一 target 的向量分量 override 合併成一列。"""
    out: list[tuple] = []
    vectors: dict[tuple, dict[str, dict[str, str]]] = {}

    for mm in mods:
        key = (mm.get("target_file_id"), mm.get("target_guid"))
        prop = mm.get("prop", "")
        base, _, comp = prop.rpartition(".")
        if base in VECTOR_PROPS and len(comp) == 1:
            vectors.setdefault(key, {}).setdefault(base, {})[comp] = mm.get("value", "")
        else:
            out.append((aid, inst_fid, key[0], key[1], prop, _unquote(mm.get("value", ""))))

    for (tfid, tguid), by_prop in vectors.items():
        for base, parts in by_prop.items():
            value = ",".join(f"{k}={parts[k]}" for k in sorted(parts))
            out.append((aid, inst_fid, tfid, tguid, base, f"({value})"))
    return out


def _collect_refs(aid, doc, out) -> None:
    """收集 document 內的引用邊，去重並限量。

    MonoFSM 的路徑解析結構（fieldName / TargetMb / value …）在 scene 裡會
    序列化出上百萬筆重複的邊。反查引用只需要「哪個 component 指向哪裡」的
    粗粒度答案，所以同一個 (欄位, 目標) 只留一筆，每個 document 也設上限。
    """
    seen: set[tuple[str, int, str | None]] = set()
    for field, fid, guid in doc.iter_refs():
        if field in STRUCTURAL_FIELDS:
            continue
        key = (field, fid, guid)
        if key in seen:
            continue
        seen.add(key)
        if len(seen) > MAX_REFS_PER_DOC:
            break
        out.append((aid, doc.file_id, field, fid, guid))


def _resolve_stripped(nodes, stripped_go, inst, mods, tf_owner) -> None:
    """補上 prefab instance 成員節點的名稱與 parent。

    名稱優先取 m_Modifications 裡的 m_Name override；沒有 override 的
    留空，交給 build() 最後的跨資產解析（去來源 prefab 查原名）。
    parent 則走 PrefabInstance 的 m_TransformParent。
    """
    name_override = {
        (m[3], m[2]): m[5] for m in mods if m[4] == "m_Name"
    }  # (target_guid, target_file_id) -> value
    inst_parent = {i[1]: i[3] for i in inst}  # instance fileID -> parent transform fileID

    for fid, (src_guid, src_fid, pi_fid) in stripped_go.items():
        row = nodes.get(fid)
        if row is None:
            continue
        nm = name_override.get((src_guid, src_fid))
        if nm:
            row[2] = _unquote(nm)
        if not row[3]:
            row[3] = tf_owner.get(inst_parent.get(pi_fid, 0), 0)


def _fill_paths(nodes: dict[int, list]) -> None:
    """用 parent 鏈算出每個節點的階層路徑，遇到環就停在該層。"""
    cache: dict[int, str] = {}

    def path_of(fid: int, depth: int = 0) -> str:
        if fid in cache:
            return cache[fid]
        row = nodes.get(fid)
        if row is None or depth > 64:
            return ""
        parent = row[3]
        p = f"{path_of(parent, depth + 1)}/{row[2]}" if parent else row[2]
        cache[fid] = p
        return p

    for fid, row in nodes.items():
        row[7] = path_of(fid)


def _keep_set(nodes: dict[int, list], go_has_script: set[int]) -> set[int]:
    """保留有 script 的節點，以及它們到 root 的整條祖先鏈（維持階層完整）。"""
    keep: set[int] = set()
    for fid in go_has_script:
        cur, guard = fid, 0
        while cur and cur in nodes and cur not in keep and guard < 64:
            keep.add(cur)
            cur = nodes[cur][3]
            guard += 1
    return keep


_ESCAPE_RE = re.compile(r"\\u([0-9a-fA-F]{4})")


def _unquote(s: str) -> str:
    """去掉 YAML 引號並還原逃逸字元。

    Unity 對含非 ASCII 的名稱會用雙引號並逃逸成 `\\uXXXX`
    （例如 蒸汽壓力 → `\\u84B8\\u6C7D\\u58D3\\u529B`）。不還原的話中文名稱
    完全搜不到，對這個專案來說等於搜尋功能失效。
    """
    if len(s) >= 2 and s[0] == s[-1] and s[0] in "'\"":
        quote, s = s[0], s[1:-1]
        if quote == '"':
            s = _ESCAPE_RE.sub(lambda m: chr(int(m.group(1), 16)), s)
            s = s.replace('\\"', '"').replace("\\\\", "\\")
        else:
            # 單引號字串裡 '' 代表一個單引號
            s = s.replace("''", "'")
    return s
