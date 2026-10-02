"""find / overrides / scope stats 的查詢實作。輸出以 anchor 為主。

anchor 格式：`Assets/…/Foo.prefab#<fileID>`
fileID 對改名穩定，是餵回 Unity 精讀（Phase 3）用的定址。
"""

from __future__ import annotations

import sqlite3


def _find_where(comp=None, name=None, path=None, scope="full", paths=None):
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
    if paths:
        # 展開 prefab 繼承鏈後，條件是一組明確路徑（本檔 + 各層 base / nested 來源），
        # 不能再用單一 LIKE 表達
        sql += " AND a.path IN (%s)" % ",".join("?" * len(paths))
        args.extend(paths)
    elif path:
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
               scope="full", paths=None) -> int:
    """同條件的總命中數 —— 讓 limit 切掉時能講出「50 / 共 4132」。"""
    sql, args = _find_where(comp, name, path, scope, paths)
    return con.execute("SELECT COUNT(*) " + sql, args).fetchone()[0]


def find_totals(con: sqlite3.Connection, comp=None, name=None, path=None,
                scope="full", paths=None):
    """(總命中數, 涵蓋幾個資產) —— --by-asset 的表尾要能講「列出的只是前幾名」。"""
    sql, args = _find_where(comp, name, path, scope, paths)
    return con.execute(
        "SELECT COUNT(*), COUNT(DISTINCT a.path) " + sql, args).fetchone()


def find_by_asset(con: sqlite3.Connection, comp=None, name=None, path=None,
                  limit=50, scope="full", paths=None):
    """同條件的分佈：[(資產路徑, 命中數)]，多的排前面。"""
    sql, args = _find_where(comp, name, path, scope, paths)
    sql = ("SELECT a.path, COUNT(*) " + sql +
           " GROUP BY a.path ORDER BY COUNT(*) DESC, a.path LIMIT ?")
    return con.execute(sql, args + [limit]).fetchall()


def find(con: sqlite3.Connection, comp=None, name=None, path=None, limit=50,
         scope="full", paths=None):
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
    where, args = _find_where(comp, name, path, scope, paths)
    sql = ("SELECT a.path, n.asset_id, n.file_id, n.path, n.is_active, n.layer " + where +
           " ORDER BY a.path, n.path LIMIT ?")
    rows = con.execute(sql, args + [limit]).fetchall()

    out = []
    for apath, asset_id, fid, npath, active, layer in rows:
        comps = con.execute(
            "SELECT group_concat(type, ' ') FROM comps WHERE asset_id=? AND go_file_id=?",
            (asset_id, fid),
        ).fetchone()[0]
        out.append((apath, fid, npath, active, comps, layer or 0))
    return out


def layer_names(root: str) -> list[str]:
    """ProjectSettings/TagManager.asset 的 `layers:` 清單 → index 對應名字（沒名字的是空字串）。

    離線索引的 nodes.layer 只存 m_Layer 數字；印給人看要轉成名字才有意義。
    讀不到檔就回空 list，呼叫端退回印數字。
    """
    import os
    path = os.path.join(root, "ProjectSettings", "TagManager.asset")
    names: list[str] = []
    try:
        with open(path, encoding="utf-8") as f:
            inside = False
            for line in f:
                if line.startswith("  layers:"):
                    inside = True
                    continue
                if inside:
                    if not line.startswith("  - ") and line.strip() != "-":
                        break
                    names.append(line.strip()[1:].strip())
    except OSError:
        return []
    return names


def layer_label(names: list[str], layer: int) -> str:
    """0（Default）回空字串 —— find 只在 layer 不是 Default 時才印，省輸出。"""
    if not layer:
        return ""
    name = names[layer] if 0 <= layer < len(names) and names[layer] else str(layer)
    return f"layer={name}"


# ── prefab 繼承鏈（variant base / nested prefab）─────────────────────
#
# 離線索引只存「每個檔案自己 YAML 裡寫出來的節點」。variant 繼承來的節點與
# nested prefab 內部的節點都不在自己的檔案裡，直接查會回 (no match) —— 一個
# 看起來像定論的假陰性。這裡沿 instances.source_guid 把來源檔案一起拉進查詢範圍，
# 讓 find 至少能指出「那個節點存在，在 base 裡」。


def assets_matching(con: sqlite3.Connection, path_like: str, limit: int = 200):
    """--path 的 LIKE 條件實際命中哪些資產 → [(path, guid)]。"""
    return con.execute(
        "SELECT path, guid FROM assets WHERE path LIKE ? ORDER BY path LIMIT ?",
        (path_like, limit),
    ).fetchall()


