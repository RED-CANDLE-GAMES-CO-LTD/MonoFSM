"""`.uprefab.json` 的讀取與路徑比對。"""

from __future__ import annotations

import fnmatch
import json
import os
from dataclasses import dataclass, field

CONFIG_NAME = ".uprefab.json"

DEFAULT_CONFIG = {
    "include": [
        "Assets/0_Gameplay/**",
        "Assets/1_Prototype/**",
        "MonoFSM/**",
        "MonoFSM-Pro/**",
        "MonoFSM-Photon-Fusion/**",
    ],
    "includeShallow": ["Assets/**"],
    "exclude": ["**/Demo/**", "**/Demos/**", "**/Examples/**", "**/Walkthrough/**"],
    "scriptOnly": True,
    "sceneRootFilter": {},
}

# 只掃這些副檔名（文字序列化的 Unity 資產）
ASSET_EXTS = (".prefab", ".unity", ".asset")


@dataclass
class Config:
    root: str
    include: list[str] = field(default_factory=list)
    include_shallow: list[str] = field(default_factory=list)
    exclude: list[str] = field(default_factory=list)
    script_only: bool = True
    scene_root_filter: dict = field(default_factory=dict)

    @staticmethod
    def load(root: str) -> "Config":
        path = os.path.join(root, CONFIG_NAME)
        data = dict(DEFAULT_CONFIG)
        if os.path.exists(path):
            with open(path, encoding="utf-8") as f:
                data.update(json.load(f))
        return Config(
            root=root,
            include=data.get("include", []),
            include_shallow=data.get("includeShallow", []),
            exclude=data.get("exclude", []),
            script_only=data.get("scriptOnly", True),
            scene_root_filter=data.get("sceneRootFilter", {}),
        )

    def write_default(self) -> str:
        path = os.path.join(self.root, CONFIG_NAME)
        with open(path, "w", encoding="utf-8") as f:
            json.dump(DEFAULT_CONFIG, f, indent=2, ensure_ascii=False)
            f.write("\n")
        return path

    def tier(self, rel: str) -> str | None:
        """回傳這個相對路徑的索引層級：'full' / 'shallow' / None（不索引）。

        exclude 命中時是「降級成 shallow」而不是整個丟掉——第三方的 Demo
        資料夾不是自己的 gameplay 內容，但別人的 prefab 引用進去時，還是
        需要查得到它的節點名與型別，否則 override 稽核會顯示 `(source 未索引)`。
        真的要完全排除，就把它從 includeShallow 的範圍拿掉。
        """
        excluded = _match_any(rel, self.exclude)
        if not excluded and _match_any(rel, self.include):
            return "full"
        if _match_any(rel, self.include_shallow):
            return "shallow"
        return None

    def excluded_roots(self, rel: str) -> set[str]:
        """特定 scene 要整棵跳過的 root GameObject 名稱。"""
        entry = self.scene_root_filter.get(rel)
        return set(entry.get("excludeRoots", [])) if entry else set()

    def iter_assets(self):
        """走訪所有納入索引的資產，產出 (相對路徑, tier)。"""
        skip_dirs = {".git", "Library", "Temp", "Logs", "obj", "Build", "Builds"}
        for dirpath, dirnames, filenames in os.walk(self.root):
            dirnames[:] = [d for d in dirnames if d not in skip_dirs and not d.endswith("~")]
            for fn in filenames:
                if not fn.endswith(ASSET_EXTS):
                    continue
                rel = os.path.relpath(os.path.join(dirpath, fn), self.root)
                t = self.tier(rel)
                if t:
                    yield rel, t


def _match_any(rel: str, patterns: list[str]) -> bool:
    # 統一成 posix 分隔符再比對，讓 pattern 在 Windows 上也一致
    rel = rel.replace(os.sep, "/")
    for p in patterns:
        if fnmatch.fnmatch(rel, p) or fnmatch.fnmatch(rel, p.rstrip("/*") + "/*"):
            return True
    return False
