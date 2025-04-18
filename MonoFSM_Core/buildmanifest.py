import os
import sys
import json
import re
import uuid

def extract_cs_summary(filepath):
    summary = {}
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            code = f.read()
        # 取得namespace
        namespace_match = re.search(r'namespace\s+([\w\.]+)', code)
        namespace = namespace_match.group(1) if namespace_match else None
        class_match = re.search(r'class\s+(\w+)(?:\s*:\s*([\w,\s]+))?', code)
        if not class_match:
            return None
        class_name = class_match.group(1)
        base_and_interfaces = class_match.group(2) or ''
        bases = [b.strip() for b in base_and_interfaces.split(',')] if base_and_interfaces else []
        base_class = bases[0] if bases and not bases[0].startswith('I') else None
        interfaces = [b for b in bases if b.startswith('I')]
        summary['class'] = class_name
        if namespace:
            summary['namespace'] = namespace
        if base_class:
            summary['base'] = base_class
        if interfaces:
            summary['interfaces'] = interfaces

        # 判斷是否為Unity Component
        if base_class in ('MonoBehaviour', 'AbstractDescriptionBehaviour'):
            summary['isComponent'] = True

        # 擷取 AutoParent/AutoChildren/Auto 欄位，使用 regex
        auto_refs = []
        field_pattern = re.compile(
            r'\[(AutoParent|AutoChildren|Auto)[^\]]*\](?:\s*\[[^\]]*\])*\s*(?:public|protected|private|internal)?\s*(?:static\s+)?(?:readonly\s+)?([\w<>\[\], ]+)\s+(\w+)\s*(?:;|=)',
            re.MULTILINE
        )
        for m in field_pattern.finditer(code):
            attr = m.group(1)
            typ = m.group(2).strip()
            name = m.group(3)
            auto_refs.append({'attribute': attr, 'type': typ, 'name': name})
        if auto_refs:
            summary['autoReferences'] = auto_refs

        return summary
    except Exception as e:
        return {'error': str(e)}

def get_meta_guid(cs_filepath):
    meta_path = cs_filepath + '.meta'
    if not os.path.exists(meta_path):
        # 嘗試移除 .cs 再加 .meta（Unity 2020+ 有時 .cs 和 .meta 不會同名）
        alt_meta_path = cs_filepath[:-3] + '.meta'
        if os.path.exists(alt_meta_path):
            meta_path = alt_meta_path
        else:
            return None
    try:
        with open(meta_path, 'r', encoding='utf-8') as f:
            for line in f:
                if line.strip().startswith('guid:'):
                    return line.strip().split('guid:')[1].strip()
    except Exception:
        return None
    return None

def build_tree(path):
    tree = {}
    files = []
    for entry in sorted(os.listdir(path)):
        full_path = os.path.join(path, entry)
        if os.path.isdir(full_path):
            subtree = build_tree(full_path)
            if subtree:  # Only add non-empty folders
                tree[entry] = subtree
        elif entry.endswith('.cs'):
            summary = extract_cs_summary(full_path)
            meta_guid = get_meta_guid(full_path)
            files.append({'filename': entry, 'meta_guid': meta_guid, 'summary': summary})
    if files:
        tree['__filelist__'] = files  # 用 __filelist__ 暫存，後面 clean_tree 會處理
    return tree

def clean_tree(tree):
    # 若只有 __filelist__，直接回傳 list
    if set(tree.keys()) == {'__filelist__'}:
        return tree['__filelist__']
    # 否則遞迴處理
    result = {}
    for k, v in tree.items():
        if k == '__filelist__':
            result = v if not result else {**result, 'files': v}
        else:
            result[k] = clean_tree(v) if isinstance(v, dict) else v
    return result

def add_ids(tree, parent_path=""):
    if isinstance(tree, list):
        for entry in tree:
            if (
                isinstance(entry, dict)
                and 'summary' in entry
                and entry['summary']
                and isinstance(entry['summary'], dict)
                and 'class' in entry['summary']
                and 'meta_guid' in entry and entry['meta_guid']
            ):
                # 用 meta_guid + class name 當 id
                class_id = f"{entry['meta_guid']}#{entry['summary']['class']}"
                # entry['id'] = str(uuid.uuid5(uuid.NAMESPACE_URL, class_id))
            for v in entry.values() if isinstance(entry, dict) else []:
                add_ids(v, parent_path)
    elif isinstance(tree, dict):
        for k, v in tree.items():
            if isinstance(v, (dict, list)):
                add_ids(v, parent_path + "/" + k if parent_path else k)

def build_manifest_root(tree):
    return {
        "manifestVersion": "1.0.0",
        "description": "MonoFSM API and file manifest for tooling and automation.",
        "intendedFor": ["AI", "IDE", "DocsGen"],
        "customData": {},
        **tree
    }

def generate_manifest(root_path):
    """
    產生該資料夾的的manifest。

    Args:
        root_path: 資料夾的root目錄。

    產生manifest後，會將它存到root目錄下面的.AI/<root_folder_name>_manifest.json。
    """
    tree = build_tree(root_path)
    tree = clean_tree(tree)
    add_ids(tree)
    manifest = build_manifest_root(tree)

    # 取得母資料夾名稱
    abs_root = os.path.abspath(root_path)
    parent_folder = os.path.basename(abs_root.rstrip(os.sep))
    ai_dir = os.path.join(abs_root, '.AI')
    os.makedirs(ai_dir, exist_ok=True)
    manifest_path = os.path.join(ai_dir, f'{parent_folder}_manifest.json')
    with open(manifest_path, 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)

if __name__ == '__main__':
    root_path = sys.argv[1] if len(sys.argv) > 1 else '.'
    generate_manifest(root_path)