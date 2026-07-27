"""find / overrides / scope stats 的查詢實作。輸出以 anchor 為主。

anchor 格式：`Assets/…/Foo.prefab#<fileID>`
fileID 對改名穩定，是餵回 Unity 精讀（Phase 3）用的定址。
"""

from __future__ import annotations

import sqlite3


def find(con: sqlite3.Connection, comp=None, name=None, path=None, limit=50):
    """依 component 型別 / 節點名 / 資產路徑定位節點。"""
    sql = """
      SELECT a.path, n.file_id, n.path, n.is_active,
             (SELECT group_concat(c.type, ' ')
                FROM comps c WHERE c.asset_id=n.asset_id AND c.go_file_id=n.file_id)
        FROM nodes n JOIN assets a ON a.id = n.asset_id
       WHERE 1=1
    """
    args: list = []
    if comp:
        sql += """ AND EXISTS (SELECT 1 FROM comps c
                    WHERE c.asset_id=n.asset_id AND c.go_file_id=n.file_id
                      AND c.type LIKE ?)"""
        args.append(comp)
    if name:
        sql += " AND n.name LIKE ?"
        args.append(name)
    if path:
        sql += " AND a.path LIKE ?"
        args.append(path)
    sql += " ORDER BY a.path, n.path LIMIT ?"
    args.append(limit)
    return con.execute(sql, args).fetchall()


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


def anchor(asset_path: str, file_id: int) -> str:
    return f"{asset_path}#{file_id}"
