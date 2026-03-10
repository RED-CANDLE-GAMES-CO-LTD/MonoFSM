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
