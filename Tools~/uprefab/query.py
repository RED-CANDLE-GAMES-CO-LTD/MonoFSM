"""find / overrides / scope stats 的查詢實作。輸出以 anchor 為主。

anchor 格式：`Assets/…/Foo.prefab#<fileID>`
fileID 對改名穩定，是餵回 Unity 精讀（Phase 3）用的定址。
"""

from __future__ import annotations

import sqlite3


def _find_where(comp=None, name=None, path=None, scope="full"):
    """find / find_count / find_by_asset 共用的 FROM+WHERE 與參數。

    抽出來是為了讓「列出來的那 50 筆」跟「總共幾筆」一定是同一組條件 ——
    兩邊各寫一份 SQL 的話，改了一邊沒改另一邊會回出互相矛盾的數字。
    """
    args: list = []
    if comp:
        sql = """
            FROM comps c
            JOIN nodes n ON n.asset_id = c.asset_id AND n.file_id = c.go_file_id
            JOIN assets a ON a.id = n.asset_id
           WHERE c.type LIKE ?
        """
        args.append(comp)
    else:
        sql = """
            FROM nodes n JOIN assets a ON a.id = n.asset_id
           WHERE 1=1
        """
    if name:
        sql += " AND n.name LIKE ?"
        args.append(name)
    if path:
        sql += " AND a.path LIKE ?"
        args.append(path)
    if scope == "full":
        sql += " AND a.tier='full'"
    elif scope == "shallow":
        sql += " AND a.tier='shallow'"
    elif scope != "all":
        raise ValueError(f"unknown find scope: {scope}")
    return sql, args


def find_count(con: sqlite3.Connection, comp=None, name=None, path=None,
               scope="full") -> int:
    """同條件的總命中數 —— 讓 limit 切掉時能講出「50 / 共 4132」。"""
    sql, args = _find_where(comp, name, path, scope)
    return con.execute("SELECT COUNT(*) " + sql, args).fetchone()[0]


def find_totals(con: sqlite3.Connection, comp=None, name=None, path=None,
                scope="full"):
    """(總命中數, 涵蓋幾個資產) —— --by-asset 的表尾要能講「列出的只是前幾名」。"""
    sql, args = _find_where(comp, name, path, scope)
    return con.execute(
        "SELECT COUNT(*), COUNT(DISTINCT a.path) " + sql, args).fetchone()


def find_by_asset(con: sqlite3.Connection, comp=None, name=None, path=None,
                  limit=50, scope="full"):
    """同條件的分佈：[(資產路徑, 命中數)]，多的排前面。"""
    sql, args = _find_where(comp, name, path, scope)
    sql = ("SELECT a.path, COUNT(*) " + sql +
           " GROUP BY a.path ORDER BY COUNT(*) DESC, a.path LIMIT ?")
    return con.execute(sql, args + [limit]).fetchall()


def find(con: sqlite3.Connection, comp=None, name=None, path=None, limit=50,
         scope="full"):
    """依 component 型別 / 節點名 / 資產路徑定位節點。

    分兩階段，理由是查詢計劃：

    1. **定位** —— 有 `comp` 條件時以 `comps` 當驅動表（`WHERE c.type LIKE ?` 走
       `ix_comps_type`），再 join `nodes`（走 PRIMARY KEY `(asset_id, file_id)`）。
       原本寫成 `FROM nodes WHERE EXISTS (SELECT … FROM comps …)`，SQLite 選的計劃是
       `SCAN n` —— 全掃 12.7 萬列 nodes、每列跑一次 EXISTS，`ix_comps_type` 完全沒用上，
       單一查詢要兩分鐘以上。
    2. **補 component 清單** —— `group_concat` 只對 LIMIT 之後的那幾列做。放在第一階段
       會變成 correlated scalar subquery，對每一列候選都跑一次（而 `comps` 沒有
       `(asset_id, go_file_id)` 的 index，每次都是該 asset 內的線性掃）。
    """
    where, args = _find_where(comp, name, path, scope)
    sql = ("SELECT a.path, n.asset_id, n.file_id, n.path, n.is_active " + where +
           " ORDER BY a.path, n.path LIMIT ?")
    rows = con.execute(sql, args + [limit]).fetchall()

    out = []
    for apath, asset_id, fid, npath, active in rows:
        comps = con.execute(
            "SELECT group_concat(type, ' ') FROM comps WHERE asset_id=? AND go_file_id=?",
            (asset_id, fid),
        ).fetchone()[0]
        out.append((apath, fid, npath, active, comps))
    return out


# ParticleSystem 模組內部、gradient/curve 的逐點數值：override 稽核時是雜訊，
# 一個粒子特效就能灌進幾百筆，蓋掉真正想看的 transform / 引用 / 數值改動。
NOISE_PATTERNS = ("Module.", "gradient.", ".curve.", "m_LocalEulerAnglesHint")


def is_noise(prop: str) -> bool:
    return any(p in prop for p in NOISE_PATTERNS)


def overrides(con: sqlite3.Connection, asset_like: str, limit=200):
    """列出資產內所有 prefab instance 的 override（來自 m_Modifications）。

    target 會解析成「來源 prefab 內的階層路徑」，這樣同一個 instance 底下
    多個物件的 override 才分得開。
    """
    return con.execute(
        """
      SELECT a.path, i.file_id, s.path, m.prop, m.value, m.target_file_id,
             COALESCE(tl.label, 'fileID:' || m.target_file_id) AS target_label
        FROM mods m
        JOIN assets a ON a.id = m.asset_id
        JOIN instances i ON i.asset_id = m.asset_id AND i.file_id = m.instance_file_id
        LEFT JOIN assets s ON s.guid = i.source_guid
        -- target 標籤在建索引的最後一階段解析（要跨資產、沿 variant 鏈回溯）
        LEFT JOIN target_labels tl
               ON tl.guid = m.target_guid AND tl.file_id = m.target_file_id
       WHERE a.path LIKE ?
       ORDER BY a.path, i.file_id, target_label, m.prop
       LIMIT ?
        """,
        (asset_like, limit),
    ).fetchall()


