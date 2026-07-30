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
        /// </summary>
        public static string AddArrayElement(string assetPath, string fieldPath)
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
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                return $"{assetPath}.{fieldPath}[{index}]  新增（現有 {prop.arraySize} 筆）";
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
