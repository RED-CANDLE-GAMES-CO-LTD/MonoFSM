#if UNITY_EDITOR
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Abort = MonoFSM.Editor.PrefabEditing.EditResolve.EditAbort;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// 建立與編輯 ScriptableObject asset 的五個原語，供 uloop execute-dynamic-code /
    /// uprefab CLI（`up asset …`）呼叫。
    ///
    /// 這是 PrefabEdit / SceneEdit 的第三面：PrefabEdit 改「掛在節點上的 component」、
    /// SceneEdit 改「scene 裡的 GameObject」，AssetEdit 改「獨立存在、不掛在任何節點上的
    /// ScriptableObject asset」（registry / config 類資料，例如 PromptIconRegistry、
    /// DeviceIconMapConfig）。三者共用同一套路徑/型別/欄位解析（EditResolve）與同一套
    /// 錯誤訊息慣例：打錯就列出「這裡實際有什麼」。
    ///
    /// 為什麼不做成 MenuItem：MenuItem 沒有參數，而「建一個某型別的 asset、把某欄位設成
    /// 某值、指向另一個 asset」天生是參數化操作，MenuItem 每次都要人在 Project 視窗點選、
    /// 手動填 Inspector，agent 做不到也不可重現。做成 static API 才能被 dynamic code 組合，
    /// 也才能寫進 `up asset` 這種可重跑的 CLI 子命令。
    ///
    /// 全程用 SerializedObject / SerializedProperty 改值（不直接改 C# field），
    /// 失敗一律不 throw、回傳 `# 未修改：原因` 這種可診斷字串（比照 PrefabEdit.Edit 的慣例）。
    /// </summary>
    public static class AssetEdit
    {
        /// <summary>
        /// 建一個 ScriptableObject asset。typeName 支援短名或 FullName，解析走
        /// EditResolve.ScriptableObjectType（保證解析出的型別確實繼承 ScriptableObject）。
        /// assetPath 已存在時預設不覆蓋，overwrite=true 才覆蓋（會先刪舊的再建新的）。
        /// </summary>
        public static string CreateAsset(string typeName, string assetPath, bool overwrite = false)
        {
            return Guard(() =>
            {
                ValidateAssetPath(assetPath);
                var type = EditResolve.ScriptableObjectType(typeName);

                var existing = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (existing != null)
                {
                    if (!overwrite)
                        throw new Abort(
                            $"{assetPath} 已存在（{existing.GetType().Name}）。" +
                            "不覆蓋；overwrite=true 才覆蓋");
                    if (!AssetDatabase.DeleteAsset(assetPath))
                        throw new Abort($"覆蓋失敗，刪不掉既有 asset: {assetPath}");
                }

                EnsureDirectory(assetPath);
                var instance = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(instance, assetPath);
                EditorUtility.SetDirty(instance);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return $"建立 {assetPath}  <{type.FullName}>" +
                       (existing != null ? "（已覆蓋原本的）" : "");
            });
        }

        /// <summary>
        /// 設定 asset 上 serialized 欄位的值（非物件引用）。fieldPath 支援巢狀
        /// （如 `_entries.Array.data[0]._family`）。跟 PrefabEdit.SetField 共用
        /// EditResolve.Prop / EditResolve.ApplyValue，不是另一套實作。
        /// </summary>
        public static string SetField(string assetPath, string fieldPath, string value)
        {
            return Guard(() =>
            {
                var asset = LoadAsset(assetPath);
                var so = new SerializedObject(asset);
                var prop = EditResolve.Prop(so, fieldPath, asset);
                var before = EditResolve.Preview(prop);
                EditResolve.ApplyValue(prop, value, fieldPath);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                return $"{assetPath}.{fieldPath}: {before} -> {EditResolve.Preview(prop)}";
            });
        }

        /// <summary>
        /// 把 asset 的欄位指向另一個 asset（ScriptableObject / prefab / Texture2D / Sprite 皆可，
        /// 解析規則見 AssetRef.Resolve：prefab 會依欄位宣告型別取對應 component）。
        /// </summary>
        public static string SetAssetRef(string assetPath, string fieldPath, string targetAssetPath)
        {
            return Guard(() =>
            {
                var asset = LoadAsset(assetPath);
                var so = new SerializedObject(asset);
                var prop = EditResolve.Prop(so, fieldPath, asset);
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    throw new Abort($"'{fieldPath}' 是 {prop.propertyType}，不是物件引用；請改用 SetField");

                prop.objectReferenceValue = AssetRef.Resolve(targetAssetPath, asset, fieldPath);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                return $"{assetPath}.{fieldPath} -> res:{targetAssetPath}";
            });
        }

        /// <summary>
        /// 在陣列/List 欄位尾端加一個元素，回傳它的 index。呼叫端接著用 SetField/SetAssetRef
        /// 填內容 —— 這是建 registry 型 asset（一個陣列裝很多 entry）的關鍵操作。
        ///
        /// typeName 只給 [SerializeReference] 陣列用（如 GameData._dataFunctions）：那種陣列
        /// 單純 arraySize++ 只會得到一個 null 元素（YAML 上是 `rid: -2`），必須指定要塞哪個
        /// 具體實作型別才有意義。一般陣列傳了會報錯，避免誤用而不自知。
        /// </summary>
        public static string AddArrayElement(string assetPath, string fieldPath,
            string typeName = null)
        {
            return Guard(() =>
            {
                var asset = LoadAsset(assetPath);
                var so = new SerializedObject(asset);
                var prop = EditResolve.Prop(so, fieldPath, asset);
                // 注意：SerializedProperty.isArray 對 string 也回 true（舊版序列化 API 把
                // string 當 char[] 存），不排除的話會把陣列元素插進字串的位元組裡，
                // 存出一份壞掉的 UTF-8。真正的陣列/List 才做。
                if (!prop.isArray || prop.propertyType == SerializedPropertyType.String)
                    throw new Abort(
                        $"'{fieldPath}' 是 {prop.propertyType}，不是陣列/List，不能 AddArrayElement");

                var index = prop.arraySize;
                prop.arraySize++;
                var element = prop.GetArrayElementAtIndex(index);
                var isManagedRef =
                    element.propertyType == SerializedPropertyType.ManagedReference;
                var created = "";

                if (!string.IsNullOrEmpty(typeName))
                {
                    if (!isManagedRef)
                        throw new Abort(
                            $"'{fieldPath}' 不是 [SerializeReference] 陣列（元素是 " +
                            $"{element.propertyType}），不吃 typeName；直接用 set / set-ref 填欄位");

                    var baseType = EditResolve.ManagedRefFieldType(element)
                                   ?? throw new Abort(
                                       $"解析不出 '{fieldPath}' 的 SerializeReference 宣告型別");
                    var type = EditResolve.ManagedRefType(baseType, typeName);
                    element.managedReferenceValue = System.Activator.CreateInstance(type);
                    created = $"  <{type.FullName}>";
                }
                else if (isManagedRef)
                {
                    // 不擋（呼叫端可能真的要一個 null 佔位），但要講清楚它現在是 null
                    created = "  <null；[SerializeReference] 陣列請加 typeName 指定實作型別>";
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                return $"{assetPath}.{fieldPath}[{index}]  新增（現有 {prop.arraySize} 筆）{created}";
            });
        }

        /// <summary>
        /// 呼叫 asset 上一個無參數的 public 方法，然後存檔。
        ///
        /// 存在理由：專案大量用 Odin `[Button]` 掛維護動作（AllFlagCollection.FindAllFlagsInProject、
        /// ScriptableCollection.FindUnderFolder…）。那些 button 只能用滑鼠按，agent 按不到，
        /// 而漏按的後果是靜默的 —— 例如新建的 GameData 沒被收進 AllFlagCollection，
        /// runtime 就不會跑 FlagAwake，它的 DataFunction dict 永遠是空的。
        /// </summary>
        public static string Invoke(string assetPath, string methodName)
        {
            return Guard(() =>
            {
                var asset = LoadAsset(assetPath);
                var type = asset.GetType();
                var method = type.GetMethod(methodName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.FlattenHierarchy);

                if (method == null)
                {
                    var candidates = type.GetMethods(
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.DeclaredOnly)
                        .Where(m => m.GetParameters().Length == 0 && !m.IsSpecialName)
                        .Select(m => m.Name).Distinct().Take(20).ToList();
                    throw new Abort(
                        $"{type.Name} 上沒有方法 '{methodName}'。無參數的有：{EditResolve.Join(candidates)}");
                }

                if (method.GetParameters().Length != 0)
                    throw new Abort($"'{methodName}' 需要參數，這裡只支援無參數方法");

                method.Invoke(asset, null);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return $"{assetPath}.{methodName}() 已執行";
            });
        }

        /// <summary>
        /// 列出 asset 上的 serialized 欄位（名稱 + 型別），用來自我診斷欄位名打錯。
        /// 走 SerializedObject 而不是純反射 —— 讀到的是 Unity 真的會序列化出來的那份
        /// （繞開 Odin 之類 attribute 造成的顯示差異）。
        /// </summary>
        public static string ListFields(string assetPath)
        {
            return Guard(() =>
            {
                var asset = LoadAsset(assetPath);
                var so = new SerializedObject(asset);
                var sb = new StringBuilder($"# {assetPath}  <{asset.GetType().FullName}>\n");

                var it = so.GetIterator();
                if (it.NextVisible(true))
                    do
                    {
                        if (it.name == "m_Script") continue;
                        sb.AppendLine($"  {it.name}: {it.type}");
                    } while (it.NextVisible(false));

                return sb.ToString();
            });
        }

        // ---- 內部 ----

        private static void ValidateAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.StartsWith("Assets/") || !assetPath.EndsWith(".asset"))
                throw new Abort(
                    $"assetPath 要以 \"Assets/\" 開頭、\".asset\" 結尾，收到：'{assetPath}'");
        }

        /// <summary>
        /// 找不到就診斷「這個資料夾底下實際有什麼」—— 比照 EditResolve 路徑打錯時
        /// 列出實際子節點的作法，asset 打錯路徑最常見的原因是資料夾/檔名打錯一個字。
        /// </summary>
        private static Object LoadAsset(string assetPath)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (obj != null) return obj;

            var dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || !AssetDatabase.IsValidFolder(dir))
                throw new Abort($"找不到 asset: {assetPath}（資料夾也不存在: {dir}）");

            var siblings = AssetDatabase.FindAssets("", new[] { dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => string.Equals(
                    System.IO.Path.GetDirectoryName(p)?.Replace('\\', '/'), dir))
                .Select(System.IO.Path.GetFileName)
                .Distinct().Take(30).ToList();

            throw new Abort(
                $"找不到 asset: {assetPath}。{dir} 底下實際有：{EditResolve.Join(siblings)}");
        }

        // AssetDatabase 不會自己建中間資料夾（跟 PrefabEdit.EnsureDirectory / SceneEdit 同款邏輯）
        private static void EnsureDirectory(string assetPath)
        {
            var dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir)) return;

            var parts = dir.Split('/');
            var cursor = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{cursor}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cursor, parts[i]);
                cursor = next;
            }
        }

        private static string Guard(System.Func<string> body)
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
#endif