def _noise_sql(include_noise: bool):
    """雜訊欄位的 SQL 濾網（跟 is_noise 同一組 pattern，聚合查詢在 SQL 裡就要濾掉）。"""
    if include_noise:
        return "", []
    clause = "".join(" AND m.prop NOT LIKE ?" for _ in NOISE_PATTERNS)
    return clause, [f"%{p}%" for p in NOISE_PATTERNS]


def overrides_count(con: sqlite3.Connection, asset_like: str, noise=False) -> int:
    """同條件的 override 總數（noise=True 才算進特效/曲線欄位）。"""
    clause, extra = _noise_sql(noise)
    return con.execute(
        "SELECT COUNT(*) FROM mods m JOIN assets a ON a.id = m.asset_id "
        "WHERE a.path LIKE ?" + clause,
        [asset_like] + extra,
    ).fetchone()[0]


def overrides_by_target(con: sqlite3.Connection, asset_like: str, limit=50, noise=False):
    """override 的分佈：[(資產, instance fileID, source, 目標節點, 筆數)]，多的排前面。"""
    clause, extra = _noise_sql(noise)
    return con.execute(
        """
      SELECT a.path, i.file_id, s.path,
             COALESCE(tl.label, 'fileID:' || m.target_file_id) AS target_label,
             COUNT(*) AS c
        FROM mods m
        JOIN assets a ON a.id = m.asset_id
        JOIN instances i ON i.asset_id = m.asset_id AND i.file_id = m.instance_file_id
        LEFT JOIN assets s ON s.guid = i.source_guid
        LEFT JOIN target_labels tl
               ON tl.guid = m.target_guid AND tl.file_id = m.target_file_id
       WHERE a.path LIKE ?"""
        + clause
        + """
       GROUP BY a.path, i.file_id, target_label
       ORDER BY c DESC, a.path, i.file_id
       LIMIT ?
        """,
        [asset_like] + extra + [limit],
    ).fetchall()


def scope_stats(con: sqlite3.Connection):
    """各 tier / 副檔名的索引統計，用來調 .uprefab.json 範圍。"""
    return con.execute(
        """
      SELECT a.tier, a.kind, COUNT(*), SUM(a.size),
             (SELECT COUNT(*) FROM nodes n WHERE n.asset_id IN
                (SELECT id FROM assets b WHERE b.tier=a.tier AND b.kind=a.kind))
        FROM assets a GROUP BY a.tier, a.kind ORDER BY a.tier, a.kind
        """
    ).fetchall()


def biggest(con: sqlite3.Connection, limit=10):
    """索引後節點數最多的資產——用來抓「還該再濾掉什麼」。"""
    return con.execute(
        """
      SELECT a.path, a.size, COUNT(n.file_id)
        FROM assets a LEFT JOIN nodes n ON n.asset_id = a.id
       GROUP BY a.id ORDER BY COUNT(n.file_id) DESC LIMIT ?
        """,
        (limit,),
    ).fetchall()


def asset_by_guid(con: sqlite3.Connection, guid: str):
    """guid → (path, kind, tier)；沒索引到就回 None。"""
    return con.execute(
        "SELECT path, kind, tier FROM assets WHERE guid=?", (guid,)
    ).fetchone()


def guid_by_path(con: sqlite3.Connection, path_like: str, limit=20):
    """資產路徑（模糊）→ [(path, guid, kind)]。"""
    return con.execute(
        "SELECT path, guid, kind FROM assets WHERE path LIKE ? ORDER BY path LIMIT ?",
        (path_like, limit),
    ).fetchall()


def anchor(asset_path: str, file_id: int) -> str:
    return f"{asset_path}#{file_id}"


# ── C# 型別目錄（catalog 表）────────────────────────────────────────

def catalog_one(con, cls: str):
    """精確或不分大小寫地找一個 class。"""
    row = con.execute(
        "SELECT class, path, kind, bases, is_abstract, is_obsolete, summary, has_doc, fields "
        "FROM catalog WHERE class=? COLLATE NOCASE", (cls,)).fetchone()
    return row


def catalog_list(con, kind=None, kinds=None, keyword=None, missing=False,
                 include_abstract=False, include_obsolete=False, limit=200):
    sql = ("SELECT class, path, kind, bases, is_abstract, is_obsolete, summary, "
           "has_doc, fields FROM catalog WHERE 1=1")
    args = []
    if kind:
        sql += " AND kind=?"
        args.append(kind)
    elif kinds:
        sql += " AND kind IN (%s)" % ",".join("?" * len(kinds))
        args += list(kinds)
    else:
        sql += " AND kind!=''"
    if not include_abstract:
        sql += " AND is_abstract=0"
    if not include_obsolete:
        sql += " AND is_obsolete=0"
    if keyword:
        sql += " AND (class LIKE ? OR summary LIKE ?)"
        args += [f"%{keyword}%", f"%{keyword}%"]
    if missing:
        sql += " AND (summary='' OR has_doc=0)"
    total = con.execute(f"SELECT COUNT(*) FROM ({sql})", args).fetchone()[0]
    sql += " ORDER BY class LIMIT ?"
    args.append(limit)
    return total, con.execute(sql, args).fetchall()
