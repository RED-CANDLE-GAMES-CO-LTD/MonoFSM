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
        /// 建一個新 scene 並存檔（會取代目前開著的 scene）。
        /// </summary>
        /// <param name="scenePath">例：Assets/Scenes/Test.unity</param>
        /// <param name="withDefaults">true = 帶 Main Camera + Directional Light</param>
        public static string NewScene(string scenePath, bool withDefaults = false)
        {
            return Guard(() =>
            {
                if (!scenePath.EndsWith(".unity"))
                    throw new Abort($"scenePath 要以 .unity 結尾：{scenePath}");
                if (Application.isPlaying)
                    throw new Abort("Play Mode 中不能建 scene");

                var setup = withDefaults
                    ? NewSceneSetup.DefaultGameObjects
                    : NewSceneSetup.EmptyScene;
                var scene = EditorSceneManager.NewScene(setup, NewSceneMode.Single);

                EnsureDirectory(scenePath);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                    throw new Abort($"存檔失敗：{scenePath}");
                return $"建立 scene {scenePath}（{(withDefaults ? "含" : "不含")}預設物件）";
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
        public static string CopyScene(string templatePath, string newScenePath)
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

                EnsureDirectory(newScenePath);
                if (!AssetDatabase.CopyAsset(templatePath, newScenePath))
                    throw new Abort($"複製失敗：{templatePath} -> {newScenePath}");
                AssetDatabase.ImportAsset(newScenePath);

                var scene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);
                return $"複製 scene {newScenePath}\n" +
                       $"  模板: {templatePath}\n" +
                       $"  已開啟，{scene.rootCount} 個 root";
            });
        }

        public static string OpenScene(string scenePath)
        {
            return Guard(() =>
            {
                if (Application.isPlaying)
                    throw new Abort("Play Mode 中不能開 scene");
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                    throw new Abort($"找不到 scene: {scenePath}");
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                return $"開啟 {scene.path}（{scene.rootCount} 個 root）";
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
                return $"建立 {full}  <{string.Join(", ", added)}>";
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

                prop.objectReferenceValue = AssetRef.Resolve(targetAssetPath, comp, fieldPath);
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
                return $"{nodePath} += <{EditResolve.Join(added)}>";
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
                return $"{nodePath}.localPosition = {node.localPosition:0.##}";
            });
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

        // ---- 批次 ----

        /// <summary>
        /// 一次跑多行操作（語法見 EditBatch）。scene 一直開著，所以整批只付一次呼叫成本，
        /// 中間也不需要重複 load/save。
        /// </summary>
        public static string Batch(string ops) => EditBatch.Run(ops, Dispatch);

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
                case "pos":
                {
                    var xyz = EditBatch.Need(a, 1, verb, "x,y,z").Split(',');
                    if (xyz.Length != 3)
                        throw new Abort($"`pos` 的座標要是 x,y,z，收到 '{EditBatch.At(a, 1)}'");
                    return SetPos(EditBatch.Need(a, 0, verb, "nodePath"),
                        float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
                }
                case "active":
                    return SetActive(EditBatch.Need(a, 0, verb, "nodePath"),
                        EditBatch.Bool(a, 1, verb));
                case "mv":
                    return Move(EditBatch.Need(a, 0, verb, "nodePath"), EditBatch.At(a, 1));
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
                    throw new Abort(
                        "不認得的操作 '" + verb +
                        "'。可用的：add prefab comp set ref aref pos active mv auto del delcomp save");
            }
        }

        // ---- 讀 ----

        /// <summary>
        /// 匯出 scene 子樹的文字版（跟 PrefabTextReader.Export 同一個 renderer）。
        /// nodePath 留空 = 只列 root object 一層，附 (+N nodes) 展開成本 —— 大 scene 直接
        /// 整棵匯出會爆 context，所以預設就是「先看目錄」。
        /// </summary>
        public static string Export(string nodePath = null, int depth = -1, bool fullExpand = true)
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
                    var comps = string.Join(" ", go.GetComponents<Component>()
                        .Where(c => c != null).Select(c => c.GetType().Name));
                    sb.AppendLine(
                        $"  {(go.activeSelf ? "" : "~")}{go.name}  " +
                        $"(+{EditResolve.CountDescendants(go.transform)} nodes)  <{comps}>");
                }
                return sb.ToString();
            }

            Transform node;
            try
            {
                node = EditResolve.NodeInRoots(roots, nodePath);
            }
            catch (Abort abort)
            {
                return $"# {abort.Message}";
            }

            var options = fullExpand
                ? HierarchyExportOptions.FullExpand
                : HierarchyExportOptions.Default;
            options._maxDepth = depth;
            if (!fullExpand)
                options._excludeComponents.AddRange(PrefabTextReader.VisualComponents);

            var head = $"# scene: {scene.path}\n# subtree: {nodePath}\n\n";
            return head + HierarchyTextExporter.Export(node.gameObject, options);
        }

        /// <summary>
        /// 數場景上的物件 —— Play Mode 下也能用，這是驗證「生成數量對不對」的省 token 手段：
        /// 回傳的是數字與少量樣本，不是整棵 hierarchy。
        /// </summary>
        /// <param name="componentType">型別名（含子類）；留空 = 數 GameObject</param>
        /// <param name="nameContains">名稱包含這段才算（模糊比對）；留空 = 不限</param>
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
                    hits = hits.Where(g =>
                        g.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

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
                    AssetDatabase.CreateFolder(cursor, parts[i]);
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
