"""script guid → C# class name 對照表。

這是離線索引的關鍵濾網：YAML 裡的 MonoBehaviour 只帶
`m_Script: {guid: ...}`，靠這張表就能在不開 Unity 的情況下知道型別，
進而只索引「掛有自家 script」的節點。

Unity 規定 MonoBehaviour 的 class name 必須等於檔名，所以 class 直接取
檔名 stem；namespace 從檔案前段抓（純字串比對，夠用且快）。
"""

from __future__ import annotations

import os
import re

NS_RE = re.compile(r"^\s*namespace\s+([\w.]+)", re.M)
GUID_RE = re.compile(r"^guid: ([0-9a-f]+)", re.M)

SKIP_DIRS = {".git", "Library", "Temp", "Logs", "obj", "Build", "Builds"}


def build_table(root: str):
    """掃全庫 .cs.meta，產出 (guid, class, namespace, 相對路徑)。

    刻意掃「全部」.cs 而不受 config 範圍限制——就算某個 script 所在的
    資料夾不索引，別的 prefab 還是可能引用到它，型別名要查得到。
    """
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS and not d.endswith("~")]
        for fn in filenames:
            if not fn.endswith(".cs.meta"):
                continue
            meta = os.path.join(dirpath, fn)
            try:
                head = open(meta, encoding="utf-8", errors="replace").read(400)
            except OSError:
                continue
            g = GUID_RE.search(head)
            if not g:
                continue
            cs = meta[: -len(".meta")]
            stem = os.path.basename(cs)[: -len(".cs")]
            ns = ""
            try:
                # namespace 一定在檔案前段，讀 4KB 就夠，避免讀進整個大檔
                m = NS_RE.search(open(cs, encoding="utf-8", errors="replace").read(4096))
                if m:
                    ns = m.group(1)
            except OSError:
                pass
            yield g.group(1), stem, ns, os.path.relpath(cs, root)


def class_from_editor_id(editor_class_id: str) -> tuple[str, str]:
    """解析 m_EditorClassIdentifier，回 (class, namespace)。

    格式是 `Assembly::Namespace.Type`，約 68% 的 MonoBehaviour 有填。
    """
    if not editor_class_id or "::" not in editor_class_id:
        return "", ""
    full = editor_class_id.split("::", 1)[1]
    if "." in full:
        ns, cls = full.rsplit(".", 1)
        return cls, ns
    return full, ""
