"""Unity YAML 的 streaming document scanner。

Unity 的文字序列化是機器產生的，格式極度規律，所以不需要通用 YAML parser
（PyYAML 對 182MB 的 scene 會直接爆記憶體）。這裡逐行掃描、逐 document 產出，
記憶體只跟「單一 document 大小」成正比。

Document 的形狀固定是：

    --- !u!114 &4154218327269351643
    MonoBehaviour:
      m_GameObject: {fileID: 13587487016138045}
      ...
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Iterator

# --- !u!{classId} &{fileID} [stripped]
DOC_HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)(\s+stripped)?\s*$")
# 縮排 2 空格的頂層欄位： "  m_Name: 值"
TOP_FIELD = re.compile(r"^  ([A-Za-z_][\w]*): ?(.*)$")
# {fileID: N} / {fileID: N, guid: G, type: T}
REF = re.compile(r"\{fileID: (-?\d+)(?:, guid: ([0-9a-f]+), type: (\d+))?\}")


@dataclass
class Doc:
    """一個 Unity YAML document。body 保留原始行，欄位取用走 helper。"""

    class_id: int
    file_id: int
    stripped: bool
    type_name: str
    lines: list[str] = field(default_factory=list)

    def top(self, key: str) -> str | None:
        """取頂層（縮排 2 空格）欄位的純量值。找不到回 None、空值回 ''。"""
        prefix = f"  {key}:"
        for ln in self.lines:
            if ln.startswith(prefix):
                return ln[len(prefix) :].strip()
        return None

    def top_ref(self, key: str) -> tuple[int, str | None] | None:
        """取頂層欄位的 reference，回 (fileID, guid)。"""
        raw = self.top(key)
        if raw is None:
            return None
        m = REF.search(raw)
        return (int(m.group(1)), m.group(2)) if m else None

    def any_ref(self, key: str) -> tuple[int, str | None] | None:
        """取任意縮排層級的 `key: {fileID: …}`。

        PrefabInstance 把 m_TransformParent 藏在 m_Modification 底下（縮排 4），
        所以不能只看頂層。
        """
        needle = key + ":"
        for ln in self.lines:
            if ln.strip().startswith(needle):
                m = REF.search(ln)
                if m:
                    return int(m.group(1)), m.group(2)
        return None

    def block(self, key: str) -> list[str]:
        """取某欄位底下的縮排區塊，欄位可在任意層級。

        以該欄位自身的縮排為基準，收集後續縮排更深的行。YAML 的序列項
        （`- target: …`）與 key 同縮排，所以同層的 `- ` 開頭也算在區塊內。
        """
        needle = key + ":"
        out: list[str] = []
        base: int | None = None
        for ln in self.lines:
            if base is None:
                stripped = ln.lstrip()
                if stripped.startswith(needle):
                    base = len(ln) - len(stripped)
                continue
            if not ln.strip():
                continue
            indent = len(ln) - len(ln.lstrip())
            if indent < base or (indent == base and not ln.lstrip().startswith("- ")):
                break
            out.append(ln)
        return out

    def iter_refs(self) -> Iterator[tuple[str, int, str | None]]:
        """走訪 document 內所有 reference，回 (欄位名, fileID, guid)。

        欄位名取「最近一個看得到的 key」——陣列元素會沿用父欄位名，
        對反查引用來說這個粒度已經夠用。
        """
        current_key = "?"
        for ln in self.lines:
            m = TOP_FIELD.match(ln)
            if m:
                current_key = m.group(1)
            else:
                # 巢狀 key（縮排 > 2）也更新，讓 refs 的欄位名更精確
                nested = re.match(r"^ {4,}(?:- )?([A-Za-z_][\w]*): ", ln)
                if nested:
                    current_key = nested.group(1)
            for r in REF.finditer(ln):
                fid = int(r.group(1))
                if fid != 0:
                    yield current_key, fid, r.group(2)


def scan(path: str) -> Iterator[Doc]:
    """逐 document 掃描一個 Unity YAML 檔。非 Unity YAML 直接回空。"""
    with open(path, "r", encoding="utf-8", errors="replace") as f:
        first = f.readline()
        if not first.startswith("%YAML"):
            return
        doc: Doc | None = None
        pending_header: tuple[int, int, bool] | None = None
        for ln in f:
            ln = ln.rstrip("\n")
            h = DOC_HEADER.match(ln)
            if h:
                if doc is not None:
                    yield doc
                    doc = None
                pending_header = (int(h.group(1)), int(h.group(2)), bool(h.group(3)))
                continue
            if pending_header is not None:
                # header 的下一行是型別名 "MonoBehaviour:"
                type_name = ln.rstrip(":").strip()
                cid, fid, stripped = pending_header
                doc = Doc(cid, fid, stripped, type_name)
                pending_header = None
                continue
            if doc is not None:
                doc.lines.append(ln)
        if doc is not None:
            yield doc


def parse_modifications(lines: list[str]) -> list[dict]:
    """解析 PrefabInstance 的 m_Modifications 區塊。

    每筆的形狀：
        - target: {fileID: 7, guid: aaa, type: 3}
          propertyPath: m_Name
          value: Foo
          objectReference: {fileID: 0}
    """
    mods: list[dict] = []
    cur: dict | None = None
    for ln in lines:
        s = ln.strip()
        if s.startswith("- target:"):
            if cur:
                mods.append(cur)
            cur = {}
            m = REF.search(s)
            if m:
                cur["target_file_id"] = int(m.group(1))
                cur["target_guid"] = m.group(2)
        elif cur is not None:
            if s.startswith("propertyPath:"):
                cur["prop"] = s[len("propertyPath:") :].strip()
            elif s.startswith("value:"):
                cur["value"] = s[len("value:") :].strip()
            elif s.startswith("objectReference:"):
                m = REF.search(s)
                if m and int(m.group(1)) != 0:
                    cur["value"] = f"→{{fileID: {m.group(1)}}}"
    if cur:
        mods.append(cur)
    return mods
