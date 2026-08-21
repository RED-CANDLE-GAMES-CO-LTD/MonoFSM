using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Editor
{
    /// <summary>
    /// 將 Prefab 轉換為 Godot tscn 風格的文字格式
    /// </summary>
    public static class PrefabToTextExporter
    {
        // 預設值快取
        private static readonly Dictionary<Type, Dictionary<string, object>> _defaultCache = new();

        public static string Export(GameObject prefab, PrefabExportSettings settings = null)
        {
            if (prefab == null)
                return string.Empty;

            settings ??= PrefabExportSettings.CreateDefault();

            var sb = new StringBuilder();

            // 輸出標頭
            sb.AppendLine("[gd_scene format=3 uid=\"unity_prefab\"]");
            sb.AppendLine();

            // 遞迴遍歷 GameObject 階層
            TraverseGameObject(prefab, null, sb, settings);

            return sb.ToString();
        }

        private static void TraverseGameObject(GameObject go, string parentPath, StringBuilder sb, PrefabExportSettings settings)
        {
            // 決定節點類型
            var nodeType = DetermineNodeType(go);

            // 輸出節點標頭
            if (parentPath == null)
            {
                sb.AppendLine($"[node name=\"{go.name}\" type=\"{nodeType}\"]");
            }
            else
            {
                sb.AppendLine($"[node name=\"{go.name}\" type=\"{nodeType}\" parent=\"{parentPath}\"]");
            }

            // 輸出 Transform
            ExportTransform(go.transform, sb, settings);

            // 輸出 Components
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                if (comp is Transform) continue;
                if (!settings.ShouldIncludeComponent(comp.GetType())) continue;

                ExportComponent(comp, sb, settings);
            }

            sb.AppendLine();

            // 遞迴子物件
            var newPath = parentPath == null ? "." : (parentPath == "." ? go.name : $"{parentPath}/{go.name}");

            foreach (Transform child in go.transform)
            {
                TraverseGameObject(child.gameObject, newPath, sb, settings);
            }
        }

        private static string DetermineNodeType(GameObject go)
        {
            // 根據 Component 決定類似 Godot 的節點類型
            if (go.GetComponent<Camera>()) return "Camera3D";
            if (go.GetComponent<Light>()) return "Light3D";
            if (go.GetComponent<AudioSource>()) return "AudioStreamPlayer3D";
            if (go.GetComponent<Rigidbody>()) return "RigidBody3D";
            if (go.GetComponent<Rigidbody2D>()) return "RigidBody2D";
            if (go.GetComponent<CharacterController>()) return "CharacterBody3D";
            if (go.GetComponent<Collider>()) return "CollisionShape3D";
            if (go.GetComponent<Collider2D>()) return "CollisionShape2D";
            if (go.GetComponent<MeshRenderer>() || go.GetComponent<SkinnedMeshRenderer>()) return "MeshInstance3D";
            if (go.GetComponent<SpriteRenderer>()) return "Sprite2D";
            if (go.GetComponent<ParticleSystem>()) return "GPUParticles3D";
            if (go.GetComponent<Animator>()) return "AnimationPlayer";
            if (go.GetComponent<Canvas>()) return "CanvasLayer";
            if (go.GetComponent<RectTransform>()) return "Control";

            return "Node3D";
        }

        private static void ExportTransform(Transform transform, StringBuilder sb, PrefabExportSettings settings)
        {
            Vector3 position, rotation, scale;

            if (settings._useLocalCoordinates)
            {
                position = transform.localPosition;
                rotation = transform.localEulerAngles;
                scale = transform.localScale;
            }
            else
            {
                position = transform.position;
                rotation = transform.eulerAngles;
                scale = transform.lossyScale;
            }

            bool isDefault = position == Vector3.zero &&
                             (rotation == Vector3.zero || Quaternion.Euler(rotation) == Quaternion.identity) &&
                             scale == Vector3.one;

            if (settings._excludeDefaultTransform && isDefault)
                return;

            if (position != Vector3.zero)
                sb.AppendLine($"position = {UnityTypeFormatter.FormatValue(position)}");

            if (rotation != Vector3.zero && Quaternion.Euler(rotation) != Quaternion.identity)
                sb.AppendLine($"rotation = {UnityTypeFormatter.FormatValue(rotation)}");

            if (scale != Vector3.one)
                sb.AppendLine($"scale = {UnityTypeFormatter.FormatValue(scale)}");
        }

        private static void ExportComponent(Component component, StringBuilder sb, PrefabExportSettings settings)
        {
            var componentType = component.GetType();

            if (settings._includeComments)
            {
                sb.AppendLine($"# Component: {componentType.Name}");
            }

            var serializedObject = new SerializedObject(component);
            var property = serializedObject.GetIterator();

            // 取得預設值
            var defaults = GetDefaultValues(componentType);

            if (property.NextVisible(true))
            {
                do
                {
                    // 跳過排除的欄位
                    if (settings.ShouldExcludeField(property.name))
                        continue;

                    // 跳過腳本引用
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.name == "m_Script")
                        continue;

                    // 檢查是否為 public 欄位
                    if (settings._onlyPublicFields && !IsPublicField(componentType, property.name))
                        continue;

                    // 跳過預設值
                    if (settings._excludeDefaults && IsDefaultValue(property, defaults))
                        continue;

                    // 處理陣列/列表
                    if (property.isArray && property.propertyType != SerializedPropertyType.String)
                    {
                        ExportArrayProperty(property, sb, settings);
                    }
                    else
                    {
                        ExportProperty(property, sb);
                    }
                }
                while (property.NextVisible(false));
            }
        }

        private static void ExportProperty(SerializedProperty property, StringBuilder sb)
        {
            var value = UnityTypeFormatter.FormatPropertyValue(property);
            sb.AppendLine($"{property.name} = {value}");
        }

        private static void ExportArrayProperty(SerializedProperty property, StringBuilder sb, PrefabExportSettings settings)
        {
            if (property.arraySize == 0)
            {
                sb.AppendLine($"{property.name} = []");
                return;
            }

            var elements = new List<string>();
            for (int i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                elements.Add(UnityTypeFormatter.FormatPropertyValue(element));
            }

            sb.AppendLine($"{property.name} = [{string.Join(", ", elements)}]");
        }

        private static bool IsPublicField(Type componentType, string fieldName)
        {
            // 移除 Unity 的命名前綴
            var cleanName = fieldName.TrimStart('_', 'm', '_');

            // 嘗試找到對應的欄位
            var field = componentType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance) ??
                        componentType.GetField(cleanName, BindingFlags.Public | BindingFlags.Instance);

            return field != null;
        }

        private static Dictionary<string, object> GetDefaultValues(Type componentType)
        {
            if (_defaultCache.TryGetValue(componentType, out var cached))
                return cached;

            var defaults = new Dictionary<string, object>();

            try
            {
                // 建立臨時物件來獲取預設值
                var tempGO = new GameObject("_TempForDefaults") { hideFlags = HideFlags.HideAndDontSave };
                var defaultComp = tempGO.AddComponent(componentType);

                if (defaultComp != null)
                {
                    var serializedDefault = new SerializedObject(defaultComp);
                    var prop = serializedDefault.GetIterator();

                    if (prop.NextVisible(true))
                    {
                        do
                        {
                            defaults[prop.propertyPath] = GetPropertyValue(prop);
                        }
                        while (prop.NextVisible(false));
                    }
                }

                Object.DestroyImmediate(tempGO);
            }
            catch
            {
                // 某些 Component 無法動態建立
            }

            _defaultCache[componentType] = defaults;
            return defaults;
        }

        private static bool IsDefaultValue(SerializedProperty property, Dictionary<string, object> defaults)
        {
            if (!defaults.TryGetValue(property.propertyPath, out var defaultValue))
                return false;

            var currentValue = GetPropertyValue(property);

            if (defaultValue == null && currentValue == null)
                return true;

            if (defaultValue == null || currentValue == null)
                return false;

            return defaultValue.Equals(currentValue);
        }

        private static object GetPropertyValue(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.longValue,
                SerializedPropertyType.Boolean => property.boolValue,
                SerializedPropertyType.Float => property.floatValue,
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.Color => property.colorValue,
                SerializedPropertyType.ObjectReference => property.objectReferenceInstanceIDValue,
                SerializedPropertyType.LayerMask => property.intValue,
                SerializedPropertyType.Enum => property.enumValueIndex,
                SerializedPropertyType.Vector2 => property.vector2Value,
                SerializedPropertyType.Vector3 => property.vector3Value,
                SerializedPropertyType.Vector4 => property.vector4Value,
                SerializedPropertyType.Rect => property.rectValue,
                SerializedPropertyType.ArraySize => property.intValue,
                SerializedPropertyType.Bounds => property.boundsValue,
                SerializedPropertyType.Quaternion => property.quaternionValue,
                SerializedPropertyType.Vector2Int => property.vector2IntValue,
                SerializedPropertyType.Vector3Int => property.vector3IntValue,
                SerializedPropertyType.RectInt => property.rectIntValue,
                SerializedPropertyType.BoundsInt => property.boundsIntValue,
                _ => null
            };
        }

        /// <summary>
        /// 清除預設值快取
        /// </summary>
        public static void ClearDefaultCache()
        {
            _defaultCache.Clear();
        }
    }
}
