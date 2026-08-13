using System.Collections;
using System.Reflection;
using System.Text;
using MonoFSM.Foundation;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor
{
    public static class DumpFieldValuesContextMenu
    {
        [MenuItem("CONTEXT/Component/Revert GameObject Name to Prefab")]
        static void RevertGameObjectNameToPrefab(MenuCommand command)
        {
            if (command.context is not Component component) return;

            var go = component.gameObject;
            if (!PrefabUtility.IsPartOfPrefabInstance(go))
            {
                Debug.LogWarning("This GameObject is not a prefab instance.", go);
                return;
            }

            var serializedObject = new SerializedObject(go);
            var nameProp = serializedObject.FindProperty("m_Name");
            PrefabUtility.RevertPropertyOverride(nameProp, InteractionMode.UserAction);
        }

        [MenuItem("CONTEXT/Component/Revert GameObject Name to Prefab", true)]
        static bool RevertGameObjectNameToPrefabValidate(MenuCommand command)
        {
            if (command.context is not Component component) return false;
            return PrefabUtility.IsPartOfPrefabInstance(component.gameObject);
        }

        //把改過的 GameObject 名字 apply 到 nested prefab 鏈最內層的那個 prefab asset
        [MenuItem("CONTEXT/Component/Apply GameObject Name to Prefab (Innermost)")]
        static void ApplyGameObjectNameToPrefab(MenuCommand command)
        {
            if (command.context is not Component component) return;

            var go = component.gameObject;
            if (!PrefabUtility.IsPartOfPrefabInstance(go))
            {
                Debug.LogWarning("[ApplyName] 這個 GameObject 不是 prefab instance", go);
                return;
            }

            var innermostSource = GetInnermostPrefabSource(go);
            if (innermostSource == null)
            {
                Debug.LogWarning("[ApplyName] 找不到對應的 prefab source", go);
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(innermostSource);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning($"[ApplyName] source {innermostSource.name} 沒有 asset path", go);
                return;
            }

            var serializedObject = new SerializedObject(go);
            var nameProp = serializedObject.FindProperty("m_Name");
            try
            {
                PrefabUtility.ApplyPropertyOverride(nameProp, assetPath, InteractionMode.UserAction);
                Debug.Log($"[ApplyName] \"{go.name}\" → {assetPath}", go);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ApplyName] apply 到 {assetPath} 失敗: {e.Message}", go);
            }
        }

        [MenuItem("CONTEXT/Component/Apply GameObject Name to Prefab (Innermost)", true)]
        static bool ApplyGameObjectNameToPrefabValidate(MenuCommand command)
        {
            if (command.context is not Component component) return false;
            return PrefabUtility.IsPartOfPrefabInstance(component.gameObject);
        }

        //一路往下追 nested prefab 的來源，直到最裡面那層
        static GameObject GetInnermostPrefabSource(GameObject go)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
            while (source != null)
            {
                var deeper = PrefabUtility.GetCorrespondingObjectFromSource(source);
                if (deeper == null || deeper == source) break;
                source = deeper;
            }

            return source;
        }

        [MenuItem("CONTEXT/MonoBehaviour/Dump Field Values")]
        static void DumpFieldValues(MenuCommand command)
        {
            if (command.context is not AbstractDescriptionBehaviour target) return;

            var sb = new StringBuilder();
            var type = target.GetType();
            sb.AppendLine($"=== {type.Name} on [{target.gameObject.name}] ===");

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var currentType = type;

            while (currentType != null && currentType != typeof(MonoBehaviour))
            {
                var fields = currentType.GetFields(flags | BindingFlags.DeclaredOnly);
                if (fields.Length == 0)
                {
                    currentType = currentType.BaseType;
                    continue;
                }

                if (currentType != type)
                    sb.AppendLine($"--- {currentType.Name} ---");

                foreach (var field in fields)
                {
                    var isPublic = field.IsPublic;
                    var hasSerialized = field.GetCustomAttribute<SerializeField>() != null;
                    if (!isPublic && !hasSerialized) continue;

                    var value = field.GetValue(target);
                    var formatted = FormatFieldValue(value);
                    sb.AppendLine($"  {field.FieldType.Name} {field.Name} = {formatted}");
                }

                currentType = currentType.BaseType;
            }

            Debug.Log(sb.ToString(), target);
        }

        static string FormatFieldValue(object value)
        {
            if (value == null) return "null";

            if (value is Object uObj)
            {
                if (uObj == null) return "null (destroyed)";
                return $"\"{uObj.name}\" ({uObj.GetType().Name})";
            }

            if (value is IList list)
            {
                var sb = new StringBuilder();
                sb.Append($"[{list.Count}] {{ ");
                var max = Mathf.Min(list.Count, 10);
                for (int i = 0; i < max; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(FormatFieldValue(list[i]));
                }

                if (list.Count > max) sb.Append(", ...");
                sb.Append(" }");
                return sb.ToString();
            }

            return UnityTypeFormatter.FormatValue(value);
        }
    }
}
