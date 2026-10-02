using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Abort = MonoFSM.Editor.PrefabEditing.EditResolve.EditAbort;
using Object = UnityEngine.Object;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// PrefabEdit 的 scene 版：建 / 開 / 存 scene，加上同一套路徑語彙的寫入原語與讀取。
    ///
    /// 跟 PrefabEdit 的三個差別，都是 scene 本身的性質造成的：
    /// 1. **多 root** —— nodePath 第一段是 root object 名稱，沒有「唯一 root」可以留空。
    /// 2. **不需要 load/save 配對** —— scene 一直開著，所以可以在一次 dynamic code 呼叫裡
    ///    連續下十幾個原語，最後 Save() 一次。PrefabEdit 每個原語都要 load/save 一輪。
    /// 3. **有 runtime** —— Count() 在 Play Mode 下也能用，這是驗證「定時生成」有沒有生對數量
    ///    的手段：不用 dump 整個 hierarchy，只回傳數字。
    ///
    /// 全部方法都回傳字串（成功訊息或 `# 未修改：原因`），配合 uloop execute-dynamic-code 的
    /// `return`。失敗一律不存檔。
    /// </summary>
    public static class SceneEdit
    {
        // ---- scene 生命週期 ----

        /// <summary>
        /// 建一個新 scene 並存檔（會取代目前開著的 scene；有 dirty scene 時預設拒絕，見 CheckDirtyOpenScenes）。
        /// </summary>
        /// <param name="scenePath">例：Assets/Scenes/Test.unity</param>
        /// <param name="withDefaults">true = 帶 Main Camera + Directional Light</param>
        /// <param name="saveDirty">true = 使用者明確同意，先存掉 dirty scene 再切</param>
        public static string NewScene(string scenePath, bool withDefaults = false, bool saveDirty = false)
        {
            return Guard(() =>
            {
                if (!scenePath.EndsWith(".unity"))
                    throw new Abort($"scenePath 要以 .unity 結尾：{scenePath}");
                if (Application.isPlaying)
                    throw new Abort("Play Mode 中不能建 scene");

                var saved = CheckDirtyOpenScenes(saveDirty);

                var setup = withDefaults
                    ? NewSceneSetup.DefaultGameObjects
                    : NewSceneSetup.EmptyScene;
                var scene = EditorSceneManager.NewScene(setup, NewSceneMode.Single);

                EnsureDirectory(scenePath);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                    throw new Abort($"{saved}存檔失敗：{scenePath}");
                return $"{saved}建立 scene {scenePath}（{(withDefaults ? "含" : "不含")}預設物件）";
            });
        }

        /// <summary>
        /// 複製一個既有 scene 當模板再開起來。
        ///
        /// 為什麼不是 NewScene：一個能跑的 gameplay scene 需要 WorldUpdateSimulator、
        /// SpawnProcessor、PoolManager、AutoAttributeManager… 這些底盤。空 scene 自己拼
        /// 會漏，而且漏掉的東西只會在 Play Mode 才炸。專案已經有現成模板
        /// （`Assets/1_Prototype/Module Test/Network FSM Template.unity`），複製它才對。
        /// </summary>
        public static string CopyScene(string templatePath, string newScenePath, bool saveDirty = false)
        {
            return Guard(() =>
            {
                if (!newScenePath.EndsWith(".unity"))
                    throw new Abort($"newScenePath 要以 .unity 結尾：{newScenePath}");
                if (Application.isPlaying)
                    throw new Abort("Play Mode 中不能建 scene");
                // 外部（git / rm）動過檔案時 AssetDatabase 還握著舊狀態，
                // 「已存在」判斷會誤判 —— 先同步一次
                AssetDatabase.Refresh();
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(templatePath) == null)
                    throw new Abort($"找不到模板 scene: {templatePath}");
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(newScenePath) != null)
                    throw new Abort($"{newScenePath} 已存在，不覆蓋");

                // 要在 CopyAsset 之前：dirty 擋下來時不會留下一份複製到一半的 scene
                var saved = CheckDirtyOpenScenes(saveDirty);

                EnsureDirectory(newScenePath);
                if (!AssetDatabase.CopyAsset(templatePath, newScenePath))
                    throw new Abort($"{saved}複製失敗：{templatePath} -> {newScenePath}");
                AssetDatabase.ImportAsset(newScenePath);

                var scene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);
                return $"{saved}複製 scene {newScenePath}\n" +
                       $"  模板: {templatePath}\n" +
                       $"  已開啟，{scene.rootCount} 個 root";
            });
        }

        public static string OpenScene(string scenePath, bool saveDirty = false)
        {
            return Guard(() =>
            {
                if (Application.isPlaying)
                    throw new Abort("Play Mode 中不能開 scene");
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                    throw new Abort($"找不到 scene: {scenePath}");
                var saved = CheckDirtyOpenScenes(saveDirty);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                return $"{saved}開啟 {scene.path}（{scene.rootCount} 個 root）";
            });
        }

        public static string Save()
        {
            return Guard(() =>
            {
                var scene = Active();
                if (string.IsNullOrEmpty(scene.path))
                    throw new Abort("scene 還沒有路徑，先用 NewScene 建一個");
                if (!EditorSceneManager.SaveScene(scene))
                    throw new Abort($"存檔失敗：{scene.path}");
                return $"存檔 {scene.path}（{scene.rootCount} 個 root）";
            });
        }

        // ---- 寫入原語 ----

        /// <summary>
        /// 建節點。parentPath 留空 = 建成 scene 的 root object。
        /// </summary>
        public static string AddNode(string parentPath, string name, params string[] componentTypes)
        {
            return Guard(() =>
            {
                var scene = Active();
                Transform parent = null;
                if (!string.IsNullOrEmpty(parentPath))
                {
                    parent = EditResolve.NodeInRoots(Roots(scene), parentPath);
                    // 已存在就跳過而不是 abort —— 批次常常要修一行再整份重跑，
                    // 「重複建立」在這種流程裡是預期狀況，不是錯誤
                    if (parent.Find(name) != null)
                        return $"（跳過）{parentPath}/{name} 已存在";
                }
                else if (Roots(scene).Any(g => g != null && g.name == name))
                {
                    return $"（跳過）root object '{name}' 已存在";
                }

                var go = new GameObject(name);
                if (parent != null) go.transform.SetParent(parent, false);
                else SceneManager.MoveGameObjectToScene(go, scene);

                var added = new List<string>();
                foreach (var typeName in componentTypes ?? Array.Empty<string>())
                {
                    var type = EditResolve.CompType(typeName);
                    if (go.GetComponent(type) != null) continue;
                    go.AddComponent(type);
                    added.Add(type.Name);
                }

                Dirty();
                var full = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";
                EditBatch.Touch(full);
                return $"建立 {full}  <{string.Join(", ", added)}>" + LayerLintSuffix(go.transform);
            });
        }

        /// <summary>
        /// 把 prefab 實例化進 scene（保持 prefab 連結）。parentPath 留空 = 放 root。
        /// </summary>
        public static string AddPrefab(string prefabPath, string parentPath = null, string name = null)
        {
            return Guard(() =>
            {
                var scene = Active();
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (asset == null) throw new Abort($"找不到 prefab: {prefabPath}");

                var parent = string.IsNullOrEmpty(parentPath)
                    ? null
                    : EditResolve.NodeInRoots(Roots(scene), parentPath);

                var existing = parent != null
                    ? parent.Find(name ?? asset.name)
                    : Roots(scene).FirstOrDefault(g => g != null && g.name == (name ?? asset.name))
                        ?.transform;
                if (existing != null)
                    return $"（跳過）{(name ?? asset.name)} 已存在";

                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                if (go == null) throw new Abort($"實例化失敗: {prefabPath}");
                if (parent != null) go.transform.SetParent(parent, false);
                if (!string.IsNullOrEmpty(name)) go.name = name;

                Dirty();
                var full = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";
                EditBatch.Touch(full);
                return $"放入 {full}  <- res:{prefabPath}";
            });
        }

        public static string SetField(
            string nodePath, string componentType, string fieldPath, object value)
        {
            return Guard(() =>
            {
                var comp = CompAt(nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = EditResolve.Prop(so, fieldPath, comp);
                var before = EditResolve.Preview(prop);
                EditResolve.ApplyValue(prop, value, fieldPath);
                so.ApplyModifiedPropertiesWithoutUndo();
                Dirty();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath}: " +
                       $"{before} -> {EditResolve.Preview(prop)}";
            });
        }

        public static string SetRef(
            string nodePath, string componentType, string fieldPath,
            string targetNodePath, string targetComponentType = null)
        {
            return Guard(() =>
            {
                var comp = CompAt(nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = EditResolve.Prop(so, fieldPath, comp);
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    throw new Abort(
                        $"'{fieldPath}' 是 {prop.propertyType}，不是物件引用；請改用 SetField");

                var target = EditResolve.NodeInRoots(Roots(Active()), targetNodePath);
                var targetComp = EditResolve.RefTarget(
                    target, targetNodePath, comp, fieldPath, targetComponentType);

                prop.objectReferenceValue = targetComp;
                so.ApplyModifiedPropertiesWithoutUndo();
                Dirty();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath} -> " +
                       $"{targetNodePath}.{targetComp.GetType().Name}";
            });
        }

        /// <summary>欄位指向 asset（prefab / ScriptableObject），會按欄位型別取對應 component。</summary>
        public static string SetAssetRef(
            string nodePath, string componentType, string fieldPath, string targetAssetPath)
        {
            return Guard(() =>
            {
                var comp = CompAt(nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = EditResolve.Prop(so, fieldPath, comp);
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    throw new Abort($"'{fieldPath}' 是 {prop.propertyType}，不是物件引用");

                var resolved = AssetRef.Resolve(targetAssetPath, comp, fieldPath);
                prop.objectReferenceValue = resolved;
                if (resolved != null && prop.objectReferenceValue == null)
                    throw new Abort(
                        $"'{fieldPath}' 拒收 {resolved.GetType().Name}（{targetAssetPath}）：型別跟欄位宣告型別對不上，Unity 會靜默寫成 null");
                so.ApplyModifiedPropertiesWithoutUndo();
                Dirty();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath} -> res:{targetAssetPath}";
            });
        }

        /// <summary>陣列 / List 欄位尾端加一個元素，回傳新元素的 index。</summary>
        public static string AddArrayElement(string nodePath, string componentType, string fieldPath)
        {
            return Guard(() =>
            {
                var comp = CompAt(nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = EditResolve.Prop(so, fieldPath, comp);
                var index = EditResolve.AddArrayElement(prop, fieldPath);
                so.ApplyModifiedPropertiesWithoutUndo();
                Dirty();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath}[{index}] " +
                       $"新增（現有 {prop.arraySize} 筆）";
            });
        }

        /// <summary>刪陣列 / List 第 index 格（elementPath 寫 `field[i]` 或 `field.Array.data[i]`）。</summary>
        public static string RemoveArrayElement(string nodePath, string componentType, string elementPath)
        {
            return Guard(() =>
            {
                EditResolve.SplitElementPath(elementPath, "delel", out var fieldPath, out var index);
                var comp = CompAt(nodePath, componentType);
                var so = new SerializedObject(comp);
                var prop = EditResolve.Prop(so, fieldPath, comp);
                var left = EditResolve.RemoveArrayElement(prop, index, fieldPath);
                so.ApplyModifiedPropertiesWithoutUndo();
                Dirty();
                return $"{nodePath}.{comp.GetType().Name}.{fieldPath}[{index}] 刪除（剩 {left} 筆）";
            });
        }

        /// <summary>加 component 到既有節點（AddNode 只在建節點時掛）。</summary>
        public static string AddComponent(string nodePath, params string[] componentTypes)
        {
            return Guard(() =>
            {
                var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
                var added = new List<string>();
                foreach (var typeName in componentTypes ?? Array.Empty<string>())
                {
                    var type = EditResolve.CompType(typeName);
                    if (node.GetComponent(type) != null) continue;
                    node.gameObject.AddComponent(type);
                    added.Add(type.Name);
                }

                Dirty();
                return $"{nodePath} += <{EditResolve.Join(added)}>" + LayerLintSuffix(node);
            });
        }

        public static string DeleteNode(string nodePath)
        {
            return Guard(() =>
            {
                var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
                var count = EditResolve.CountDescendants(node);
                Object.DestroyImmediate(node.gameObject);
                Dirty();
                return $"刪除 {nodePath}（含 {count} 個子節點）";
            });
        }

        /// <summary>移除節點上的 component。不存在就跳過 —— 語意是「確保它不在」。</summary>
        public static string DeleteComponents(string nodePath, string componentTypes)
        {
            return Guard(() =>
            {
                var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
                var removed = new List<string>();
                foreach (var typeName in (componentTypes ?? "").Split(','))
                {
                    if (string.IsNullOrWhiteSpace(typeName)) continue;
                    var comp = node.GetComponent(EditResolve.CompType(typeName.Trim()));
                    if (comp == null) continue;
                    removed.Add(comp.GetType().Name);
                    Object.DestroyImmediate(comp, true);
                }

                if (removed.Count == 0)
                    return $"（跳過）{EditResolve.Describe(nodePath)} 上沒有那些 component";
                Dirty();
                return $"{nodePath} -= <{EditResolve.Join(removed)}>";
            });
        }

        /// <summary>結構改完重跑 [Auto*] 綁定（理由見 EditResolve.RunAuto）。</summary>
        public static string Auto(string nodePath)
        {
            return Guard(() =>
            {
                var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
                var msg = EditResolve.RunAuto(node);
                Dirty();
                return msg;
            });
        }

        public static string SetPos(string nodePath, float x, float y, float z)
        {
            return Guard(() =>
            {
                var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
                node.localPosition = new Vector3(x, y, z);
                Dirty();
                var uiWarning = node is RectTransform
                    ? "\n# 注意：這是 RectTransform，localPosition 會被 Canvas relayout 覆寫。" +
                      "要改 UI 位置請用 `rect|<node>|<x,y>`（寫 anchoredPosition）"
                    : "";
                return $"{nodePath}.localPosition = {node.localPosition:0.##}" + uiWarning;
            });
        }

        /// <summary>
        /// UI 節點的 anchoredPosition / sizeDelta / anchor / pivot。本體跟 prefab 共用
        /// EditBatch.ApplyRect，這裡只負責 scene 端的 dirty（以及節點在 prefab 實例裡時記 override）。
        /// </summary>
        public static string SetRect(string nodePath, string[] args)
        {
            return Guard(() =>
            {
                var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
                var msg = EditBatch.ApplyRect(node, nodePath, args, "rect", null);
                EditorUtility.SetDirty(node);
                if (PrefabUtility.IsPartOfPrefabInstance(node))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(node);
                Dirty();
                EditBatch.Touch(nodePath);
                return msg;
            });
        }

        /// <summary>
        /// 複製節點（整棵子樹）到同一個 parent、排在原節點後面一格、改名成 newName。
        ///
        /// 用 Object.Instantiate：子樹內部互指的 reference 會自動對到複本，指向子樹外的維持原樣
        /// —— 「照抄一顆按鈕再改字」要的就是這個。但 Instantiate 會扯斷 prefab 連結，所以：
        /// - 原節點本身是 prefab 實例 root → 改用 EditCopy.InstantiateLikeInstance（同一個來源
        ///   asset + 套 PropertyModifications），實例上 added / removed 的東西帶不過去，會印警告。
        ///   做不到就退回 Instantiate 並明講連結斷了。
        /// - 原節點在某個 prefab 實例「裡面」、或子樹裡含 nested 實例 → 照樣 Instantiate，印警告。
        /// 撞名直接擋（不像 add 那樣跳過）：跳過的話後面的 `$` / 路徑會默默改到舊節點。
        /// </summary>
        public static string Duplicate(string nodePath, string newName)
        {
            return Guard(() =>
            {
                var scene = Active();
                var src = EditResolve.NodeInRoots(Roots(scene), nodePath);
                if (string.IsNullOrWhiteSpace(newName))
                    throw new Abort("`dup` 要給新名字：dup|<node>|<newName>");
                if (newName.IndexOf('/') >= 0 || newName.IndexOf('\n') >= 0)
                    throw new Abort($"`dup` 的 newName 不能含 `/` 或換行：'{newName}'");

                var parent = src.parent;
                var siblingClash = parent != null
                    ? FindDirectChild(parent, newName) != null
                    : Roots(scene).Any(g => g != null && g.name == newName);
                if (siblingClash)
                    throw new Abort(
                        $"'{(parent != null ? PathOf(parent) : "(scene root)")}' 底下已經有叫 '{newName}' 的節點。" +
                        "換個名字，或先 `del|<那個節點>` 再重跑（dup 不會跳過，免得後面的操作改到舊節點）");

                var warnings = new List<string>();
                GameObject clone = null;
                var srcGo = src.gameObject;
                var isInstanceRoot = PrefabUtility.IsAnyPrefabInstanceRoot(srcGo);
                if (isInstanceRoot)
                {
                    clone = EditCopy.InstantiateLikeInstance(src, warnings, "dup");
                    if (clone != null)
                    {
                        if (parent != null) clone.transform.SetParent(parent, false);
                        else SceneManager.MoveGameObjectToScene(clone, srcGo.scene);
                    }
                    else
                    {
                        warnings.Add($"'{srcGo.name}' 是 prefab 實例，但重建失敗，退回 Object.Instantiate —— " +
                                     "複本是普通節點，跟 prefab 的連結斷了（之後改 prefab 不會同步到這顆）");
                    }
                }
                else
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(srcGo))
                    {
                        var owner = PrefabUtility.GetOutermostPrefabInstanceRoot(srcGo);
                        warnings.Add($"原節點在 prefab 實例 '{(owner != null ? owner.name : "?")}' 裡面，" +
                                     "複本會變成掛在實例上的 added GameObject（普通節點，不屬於 prefab）");
                    }

                    var nested = CountNestedInstanceRoots(src);
                    if (nested > 0)
                        warnings.Add($"子樹裡有 {nested} 個 nested prefab 實例，Object.Instantiate 後在複本裡" +
                                     "都變成普通節點（prefab 連結斷了）");
                }

                if (clone == null)
                {
                    clone = parent != null
                        ? Object.Instantiate(srcGo, parent, false)
                        : Object.Instantiate(srcGo);
                    if (parent == null) SceneManager.MoveGameObjectToScene(clone, srcGo.scene);
                }

                clone.name = newName;
                clone.SetActive(srcGo.activeSelf);
                clone.transform.SetSiblingIndex(src.GetSiblingIndex() + 1);
                Undo.RegisterCreatedObjectUndo(clone, $"uprefab dup {newName}");
                Dirty();

                var full = parent != null ? $"{ParentPathOf(nodePath)}/{EditResolve.EscapeName(newName)}" : EditResolve.EscapeName(newName);
                EditBatch.Touch(full);
                var sb = new StringBuilder(
                    $"複製 {nodePath} -> {full}（sibling index {clone.transform.GetSiblingIndex()}" +
                    $"，含 {EditResolve.CountDescendants(clone.transform)} 個子節點" +
                    (isInstanceRoot && PrefabUtility.IsAnyPrefabInstanceRoot(clone) ? "，保留 prefab 連結" : "") + "）");
                foreach (var w in warnings) sb.Append("\n# dup: " + w);
                sb.Append(LayerLintSuffix(clone.transform));
                return sb.ToString();
            });
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == name) return c;
            }
            return null;
        }

        private static int CountNestedInstanceRoots(Transform t)
        {
            var n = 0;
            for (var i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                if (PrefabUtility.IsAnyPrefabInstanceRoot(c.gameObject)) n++;
                else n += CountNestedInstanceRoots(c);
            }
            return n;
        }

        /// <summary>
        /// 使用者給的 nodePath 去掉最後一段（尊重 `\/` 逃逸）。拿使用者原本的字串而不是從
        /// Transform 重組，是因為那條路徑已經證明解得開 —— 自動命名的節點重組出來不一定對得上。
        /// </summary>
        private static string ParentPathOf(string nodePath)
        {
            for (var i = nodePath.Length - 1; i >= 0; i--)
            {
                if (nodePath[i] != '/') continue;
                var bs = 0;
                for (var j = i - 1; j >= 0 && nodePath[j] == '\\'; j--) bs++;
                if (bs % 2 == 0) return nodePath.Substring(0, i);
            }
            return "";
        }

        public static string SetActive(string nodePath, bool active)
        {
            return Guard(() =>
            {
                var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
                node.gameObject.SetActive(active);
                Dirty();
                return $"{nodePath}.activeSelf = {active}";
            });
        }

        /// <summary>
        /// 設 GameObject layer（吃 layer 名字；children = true 連整棵子樹）。
        /// 設完若違反「detection source 一律 Detector layer」慣例，回傳值附警告與修正指令。
        /// </summary>
        public static string SetLayer(string nodePath, string layerName, bool children = false)
        {
            return Guard(() =>
            {
                var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
                var layer = EditLayer.Resolve(layerName, "layer");
                var before = EditLayer.Name(node.gameObject.layer);
                var targets = EditLayer.Apply(node, layer, children);
                Dirty();
                var scope = targets.Count > 1 ? $"（連子樹共 {targets.Count} 個節點）" : "";
                return $"{nodePath}.layer: {before} -> {EditLayer.Name(layer)}{scope}" + LayerLintSuffix(node);
            });
        }

        /// <summary>scene 沒有 do 存檔後驗證那一段，改成改到節點的當下就檢查子樹的 Detector layer 慣例。</summary>
        private static string LayerLintSuffix(Transform node)
        {
            var list = EditLayer.FindViolations(node);
            return list.Count == 0 ? "" : "\n" + EditLayer.FormatWarning(list, list.Count, null, 6, "").TrimEnd('\n');
        }

        public static string Move(string nodePath, string newParentPath)
        {
            return Guard(() =>
            {
                var scene = Active();
                var node = EditResolve.NodeInRoots(Roots(scene), nodePath);
                if (string.IsNullOrEmpty(newParentPath))
                {
                    node.SetParent(null, false);
                    Dirty();
                    return $"{nodePath} -> (root)";
                }

                var parent = EditResolve.NodeInRoots(Roots(scene), newParentPath);
                if (parent.IsChildOf(node))
                    throw new Abort($"'{newParentPath}' 在 '{nodePath}' 底下，會造成迴圈");
                node.SetParent(parent, false);
                Dirty();
                return $"{nodePath} -> {newParentPath}/{node.name}";
            });
        }

        /// <summary>
        /// 調整 sibling 順序。MonoFSM 裡 child 順序是語意的一部分（value source / condition
        /// 依順序取第一個成立的），所以「排第幾」＝優先序。負數 = 從尾端算（-1 = 最後）。
        /// </summary>
        public static string SetSiblingIndex(string nodePath, int siblingIndex)
        {
            return Guard(() =>
            {
                var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
                var parent = node.parent;
                var count = parent != null ? parent.childCount : Active().rootCount;
                var target = siblingIndex < 0 ? count + siblingIndex : siblingIndex;
                if (target < 0 || target >= count)
                    throw new Abort(
                        $"siblingIndex {siblingIndex} 超出範圍：這層有 {count} 個節點" +
                        $"（可用 0..{count - 1}，或 -1..-{count}）");
                var before = node.GetSiblingIndex();
                node.SetSiblingIndex(target);
                Dirty();
                return $"{nodePath} sibling index: {before} -> {node.GetSiblingIndex()}";
            });
        }

        // ---- 批次 ----

        /// <summary>
        /// 一次跑多行操作（語法見 EditBatch）。scene 一直開著，所以整批只付一次呼叫成本，
        /// 中間也不需要重複 load/save。
        /// </summary>
        public static string Batch(string ops) => EditBatch.Run(ops, Dispatch, out _, SceneSpace.Instance);

        /// <summary>scene 版的路徑 ⇄ 節點換算（多 root、第一段是 root 名），給 mark / `$` 存節點用。</summary>
        private sealed class SceneSpace : EditBatch.INodeSpace
        {
            internal static readonly SceneSpace Instance = new();

            public Transform Resolve(string path, out string suggestion) =>
                EditResolve.TryNodeInRootsExact(Roots(Active()), path, out suggestion);

            public string PathOf(Transform node) => EditResolve.PathInRoots(Roots(Active()), node);
        }

        private static string Dispatch(string verb, string[] a)
        {
            switch (verb)
            {
                case "add":
                    return AddNode(EditBatch.At(a, 0), EditBatch.Need(a, 1, verb, "name"),
                        EditBatch.Types(a, 2));
                case "prefab":
                    return AddPrefab(EditBatch.Need(a, 0, verb, "prefabPath"),
                        EditBatch.At(a, 1), EditBatch.At(a, 2));
                case "comp":
                    return AddComponent(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Types(a, 1));
                case "set":
                    return SetField(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Need(a, 1, verb, "componentType"),
                        EditBatch.Need(a, 2, verb, "fieldPath"),
                        EditBatch.At(a, 3) ?? "");
                case "ref":
                    return SetRef(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Need(a, 1, verb, "componentType"),
                        EditBatch.Need(a, 2, verb, "fieldPath"),
                        EditBatch.Need(a, 3, verb, "targetNodePath"),
                        EditBatch.At(a, 4));
                case "aref":
                    return SetAssetRef(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Need(a, 1, verb, "componentType"),
                        EditBatch.Need(a, 2, verb, "fieldPath"),
                        EditBatch.Need(a, 3, verb, "assetPath"));
                case "addel":
                    return AddArrayElement(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Need(a, 1, verb, "componentType"),
                        EditBatch.Need(a, 2, verb, "fieldPath"));
                case "delel":
                    return RemoveArrayElement(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Need(a, 1, verb, "componentType"),
                        EditBatch.Need(a, 2, verb, "<field>[i]"));
                case "pos":
                {
                    var xyz = EditBatch.Need(a, 1, verb, "x,y,z").Split(',');
                    if (xyz.Length != 3)
                        throw new Abort($"`pos` 的座標要是 x,y,z，收到 '{EditBatch.At(a, 1)}'");
                    return SetPos(EditBatch.Need(a, 0, verb, "nodePath"),
                        float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
                }
                case "rect":
                    return SetRect(EditBatch.Need(a, 0, verb, "nodePath"), a);
                case "dup":
                    return Duplicate(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Need(a, 1, verb, "newName"));
                case "active":
                    return SetActive(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Bool(a, 1, verb));
                case "layer":
                    return SetLayer(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Need(a, 1, verb, "layerName"),
                        EditLayer.IsChildrenFlag(EditBatch.At(a, 2)));
                case "mv":
                    return Move(EditBatch.Need(a, 0, verb, "nodePath"), EditBatch.At(a, 1));
                case "idx":
                    return SetSiblingIndex(
                        EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Int(a, 1, verb, "siblingIndex"));
                case "auto":
                    return Auto(EditBatch.Need(a, 0, verb, "nodePath"));
                case "del":
                    return DeleteNode(EditBatch.Need(a, 0, verb, "nodePath"));
                case "delcomp":
                    return DeleteComponents(
                        EditBatch.Need(a, 0, verb, "nodePath"), EditBatch.At(a, 1));
                case "save":
                    return Save();
                default:
                {
                    var ctx = new EditFsm.Ctx
                    {
                        Node = p => EditResolve.NodeInRoots(Roots(Active()), p),
                        Dirty = Dirty,
                    };
                    if (EditFsm.TryDispatch(ctx, verb, a, out var fsm)) return fsm;
                    throw new Abort(
                        "不認得的操作 '" + verb +
                        "'。可用的：add prefab comp set ref aref addel delel pos rect active layer mv idx dup auto del delcomp save mark " +
                        EditFsm.Verbs);
                }
            }
        }

        // ---- 讀 ----

        /// <summary>
        /// 匯出 scene 子樹的文字版（跟 PrefabTextReader.Export 同一個 renderer）。
        /// nodePath 留空 = 只列 root object 一層，附 (+N nodes) 展開成本 —— 大 scene 直接
        /// 整棵匯出會爆 context，所以預設就是「先看目錄」。
        /// </summary>
        public static string Export(
            string nodePath = null, int depth = -1, bool fullExpand = true,
            int charBudget = PrefabTextReader.DefaultCharBudget, bool structureOnly = false)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = Roots(scene);

            if (string.IsNullOrEmpty(nodePath))
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# scene: {scene.path}  ({roots.Count} roots)");
                sb.AppendLine("# 這層是目錄。要看子樹細節：SceneEdit.Export(\"<root 名>/<子路徑>\")");
                foreach (var go in roots.Where(g => g != null))
                {
                    var comps = structureOnly
                        ? ""
                        : "  <" + string.Join(" ", go.GetComponents<Component>()
                            .Where(c => c != null).Select(c => c.GetType().Name)) + ">";
                    // 跟子節點同一套 `(prefab:res:…)` 後綴（HierarchyTextExporter），root 層才看得出是不是 prefab instance
                    var prefabPart = "";
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
                    {
                        var src = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                        if (!string.IsNullOrEmpty(src))
                            prefabPart = $" (prefab:res:{CompactValueFormatter.StripAssetsPrefix(src)})";
                    }
                    sb.AppendLine(
                        $"  {(go.activeSelf ? "" : "~")}{go.name}  " +
                        $"(+{EditResolve.CountDescendants(go.transform)} nodes){comps}{prefabPart}");
                }
                return PrefabTextReader.HardCap(sb.ToString(), charBudget);
            }

            Transform node;
            try
            {
                node = EditResolve.NodeInRoots(roots, nodePath);
            }
            catch (Abort abort)
            {
                return PrefabTextReader.HardCap($"# {abort.Message}", charBudget);
            }

            var head = new StringBuilder();
            head.AppendLine($"# scene: {scene.path}");
            head.AppendLine($"# subtree: {nodePath}");
            return PrefabTextReader.ExportResolvedNode(
                node.gameObject, depth, fullExpand, charBudget, head,
                includeFsm: false, fsmOnly: false, structureOnly: structureOnly);
        }

        /// <summary>
        /// 數場景上的物件 —— Play Mode 下也能用，這是驗證「生成數量對不對」的省 token 手段：
        /// 回傳的是數字與少量樣本，不是整棵 hierarchy。
        /// </summary>
        /// <param name="componentType">型別名（含子類）；留空 = 數 GameObject</param>
        /// <param name="nameContains">名稱篩選：含 * / ? 當 glob 整段比對，否則當 substring
        /// （都忽略大小寫）；留空 = 不限</param>
        /// <param name="sample">附幾筆樣本路徑（預設 0 = 不附）</param>
        public static string Count(string componentType = null, string nameContains = null, int sample = 0)
        {
            return Guard(() =>
            {
                List<GameObject> hits;

                if (string.IsNullOrEmpty(componentType))
                {
                    // 刻意**不**只掃 active scene 的 root：物件池會把借出的物件掛在
                    // PoolManager 底下（可能在另一個 scene 或 DontDestroyOnLoad），
                    // 只掃 active scene 會數到 0 而誤以為根本沒生成。
                    hits = Object.FindObjectsByType<Transform>(
                            FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(t => t != null)
                        .Select(t => t.gameObject)
                        .ToList();
                }
                else
                {
                    var type = EditResolve.CompType(componentType);
                    hits = Object.FindObjectsByType(
                            type, FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .OfType<Component>()
                        .Where(c => c != null)
                        .Select(c => c.gameObject)
                        .Distinct()
                        .ToList();
                }

                if (!string.IsNullOrEmpty(nameContains))
                {
                    var nameOk = EditResolve.NameMatcher(nameContains);
                    hits = hits.Where(g => nameOk(g.name)).ToList();
                }

                var active = hits.Count(g => g.activeInHierarchy);
                // 借出中 / 回池中的比例是「生成是否正常」的關鍵訊號，所以 active 分開報
                var byScene = hits.GroupBy(g => g.scene.IsValid() ? g.scene.name : "(no scene)")
                    .OrderByDescending(g => g.Count())
                    .Select(g => $"{g.Key}={g.Count()}");
                var line = $"count={hits.Count} activeInHierarchy={active}" +
                           $"  [{(Application.isPlaying ? "PlayMode" : "EditMode")}]" +
                           $"  filter: comp={componentType ?? "*"} name={nameContains ?? "*"}" +
                           (hits.Count > 0 ? $"\n  scenes: {string.Join(" ", byScene)}" : "");

                if (sample <= 0) return line;
                var sb = new StringBuilder(line);
                foreach (var go in hits.Take(sample))
                    sb.Append($"\n  {(go.activeInHierarchy ? "" : "~")}{PathOf(go.transform)}");
                if (hits.Count > sample) sb.Append($"\n  … 還有 {hits.Count - sample} 筆");
                return sb.ToString();
            });
        }

        // ---- 內部 ----

        /// <summary>
        /// 用 Single 模式切 scene 之前呼叫。Single 模式會直接丟掉沒存的改動、Editor 不會問。
        ///
        /// 預設（saveDirty=false）：有任何 dirty 的已開 scene 就 Abort —— 不切、不存，列出路徑。
        /// 2026-09-29 版本是「自動存掉再切」，結果連兩天存了使用者故意不存的 scene
        /// （`TestKCC Train Move`、`_Recovery/0_下山逃脫_July_lake 1.unity`）。dirty scene 是
        /// 使用者的東西，存或不存只有使用者能決定，所以預設停下來交給人。
        /// saveDirty=true（CLI `--save-dirty`）：使用者明確同意才照舊存完再切，回傳一行「存了哪些」。
        ///
        /// 刻意不提供 discard：丟掉就救不回來，CLI 不該有這個按鈕。
        /// 有 dirty 的 Untitled（沒路徑）scene 不管 saveDirty 都整個 Abort：
        /// 靜默存到某個自動路徑使用者找不到，丟掉又是資料遺失。
        /// 故意不用 SaveCurrentModifiedScenesIfUserWantsTo —— 它會跳對話框卡住 CLI。
        /// </summary>
        private static string CheckDirtyOpenScenes(bool saveDirty)
        {
            var dirty = new List<Scene>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.IsValid() || !s.isLoaded || !s.isDirty) continue;
                if (string.IsNullOrEmpty(s.path))
                    throw new Abort(
                        $"有未存檔的 Untitled scene（{(string.IsNullOrEmpty(s.name) ? "Untitled" : s.name)}），" +
                        "它沒有路徑、不能自動存；切 scene 會把它丟掉（--save-dirty 也救不了）。" +
                        "請使用者在 Editor 裡先存（File > Save As）或手動關掉再重跑");
                dirty.Add(s);
            }
            if (dirty.Count == 0) return "";

            if (!saveDirty)
            {
                var sb = new StringBuilder();
                sb.Append($"Editor 裡有 {dirty.Count} 個未存檔的 scene，沒有切 scene、也沒有替使用者存：");
                foreach (var s in dirty) sb.Append($"\n  - {s.path}");
                sb.Append("\n  這是使用者的改動，存不存要使用者決定（可能是故意不存的）：" +
                          "請使用者在 Editor 自己存（Cmd+S）或放棄改動後再重跑；" +
                          "使用者明確說要存，才重跑同一條指令加 --save-dirty");
                throw new Abort(sb.ToString());
            }

            var names = new List<string>(dirty.Count);
            foreach (var s in dirty)
            {
                if (!EditorSceneManager.SaveScene(s))
                    throw new Abort($"切 scene 前存檔失敗：{s.path}（沒有切 scene）");
                names.Add(s.path);
            }
            return $"切換前已存檔 {names.Count} 個 dirty scene（--save-dirty）：{string.Join("、", names)}\n";
        }

        private static Scene Active()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) throw new Abort("沒有有效的 active scene");
            return scene;
        }

        private static List<GameObject> Roots(Scene scene) =>
            scene.IsValid() ? scene.GetRootGameObjects().ToList() : new List<GameObject>();

        private static Component CompAt(string nodePath, string componentType)
        {
            var node = EditResolve.NodeInRoots(Roots(Active()), nodePath);
            return EditResolve.Comp(node, nodePath, componentType);
        }

        // 每個原語各自標 dirty，這樣呼叫端可以連下十幾個原語再 Save() 一次
        private static void Dirty() => EditorSceneManager.MarkSceneDirty(Active());

        // SaveScene 不會自己建中間資料夾，路徑不存在時它只是靜默失敗
        private static void EnsureDirectory(string assetPath)
        {
            var dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir)) return;

            var parts = dir.Split('/');
            var cursor = parts[0]; // "Assets"
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{cursor}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    // 磁碟上已經有、只是 Unity 還沒 import（例：agent 先 `mkdir -p` 再叫 up）→ 先 import。
                    // 直接 CreateFolder 的話 Unity 會看到磁碟撞名，自動改建「<名字> 1」，
                    // 而後面存檔還是寫進原本那個資料夾，留下一個空的「 1」資料夾（2026-09-29 廢鐵青蛙）
                    if (System.IO.Directory.Exists(next))
                    {
                        AssetDatabase.ImportAsset(next, ImportAssetOptions.ForceSynchronousImport);
                        if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.Refresh();
                        if (!AssetDatabase.IsValidFolder(next))
                            throw new Abort($"資料夾 {next} 在磁碟上存在但 Unity 認不得（import 失敗），請在 Editor 按 Cmd+R 後重跑");
                    }
                    else
                    {
                        var guid = AssetDatabase.CreateFolder(cursor, parts[i]);
                        var created = AssetDatabase.GUIDToAssetPath(guid);
                        if (created != next)
                            throw new Abort($"建資料夾 {next} 失敗，Unity 實際建成「{created}」—— 請刪掉那個多出來的資料夾後重跑");
                    }
                }
                cursor = next;
            }
        }

        private static IEnumerable<GameObject> AllInSubtree(GameObject go)
        {
            yield return go;
            foreach (Transform child in go.transform)
            foreach (var g in AllInSubtree(child.gameObject))
                yield return g;
        }

        private static string PathOf(Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = $"{p.name}/{path}";
            return path;
        }

        private static string Guard(Func<string> body)
        {
            try
            {
                return body();
            }
            catch (Abort abort)
            {
                return $"# 未修改：{abort.Message}";
            }
        }
    }
}
