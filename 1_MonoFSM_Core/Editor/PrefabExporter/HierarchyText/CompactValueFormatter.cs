using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Editor
{
    // 匯出時的 traversal context：目前的匯出 root 與正在輸出的 node，用來判斷 tree-ref 相對路徑
    public class HierarchyExportContext
    {
        public Transform Root;
        public Transform Current;
        public HierarchyExportOptions Options;
        public Type CurrentComponentType; // 正在輸出的 component 型別，供巢狀欄位過濾預設值用
    }

    // 把 SerializedProperty 的值格式化成精簡文字（見 skill 文件的完整 spec）
    public static class CompactValueFormatter
    {
        public static string FormatValue(SerializedProperty property, HierarchyExportContext ctx, int depth = 0)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return property.boolValue ? "" : "off";
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();
                case SerializedPropertyType.Float:
                    return FormatFloat(property.floatValue);
                case SerializedPropertyType.String:
                    return FormatString(property.stringValue, ctx.Options._maxStringLength);
                case SerializedPropertyType.Enum:
                    return FormatEnum(property);
                case SerializedPropertyType.Color:
                    return FormatColor(property.colorValue);
                case SerializedPropertyType.Vector2:
                    return FormatVec(property.vector2Value.x, property.vector2Value.y);
                case SerializedPropertyType.Vector3:
                    return FormatVec(property.vector3Value.x, property.vector3Value.y, property.vector3Value.z);
                case SerializedPropertyType.Vector4:
                    return FormatVec(property.vector4Value.x, property.vector4Value.y, property.vector4Value.z, property.vector4Value.w);
                case SerializedPropertyType.Vector2Int:
                    return FormatVec(property.vector2IntValue.x, property.vector2IntValue.y);
                case SerializedPropertyType.Vector3Int:
                    return FormatVec(property.vector3IntValue.x, property.vector3IntValue.y, property.vector3IntValue.z);
                case SerializedPropertyType.LayerMask:
                    return FormatLayerMask(property.intValue);
                case SerializedPropertyType.AnimationCurve:
                    return $"curve({property.animationCurveValue?.keys.Length ?? 0} keys)";
                case SerializedPropertyType.ObjectReference:
                    return FormatObjectReference(property, ctx);
                case SerializedPropertyType.Quaternion:
                    var eu = property.quaternionValue.eulerAngles;
                    return FormatVec(eu.x, eu.y, eu.z);
                case SerializedPropertyType.Bounds:
                    var b = property.boundsValue;
                    return $"bounds({FormatVec(b.center.x, b.center.y, b.center.z)},{FormatVec(b.size.x, b.size.y, b.size.z)})";
                case SerializedPropertyType.BoundsInt:
                    var bi = property.boundsIntValue;
                    return $"bounds({FormatVec(bi.position.x, bi.position.y, bi.position.z)},{FormatVec(bi.size.x, bi.size.y, bi.size.z)})";
                case SerializedPropertyType.Rect:
                    var r = property.rectValue;
                    return $"rect({FormatFloat(r.x)},{FormatFloat(r.y)},{FormatFloat(r.width)},{FormatFloat(r.height)})";
                case SerializedPropertyType.RectInt:
                    var ri = property.rectIntValue;
                    return $"rect({ri.x},{ri.y},{ri.width},{ri.height})";
                case SerializedPropertyType.Character:
                    return $"'{(char)property.intValue}'";
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.FixedBufferSize:
                    return property.intValue.ToString();
                case SerializedPropertyType.Hash128:
                    return property.hash128Value.ToString();
                case SerializedPropertyType.Gradient:
                    return "gradient";
                case SerializedPropertyType.Generic:
                    if (property.isArray)
                        return FormatArray(property, ctx, depth);
                    if (property.isFixedBuffer)
                        return "…";
                    return FormatNested(property, ctx, depth);
                default:
                    // 無法解讀的型別一律輸出 "…"，絕不 fallback 到 ToString()
                    // （SerializedProperty.ToString() 會輸出 "UnityEditor.SerializedProperty" 洩漏進結果）
                    return "…";
            }
        }

        private static string FormatFloat(float f)
        {
            if (float.IsNaN(f)) return "NaN";
            if (float.IsInfinity(f)) return f > 0 ? "inf" : "-inf";
            if (Mathf.Approximately(f, Mathf.Round(f))) return Mathf.RoundToInt(f).ToString();
            return f.ToString("0.###");
        }

        private static string FormatVec(params float[] comps) => "(" + string.Join(",", comps.Select(FormatFloat)) + ")";
        private static string FormatVec(params int[] comps) => "(" + string.Join(",", comps) + ")";

        private static string FormatString(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            if (s.Length > maxLen) return $"\"{s.Substring(0, maxLen)}…\"";
            return $"\"{s}\"";
        }

        private static string FormatEnum(SerializedProperty property)
        {
            var names = property.enumNames;
            var idx = property.enumValueIndex;
            if (names == null || idx < 0 || idx >= names.Length) return property.intValue.ToString();
            return names[idx];
        }

        private static string FormatColor(Color c)
        {
            Color32 c32 = c;
            return c.a >= 1f
                ? $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}"
                : $"#{c32.r:X2}{c32.g:X2}{c32.b:X2}{c32.a:X2}";
        }

        private static string FormatLayerMask(int mask)
        {
            var names = new System.Collections.Generic.List<string>();
            for (var i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                var n = LayerMask.LayerToName(i);
                names.Add(string.IsNullOrEmpty(n) ? i.ToString() : n);
            }
            return names.Count == 0 ? "none" : string.Join(",", names);
        }

        private static string FormatArray(SerializedProperty property, HierarchyExportContext ctx, int depth)
        {
            var size = property.arraySize;
            if (size == 0) return "[]";

            var max = ctx.Options._maxArrayElements;
            var count = Math.Min(size, max);
            var parts = new string[count];
            var allSame = true;
            for (var i = 0; i < count; i++)
            {
                parts[i] = FormatValue(property.GetArrayElementAtIndex(i), ctx, depth + 1);
                if (i > 0 && parts[i] != parts[0]) allSame = false;
            }

            // 全部元素長一樣（常見 [null,null,...]）→ 壓縮成 [N×value]
            if (allSame && size == count && count > 2)
                return $"[{size}×{parts[0]}]";

            if (size > max)
                return $"[{size} items: {string.Join(",", parts)},…]";
            return $"[{string.Join(",", parts)}]";
        }

        private static string FormatNested(SerializedProperty property, HierarchyExportContext ctx, int depth)
        {
            if (depth >= ctx.Options._maxNestedDepth) return "{…}";

            var sb = new StringBuilder("{");
            var endPath = property.GetEndProperty();
            var child = property.Copy();
            var first = true;
            if (child.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(child, endPath)) break;
                    if (IsNestedChildDefault(child, ctx)) continue;

                    if (!first) sb.Append(",");
                    first = false;
                    if (child.propertyType == SerializedPropertyType.Boolean)
                    {
                        sb.Append(child.name);
                        if (!child.boolValue) sb.Append("=off");
                    }
                    else
                    {
                        sb.Append(child.name).Append("=").Append(FormatValue(child, ctx, depth + 1));
                    }
                } while (child.NextVisible(false));
            }
            sb.Append("}");
            return sb.ToString();
        }

        // 巢狀欄位也逐一跟 component 預設值比對，避免整包 Generic 只因一個子欄位有改就全展開
        private static bool IsNestedChildDefault(SerializedProperty child, HierarchyExportContext ctx)
        {
            if (!ctx.Options._excludeDefaults || ctx.CurrentComponentType == null) return false;
            var defaultProp = ComponentDefaultCache.FindDefaultProperty(ctx.CurrentComponentType, child.propertyPath);
            return defaultProp != null && SerializedProperty.DataEquals(child, defaultProp);
        }

        private static string FormatObjectReference(SerializedProperty property, HierarchyExportContext ctx)
        {
            var obj = property.objectReferenceValue;
            if (obj == null) return "null";

            var baseRef = FormatObjectRefCore(obj, ctx);
            var declared = GetDeclaredFieldType(property);
            if (declared != null && declared != obj.GetType() && !(obj is GameObject))
                baseRef += $"#{obj.GetType().Name}";
            return baseRef;
        }

        private static string FormatObjectRefCore(Object obj, HierarchyExportContext ctx)
        {
            Transform targetTr = obj switch
            {
                GameObject go => go.transform,
                Component comp => comp.transform,
                _ => null
            };

            if (targetTr != null)
            {
                if (ctx.Root != null && (targetTr == ctx.Root || targetTr.IsChildOf(ctx.Root)))
                {
                    var rel = UnityTypeFormatter.GetRelativePath(ctx.Current, targetTr);
                    return "@" + rel;
                }

                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath))
                    return "res:" + StripAssetsPrefix(assetPath);

                var goForPath = obj is GameObject g ? g : ((Component)obj).gameObject;
                return "@/" + UnityTypeFormatter.GetGameObjectPath(goForPath);
            }

            var path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
                return "res:" + StripAssetsPrefix(path);

            return $"<{obj.GetType().Name}>";
        }

        public static string StripAssetsPrefix(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            return path.StartsWith("Assets/") ? path.Substring("Assets/".Length) : path;
        }

        private static Type GetDeclaredFieldType(SerializedProperty property)
        {
            try
            {
                var targetType = property.serializedObject.targetObject.GetType();
                var topName = property.propertyPath.Split('.')[0];
                var field = targetType.GetField(topName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) return null;
                var t = field.FieldType;
                return t.IsArray ? t.GetElementType() : t;
            }
            catch
            {
                return null;
            }
        }
    }
}