def prefab_sources(con: sqlite3.Connection, asset_paths, max_depth: int = 8):
    """沿 instances.source_guid 遞迴展開 variant base 與 nested prefab 來源。

    回 {source_path: (depth, via_path)}，不含輸入本身。depth 1 = 直接來源。
    索引不到的 guid（不在索引範圍內的資產）會被跳過，不會假裝解出來。
    """
    seen = {p: 0 for p in asset_paths}
    out: dict[str, tuple[int, str]] = {}
    frontier = list(asset_paths)
    for depth in range(1, max_depth + 1):
        if not frontier:
            break
        rows = con.execute(
            "SELECT a.path, i.source_guid FROM instances i JOIN assets a ON a.id = i.asset_id "
            "WHERE a.path IN (%s) AND i.source_guid IS NOT NULL"
            % ",".join("?" * len(frontier)),
            frontier,
        ).fetchall()
        nxt = []
        for via, guid in rows:
            row = con.execute(
                "SELECT path FROM assets WHERE guid=?", (guid,)).fetchone()
            if not row:
                continue
            src = row[0]
            if src in seen:
                continue
            seen[src] = depth
            out[src] = (depth, via)
            nxt.append(src)
        frontier = nxt
    return out


def has_instances(con: sqlite3.Connection, asset_paths) -> bool:
    """這批資產裡有沒有任何 prefab instance（= 是 variant 或含 nested prefab）。"""
    if not asset_paths:
        return False
    return con.execute(
        "SELECT 1 FROM instances i JOIN assets a ON a.id = i.asset_id "
        "WHERE a.path IN (%s) LIMIT 1" % ",".join("?" * len(asset_paths)),
        list(asset_paths),
    ).fetchone() is not None


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


def node_name(con: sqlite3.Connection, asset_path: str, file_id: int) -> str | None:
    """anchor 的節點**名字**（只有名字，不給路徑）；查不到回 None。

    給 EditAnchor.Resolve 當最後一層 fallback（fileID 比不到時用名稱唯一比對）。
    **節點路徑一律問 Unity** —— 以前這裡有 node_by_file_id / _is_rooted / native_path 從離線
    parent 鏈推路徑，但 nested instance 的名字是來源 prefab 的，被 override 改名後離線看不到
    （實測 Base Character.prefab 的 `[Anim] Base Character` 被記成 `Base Character`，
    路徑少四層還被判成從 root 起算），錯的路徑比沒有更糟，2026-10-01 整套拿掉。
    """
    row = con.execute(
        "SELECT n.name FROM assets a JOIN nodes n ON n.asset_id = a.id "
        "WHERE a.path=? AND n.file_id=?", (asset_path, file_id)).fetchone()
    return row[0] if row else None


def path_may_be_truncated(con: sqlite3.Connection, asset_id: int, asset_path: str,
                          file_id: int, hops=64) -> bool:
    """find 第二行的離線路徑「可能少層 / 名字不對」→ True。只回 bool，**不推路徑**。

    只用離線索引：find 一次掃很多資產，逐筆問 Unity 太慢。判斷方式（任一成立就 True）：
    - 鏈上（含自己）有 prefab instance 成員（src_guid 非空）：名字是來源 prefab 的，
      override 改名看不到，接點也可能接錯（實測 Base Character.prefab 的 `[Anim] Base Character`
      被印成 `Base Character/[Anim] Base Character`，真路徑多三層）
    - 鏈斷在索引查不到的節點
    - prefab 的鏈頂不是檔名同名那顆（離線唯一認得出 prefab root 的線索）
    全是本檔原生節點時回 False —— 這種才不標，標太多大家會直接無視。
    """
    cur, top = file_id, None
    for _ in range(hops):
        r = con.execute("SELECT name, parent_file_id, src_guid FROM nodes "
                        "WHERE asset_id=? AND file_id=?", (asset_id, cur)).fetchone()
        if r is None or r[2]:
            return True
        top, cur = r[0], r[1]
        if not cur:
            break
    else:
        return True
    if asset_path.endswith(".prefab"):
        return top != asset_path.rsplit("/", 1)[-1].rsplit(".", 1)[0]
    return False


def asset_id_of(con: sqlite3.Connection, asset_path: str) -> int | None:
    row = con.execute("SELECT id FROM assets WHERE path=?", (asset_path,)).fetchone()
    return row[0] if row else None


def anchor(asset_path: str, file_id: int) -> str:
    return f"{asset_path}#{file_id}"


# ── C# 型別目錄（catalog 表）────────────────────────────────────────

def catalog_one(con, cls: str):
    """精確或不分大小寫地找一個 class。"""
    row = con.execute(
        "SELECT class, path, kind, bases, is_abstract, is_obsolete, summary, has_doc, fields "
        "FROM catalog WHERE class=? COLLATE NOCASE", (cls,)).fetchone()
    return row


def serial_find(con, keyword: str, limit: int = 20):
    """名稱含 keyword（不分大小寫）的 [Serializable] plain class / struct。"""
    try:
        return con.execute(
            "SELECT class, path, kw, bases, summary, fields FROM serial_types "
            "WHERE class LIKE ? ORDER BY length(class), class LIMIT ?",
            (f"%{keyword}%", limit)).fetchall()
    except Exception:
        return []


def serial_one(con, cls: str):
    """精確（不分大小寫）找 [Serializable] plain class / struct；同名多支檔案時全回。"""
    try:
        return con.execute(
            "SELECT class, path, kw, bases, summary, fields FROM serial_types "
            "WHERE class=? COLLATE NOCASE ORDER BY path", (cls,)).fetchall()
    except Exception:
        return []


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
