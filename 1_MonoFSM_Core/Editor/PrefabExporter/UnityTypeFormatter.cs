using System;
using System.Collections;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Editor
{
    /// <summary>
    /// 將 Unity 類型格式化為 Godot tscn 風格的文字
    /// </summary>
    public static class UnityTypeFormatter
    {
        public static string FormatValue(object value)
        {
            if (value == null)
                return "null";

            return value switch
            {
                Vector2 v2 => $"Vector2({v2.x}, {v2.y})",
                Vector3 v3 => $"Vector3({v3.x}, {v3.y}, {v3.z})",
                Vector4 v4 => $"Vector4({v4.x}, {v4.y}, {v4.z}, {v4.w})",
                Vector2Int v2i => $"Vector2i({v2i.x}, {v2i.y})",
                Vector3Int v3i => $"Vector3i({v3i.x}, {v3i.y}, {v3i.z})",
                Quaternion q => FormatQuaternion(q),
                Color c => $"Color({c.r:F3}, {c.g:F3}, {c.b:F3}, {c.a:F3})",
                Color32 c32 => $"Color({c32.r / 255f:F3}, {c32.g / 255f:F3}, {c32.b / 255f:F3}, {c32.a / 255f:F3})",
                Rect r => $"Rect2({r.x}, {r.y}, {r.width}, {r.height})",
                RectInt ri => $"Rect2i({ri.x}, {ri.y}, {ri.width}, {ri.height})",
                Bounds b => $"AABB({FormatValue(b.center)}, {FormatValue(b.size)})",
                BoundsInt bi => $"AABB({FormatValue(bi.center)}, {FormatValue(bi.size)})",
                Matrix4x4 m => FormatMatrix(m),
                AnimationCurve curve => FormatAnimationCurve(curve),
                Gradient gradient => FormatGradient(gradient),
                LayerMask layer => $"LayerMask({layer.value})",
                Object obj => FormatUnityObject(obj),
                Enum e => $"\"{e}\"",
                string s => $"\"{EscapeString(s)}\"",
                bool b => b ? "true" : "false",
                float f => FormatFloat(f),
                double d => FormatDouble(d),
                int i => i.ToString(),
                long l => l.ToString(),
                IList list => FormatArray(list),
                _ => value.ToString()
            };
        }

        public static string FormatPropertyValue(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.longValue.ToString(),
                SerializedPropertyType.Boolean => property.boolValue ? "true" : "false",
                SerializedPropertyType.Float => FormatFloat(property.floatValue),
                SerializedPropertyType.String => $"\"{EscapeString(property.stringValue)}\"",
                SerializedPropertyType.Color => FormatValue(property.colorValue),
                SerializedPropertyType.ObjectReference => FormatObjectReference(property),
                SerializedPropertyType.LayerMask => $"LayerMask({property.intValue})",
                SerializedPropertyType.Enum => FormatEnum(property),
                SerializedPropertyType.Vector2 => FormatValue(property.vector2Value),
                SerializedPropertyType.Vector3 => FormatValue(property.vector3Value),
                SerializedPropertyType.Vector4 => FormatValue(property.vector4Value),
                SerializedPropertyType.Rect => FormatValue(property.rectValue),
                SerializedPropertyType.ArraySize => property.intValue.ToString(),
                SerializedPropertyType.Character => $"'{(char)property.intValue}'",
                SerializedPropertyType.AnimationCurve => FormatAnimationCurve(property.animationCurveValue),
                SerializedPropertyType.Bounds => FormatValue(property.boundsValue),
                SerializedPropertyType.Quaternion => FormatQuaternion(property.quaternionValue),
                SerializedPropertyType.ExposedReference => FormatExposedReference(property),
                SerializedPropertyType.Vector2Int => FormatValue(property.vector2IntValue),
                SerializedPropertyType.Vector3Int => FormatValue(property.vector3IntValue),
                SerializedPropertyType.RectInt => FormatValue(property.rectIntValue),
                SerializedPropertyType.BoundsInt => FormatValue(property.boundsIntValue),
                SerializedPropertyType.Hash128 => $"Hash128(\"{property.hash128Value}\")",
                _ => $"<{property.propertyType}>"
            };
        }

        private static string FormatEnum(SerializedProperty property)
        {
            var enumNames = property.enumNames;
            var index = property.enumValueIndex;

            // 檢查索引是否有效
            if (enumNames == null || enumNames.Length == 0)
                return $"\"{property.intValue}\"";

            if (index < 0 || index >= enumNames.Length)
                return $"\"{property.intValue}\"";

            return $"\"{enumNames[index]}\"";
        }

        private static string FormatQuaternion(Quaternion q)
        {
            var euler = q.eulerAngles;
            return $"Vector3({euler.x:F2}, {euler.y:F2}, {euler.z:F2})";
        }

        private static string FormatMatrix(Matrix4x4 m)
        {
            var sb = new StringBuilder("Transform3D(");
            for (int i = 0; i < 16; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatFloat(m[i]));
            }
            sb.Append(")");
            return sb.ToString();
        }

        private static string FormatAnimationCurve(AnimationCurve curve)
        {
            if (curve == null || curve.keys.Length == 0)
                return "Curve([])";

            var sb = new StringBuilder("Curve([");
            for (int i = 0; i < curve.keys.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                var key = curve.keys[i];
                sb.Append($"({key.time:F3}, {key.value:F3})");
            }
            sb.Append("])");
            return sb.ToString();
        }

        private static string FormatGradient(Gradient gradient)
        {
            if (gradient == null)
                return "Gradient()";

            var sb = new StringBuilder("Gradient([");
            var colorKeys = gradient.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                var key = colorKeys[i];
                sb.Append($"({key.time:F3}, {FormatValue(key.color)})");
            }
            sb.Append("])");
            return sb.ToString();
        }

        private static string FormatUnityObject(Object obj) => FormatUnityObject(obj, null);

        // containerAssetPath：目前正在序列化的 SerializedObject 所屬的 asset path（同一份 prefab 內的
        // node/component reference 即使 IsPersistent 也該當 NodePath，而不是誤判成外部 asset）
        private static string FormatUnityObject(Object obj, string containerAssetPath)
        {
            if (obj == null)
                return "null";

            var isNodeLike = obj is GameObject || obj is Component;
            if (isNodeLike)
            {
                if (!EditorUtility.IsPersistent(obj))
                    return $"NodePath(\"{GetNodeLikePath(obj)}\")";

                var objPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(containerAssetPath) && objPath == containerAssetPath)
                    return $"NodePath(\"{GetNodeLikePath(obj)}\")";
            }

            var path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                return $"ExtResource(\"{path}\")";
            }

            // Scene object reference
            if (obj is GameObject go)
            {
                return $"NodePath(\"{GetGameObjectPath(go)}\")";
            }

            if (obj is Component comp)
            {
                return $"NodePath(\"{GetGameObjectPath(comp.gameObject)}\")";
            }

            return $"<{obj.GetType().Name}: {obj.name}>";
        }

        private static string GetNodeLikePath(Object obj)
        {
            if (obj is GameObject go) return GetGameObjectPath(go);
            if (obj is Component comp) return GetGameObjectPath(comp.gameObject);
            return obj.name;
        }

        private static string FormatObjectReference(SerializedProperty property)
        {
            var obj = property.objectReferenceValue;
            if (obj == null)
                return "null";

            var containerAssetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
            return FormatUnityObject(obj, containerAssetPath);
        }

        private static string FormatExposedReference(SerializedProperty property)
        {
            var exposedNameProp = property.FindPropertyRelative("exposedName");
            if (exposedNameProp != null && !string.IsNullOrEmpty(exposedNameProp.stringValue))
            {
                return $"ExposedRef(\"{exposedNameProp.stringValue}\")";
            }
            return "null";
        }

        private static string FormatArray(IList list)
        {
            if (list == null || list.Count == 0)
                return "[]";

            var sb = new StringBuilder("[");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(FormatValue(list[i]));
            }
            sb.Append("]");
            return sb.ToString();
        }

        private static string FormatFloat(float f)
        {
            if (float.IsNaN(f)) return "NaN";
            if (float.IsPositiveInfinity(f)) return "inf";
            if (float.IsNegativeInfinity(f)) return "-inf";

            // 移除不必要的小數點
            if (Mathf.Approximately(f, Mathf.Round(f)))
                return ((int)f).ToString();

            return f.ToString("G");
        }

        private static string FormatDouble(double d)
        {
            if (double.IsNaN(d)) return "NaN";
            if (double.IsPositiveInfinity(d)) return "inf";
            if (double.IsNegativeInfinity(d)) return "-inf";

            if (Math.Abs(d - Math.Round(d)) < 0.0001)
                return ((long)d).ToString();

            return d.ToString("G");
        }

        private static string EscapeString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        public static string GetGameObjectPath(GameObject go)
        {
            if (go == null)
                return string.Empty;

            var path = go.name;
            var parent = go.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public static string GetRelativePath(Transform from, Transform to)
        {
            if (from == null || to == null)
                return string.Empty;

            if (from == to)
                return ".";

            // 找到共同祖先
            var fromAncestors = new System.Collections.Generic.List<Transform>();
            var current = from;
            while (current != null)
            {
                fromAncestors.Add(current);
                current = current.parent;
            }

            var toAncestors = new System.Collections.Generic.List<Transform>();
            current = to;
            while (current != null)
            {
                toAncestors.Add(current);
                current = current.parent;
            }

            // 找到共同祖先
            Transform commonAncestor = null;
            int fromIndex = -1, toIndex = -1;

            for (int i = 0; i < fromAncestors.Count; i++)
            {
                for (int j = 0; j < toAncestors.Count; j++)
                {
                    if (fromAncestors[i] == toAncestors[j])
                    {
                        commonAncestor = fromAncestors[i];
                        fromIndex = i;
                        toIndex = j;
                        break;
                    }
                }
                if (commonAncestor != null) break;
            }

            if (commonAncestor == null)
                return GetGameObjectPath(to.gameObject);

            var sb = new StringBuilder();

            // 往上走到共同祖先
            for (int i = 0; i < fromIndex; i++)
            {
                if (sb.Length > 0) sb.Append("/");
                sb.Append("..");
            }

            // 往下走到目標
            for (int i = toIndex - 1; i >= 0; i--)
            {
                if (sb.Length > 0) sb.Append("/");
                sb.Append(toAncestors[i].name);
            }

            return sb.Length > 0 ? sb.ToString() : ".";
        }
    }
}
