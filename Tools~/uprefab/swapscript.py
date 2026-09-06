"""把 prefab / scene 裡某顆 MonoBehaviour 換成另一個 C# 型別（離線、文字層）。

**為什麼一定要離線做**：C# 重構把欄位從 A 型別搬到 B 型別之後，A 的 YAML document
裡那些欄位就變成孤兒 —— Unity 載入時直接丟掉，所以 `SerializedObject` / `peek`
一律看不到它們（`up prefab peek` 只會回「型別上沒有這個欄位」）。值只還活在檔案文字裡，
任何走 Unity API 的搬移都會搬到空值。這裡直接改 `m_Script` 的 guid：同名欄位會被
新型別原樣吃下去，不同名的留在檔裡由 Unity 下次存檔時丟掉。

限制（刻意的）：
- 只處理「自有 document」（`--- !u!114 &id` 段落）。variant 上繼承節點的 override 值
  住在 `m_Modifications`，型別是 base 決定的，換不了 —— 那種情況要去 base 改。
- 不驗證新型別有哪些欄位（離線不做 C# 解析）。要清掉舊欄位用 `--drop`。
- 改完 Unity 端要 reimport 才看得到（`up index` 只更新離線索引）。
"""

from __future__ import annotations

import os
import re
import shutil

MB_HEADER = re.compile(r"^--- !u!114 &(-?\d+)\s*$")
GO_HEADER = re.compile(r"^--- !u!1 &(-?\d+)\s*$")
ANY_HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)")
SCRIPT_LINE = re.compile(r"^  m_Script: \{fileID: (-?\d+), guid: ([0-9a-f]+), type: (\d+)\}\s*$")
EDITOR_ID = re.compile(r"^  m_EditorClassIdentifier: (.*)$")
GO_REF = re.compile(r"^  m_GameObject: \{fileID: (-?\d+)\}\s*$")
NAME_LINE = re.compile(r"^  m_Name: (.*)$")


def lookup_script(con, class_name: str):
    """class name → (guid, ns, path)。同名多支會全部回傳，讓呼叫端報錯。"""
    rows = con.execute(
        "SELECT guid, ns, path FROM scripts WHERE class = ?", (class_name,)
    ).fetchall()
    return rows


def _gameobject_names(lines: list[str]) -> dict[int, str]:
    names: dict[int, str] = {}
    cur = None
    for ln in lines:
        m = GO_HEADER.match(ln)
        if m:
            cur = int(m.group(1))
            continue
        if cur is not None:
            if ln.startswith("--- "):
                cur = None
                continue
            n = NAME_LINE.match(ln)
            if n:
                names[cur] = n.group(1).strip()
                cur = None
    return names


def swap(path: str, from_guid: str, to_guid: str, to_editor_id: str,
         only_fileids: set[int] | None = None,
         drop_fields: tuple[str, ...] = (),
         dry_run: bool = False,
         backup_dir: str | None = None):
    """hits = [(docFileId, goFileId, goName, [保留的欄位名], [原始 "欄位: 值" 行])]。

    `raw` 這欄是重點：孤兒欄位（C# 上已經沒有、只剩檔案裡有）的值只有這裡看得到，
    `up prefab peek` 走 Unity 一律回「型別上沒有這個欄位」。所以 `--dry-run` 同時
    也是「讀孤兒值」的唯一手段。"""
    with open(path, encoding="utf-8", errors="replace") as fh:
        lines = fh.read().split("\n")

    go_names = _gameobject_names(lines)

    hits = []
    # 先掃出每個 MonoBehaviour document 的行區間
    doc_start = None
    doc_id = None
    docs = []
    for i, ln in enumerate(lines):
        m = ANY_HEADER.match(ln)
        if m:
            if doc_start is not None:
                docs.append((doc_id, doc_start, i))
            mb = MB_HEADER.match(ln)
            doc_start, doc_id = (i, int(mb.group(1))) if mb else (None, None)
    if doc_start is not None:
        docs.append((doc_id, doc_start, len(lines)))

    out_lines = list(lines)
    drop_idx: set[int] = set()
    for did, s, e in docs:
        seg = lines[s:e]
        script_i = None
        for j, ln in enumerate(seg):
            sm = SCRIPT_LINE.match(ln)
            if sm:
                if sm.group(2) != from_guid:
                    break
                script_i = j
                break
        if script_i is None:
            continue
        if only_fileids is not None and did not in only_fileids:
            continue

        go_id = 0
        kept = []
        for j, ln in enumerate(seg):
            g = GO_REF.match(ln)
            if g:
                go_id = int(g.group(1))
            f = re.match(r"^  (_[\w]+): ", ln)
            if f:
                if f.group(1) in drop_fields:
                    drop_idx.add(s + j)
                else:
                    kept.append(f.group(1))
        raw = [ln[2:] for ln in seg if re.match(r"^  _[\w]+: ", ln)]
        hits.append((did, go_id, go_names.get(go_id, "?"), kept, raw))

        out_lines[s + script_i] = (
            f"  m_Script: {{fileID: 11500000, guid: {to_guid}, type: 3}}"
        )
        for j, ln in enumerate(seg):
            if EDITOR_ID.match(ln):
                out_lines[s + j] = f"  m_EditorClassIdentifier: {to_editor_id}"
                break

    if hits and not dry_run:
        # 備份刻意不放 Assets/ 底下 —— .bak 會被 Unity 當成未知資產 import 並生 .meta
        if backup_dir:
            os.makedirs(backup_dir, exist_ok=True)
            shutil.copyfile(path, os.path.join(backup_dir, os.path.basename(path) + ".bak"))
        final = [l for i, l in enumerate(out_lines) if i not in drop_idx]
        with open(path, "w", encoding="utf-8") as fh:
            fh.write("\n".join(final))
    return hits
