"""DLL 裡的 public 型別白名單（給 `up verify-skills` 判斷「型別存在」用）。

離線索引只讀專案 .cs，Fusion 這類只以 DLL 形式存在的型別（`NetworkTransform`、
`NetworkBool`、`NetworkProjectConfig`…）在索引裡查不到，verify-skills 就一直誤報
「型別不存在」。這裡直接用純 Python 讀 .NET metadata（ECMA-335 的 TypeDef 表），
不依賴 dotnet / mono —— 讀一顆 DLL 是毫秒級，所以 `up index` 和 verify-skills
都會順手對齊（mtime/size 沒變就跳過）。

要讀哪些 DLL：`.uprefab.json` 的 `dllTypes`（glob 清單），沒寫就用 DEFAULT_GLOBS。
native DLL（沒有 CLI header）會被安靜跳過；managed DLL 讀壞了只印一行提示，
不擋 index 也不擋 verify-skills（退回只用 .cs 索引的舊檢查）。
"""

from __future__ import annotations

import fnmatch
import json
import os
import sqlite3
import struct

# 只放 DLL 路徑 glob，不放型別名 —— 型別一律從 metadata 讀
DEFAULT_GLOBS = ["Assets/Photon/**/*.dll"]

SCHEMA = """
CREATE TABLE IF NOT EXISTS dll_types (name TEXT, ns TEXT, dll TEXT);
CREATE INDEX IF NOT EXISTS dll_types_name ON dll_types(name);
CREATE TABLE IF NOT EXISTS dll_files (path TEXT PRIMARY KEY, mtime REAL, size INTEGER, n INTEGER);
"""

# ECMA-335 table ids
T_MODULE, T_TYPEREF, T_TYPEDEF, T_FIELD, T_METHOD = 0x00, 0x01, 0x02, 0x04, 0x06
T_MODULEREF, T_TYPESPEC, T_ASSEMBLYREF = 0x1A, 0x1B, 0x23

VIS_MASK, VIS_PUBLIC, VIS_NESTED_PUBLIC = 0x7, 0x1, 0x2


class NotManaged(Exception):
    """沒有 CLI header（native DLL），不是錯誤。"""


def read_public_types(path: str) -> list[tuple[str, str]]:
    """回傳 [(型別名, namespace)]，只收 Public / NestedPublic。泛型的 `` `1 `` 尾巴去掉。"""
    with open(path, "rb") as fh:
        data = fh.read()
    if data[:2] != b"MZ":
        raise NotManaged
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        raise NotManaged
    coff = pe + 4
    n_sec, = struct.unpack_from("<H", data, coff + 2)
    opt_size, = struct.unpack_from("<H", data, coff + 16)
    opt = coff + 20
    magic, = struct.unpack_from("<H", data, opt)
    dd = opt + (96 if magic == 0x10B else 112)
    n_dd, = struct.unpack_from("<I", data, dd - 4)
    if n_dd <= 14:
        raise NotManaged
    cli_rva, cli_size = struct.unpack_from("<II", data, dd + 14 * 8)
    if not cli_rva:
        raise NotManaged
    secs = []
    for i in range(n_sec):
        s = opt + opt_size + i * 40
        vsize, va, rsize, raw = struct.unpack_from("<IIII", data, s + 8)
        secs.append((va, max(vsize, rsize), raw))

    def off(rva: int) -> int:
        for va, size, raw in secs:
            if va <= rva < va + size:
                return rva - va + raw
        raise ValueError(f"RVA 0x{rva:x} 不在任何 section")

    md_rva, = struct.unpack_from("<I", data, off(cli_rva) + 8)
    md = off(md_rva)
    if struct.unpack_from("<I", data, md)[0] != 0x424A5342:
        raise ValueError("metadata signature 不對")
    ver_len, = struct.unpack_from("<I", data, md + 12)
    p = md + 16 + ver_len + 2
    n_streams, = struct.unpack_from("<H", data, p)
    p += 2
    streams = {}
    for _ in range(n_streams):
        s_off, s_size = struct.unpack_from("<II", data, p)
        p += 8
        end = data.index(b"\0", p)
        name = data[p:end].decode("ascii")
        p += (len(name) + 4) & ~3
        streams[name] = md + s_off
    tbl = streams.get("#~", streams.get("#-"))
    strings = streams["#Strings"]
    heap_sizes = data[tbl + 6]
    valid, = struct.unpack_from("<Q", data, tbl + 8)
    p = tbl + 24
    rows = {}
    for t in range(64):
        if valid >> t & 1:
            rows[t] = struct.unpack_from("<I", data, p)[0]
            p += 4
    if heap_sizes & 0x40:
        p += 4  # extra data
    s_idx = 4 if heap_sizes & 0x01 else 2
    g_idx = 4 if heap_sizes & 0x02 else 2

    def simple(t: int) -> int:
        return 4 if rows.get(t, 0) >= 1 << 16 else 2

    def coded(tables: list[int], tag_bits: int) -> int:
        return 4 if max(rows.get(t, 0) for t in tables) >= 1 << (16 - tag_bits) else 2

    module_row = 2 + s_idx + 3 * g_idx
    typeref_row = coded([T_MODULE, T_MODULEREF, T_ASSEMBLYREF, T_TYPEREF], 2) + 2 * s_idx
    extends = coded([T_TYPEDEF, T_TYPEREF, T_TYPESPEC], 2)
    typedef_row = 4 + 2 * s_idx + extends + simple(T_FIELD) + simple(T_METHOD)
    p += rows.get(T_MODULE, 0) * module_row + rows.get(T_TYPEREF, 0) * typeref_row

    def rd(at: int, size: int) -> int:
        return struct.unpack_from("<I" if size == 4 else "<H", data, at)[0]

    def string(i: int) -> str:
        a = strings + i
        return data[a:data.index(b"\0", a)].decode("utf-8", "replace")

    out = []
    for r in range(rows.get(T_TYPEDEF, 0)):
        row = p + r * typedef_row
        flags = rd(row, 4)
        if flags & VIS_MASK not in (VIS_PUBLIC, VIS_NESTED_PUBLIC):
            continue
        name = string(rd(row + 4, s_idx)).split("`", 1)[0]
        if not name or "<" in name:
            continue  # compiler-generated
        out.append((name, string(rd(row + 4 + s_idx, s_idx))))
    return out


def _globs(root: str) -> list[str]:
    try:
        with open(os.path.join(root, ".uprefab.json"), encoding="utf-8") as fh:
            g = json.load(fh).get("dllTypes")
        if g:
            return list(g)
    except (OSError, ValueError):
        pass
    return DEFAULT_GLOBS


def _iter_dlls(root: str):
    globs = _globs(root)
    # 只走 glob 的固定前綴，別整個 Assets/ 掃
    bases = sorted({g.split("*", 1)[0].rsplit("/", 1)[0] for g in globs})
    for b in bases:
        full = os.path.join(root, b)
        if not os.path.isdir(full):
            continue
        for dp, dn, fns in os.walk(full):
            dn[:] = [d for d in dn if not d.startswith(".")]
            for f in fns:
                if not f.lower().endswith(".dll"):
                    continue
                rel = os.path.relpath(os.path.join(dp, f), root).replace(os.sep, "/")
                # fnmatch 的 `**/` 至少要吃一層資料夾，補一個去掉 `**/` 的版本才收得到直屬檔案
                if any(fnmatch.fnmatch(rel, g) or fnmatch.fnmatch(rel, g.replace("**/", ""))
                       for g in globs):
                    yield rel


def refresh(con: sqlite3.Connection, root: str, progress=None) -> tuple[int, list[str]]:
    """對齊 DLL 白名單，回 (型別總數, 讀壞的 DLL 提示)。DLL 沒變就不重讀。"""
    con.executescript(SCHEMA)
    known = {p: (m, s) for p, m, s in con.execute("SELECT path, mtime, size FROM dll_files")}
    seen, errors = set(), []
    for rel in _iter_dlls(root):
        seen.add(rel)
        try:
            st = os.stat(os.path.join(root, rel))
        except OSError:
            continue
        if known.get(rel) == (st.st_mtime, st.st_size):
            continue
        con.execute("DELETE FROM dll_types WHERE dll=?", (rel,))
        try:
            types = read_public_types(os.path.join(root, rel))
        except NotManaged:
            types = []
        except Exception as e:  # noqa: BLE001 —— 讀壞不能擋住整個 up
            errors.append(f"{rel}: {type(e).__name__}: {e}")
            continue  # 不寫 dll_files，下次還會重試
        con.executemany("INSERT INTO dll_types VALUES (?,?,?)",
                        [(n, ns, rel) for n, ns in types])
        con.execute("INSERT OR REPLACE INTO dll_files VALUES (?,?,?,?)",
                    (rel, st.st_mtime, st.st_size, len(types)))
        if progress and types:
            progress(f"dll types: {rel} ({len(types)})")
    for gone in set(known) - seen:
        con.execute("DELETE FROM dll_types WHERE dll=?", (gone,))
        con.execute("DELETE FROM dll_files WHERE path=?", (gone,))
    con.commit()
    total, = con.execute("SELECT COUNT(DISTINCT name) FROM dll_types").fetchone()
    return total, errors


def names(con: sqlite3.Connection) -> set[str]:
    try:
        return {n for (n,) in con.execute("SELECT DISTINCT name FROM dll_types")}
    except sqlite3.OperationalError:
        return set()
