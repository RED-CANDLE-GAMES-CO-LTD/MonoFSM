using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Editor
{
    // 判斷 SerializedProperty 目前值是否等於該 Component 型別的預設值
    // 優先用「臨時 instance + SerializedProperty.DataEquals」，AddComponent 失敗則退回型別零值 heuristic
    public static class ComponentDefaultCache
    {
        private static GameObject _host;
        private static readonly Dictionary<Type, SerializedObject> _cache = new();
        private static readonly HashSet<Type> _heuristicOnly = new();
        private static bool _hooked;

        private static void EnsureHooked()
        {
            if (_hooked) return;
            _hooked = true;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.quitting += Clear;
        }

        // 取得該 component 型別預設 instance 上對應 propertyPath 的 property（無法建立預設 instance 時回 null）
        public static SerializedProperty FindDefaultProperty(Type componentType, string propertyPath)
        {
            EnsureHooked();
            if (_heuristicOnly.Contains(componentType)) return null;
            var defaultSo = GetDefault(componentType);
            return defaultSo?.FindProperty(propertyPath);
        }

        public static bool IsDefaultValue(SerializedProperty current, Type componentType)
        {
            EnsureHooked();

            if (!_heuristicOnly.Contains(componentType))
            {
                var defaultSo = GetDefault(componentType);
                if (defaultSo != null)
                {
                    var defaultProp = defaultSo.FindProperty(current.propertyPath);
                    if (defaultProp != null)
                        return SerializedProperty.DataEquals(current, defaultProp);
                }
            }

            return IsHeuristicDefault(current);
        }

        private static SerializedObject GetDefault(Type componentType)
        {
            if (_cache.TryGetValue(componentType, out var so))
                return so;

            EnsureHost();
            // 每個型別用獨立的子物件當宿主，避免 DisallowMultipleComponent / Renderer 互斥
            var holder = new GameObject(componentType.Name) { hideFlags = HideFlags.HideAndDontSave };
            holder.transform.SetParent(_host.transform);

            Component comp = null;
            try
            {
                comp = ObjectFactory.AddComponent(holder, componentType);
            }
            catch
            {
                try
                {
                    comp = holder.AddComponent(componentType);
                }
                catch
                {
                    comp = null;
                }
            }

            if (comp == null)
            {
                // 有些型別不能直接 AddComponent（如 ParticleSystemRenderer 要跟著 ParticleSystem）
                comp = holder.GetComponent(componentType);
                if (comp == null && typeof(ParticleSystemRenderer).IsAssignableFrom(componentType))
                {
                    try
                    {
                        holder.AddComponent<ParticleSystem>();
                        comp = holder.GetComponent(componentType);
                    }
                    catch
                    {
                        comp = null;
                    }
                }
            }

            if (comp == null)
            {
                Object.DestroyImmediate(holder);
                _heuristicOnly.Add(componentType);
                _cache[componentType] = null;
                return null;
            }

            var result = new SerializedObject(comp);
            _cache[componentType] = result;
            return result;
        }

        private static void EnsureHost()
        {
            if (_host != null) return;
            _host = new GameObject("_HierarchyTextExporterDefaultsHost")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        public static void Clear()
        {
            _cache.Clear();
            _heuristicOnly.Clear();
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
                _host = null;
            }
        }

        private static bool IsHeuristicDefault(SerializedProperty p)
        {
            if (p.isArray && p.propertyType != SerializedPropertyType.String)
                return p.arraySize == 0;

            return p.propertyType switch
            {
                SerializedPropertyType.Integer => p.longValue == 0,
                SerializedPropertyType.Boolean => p.boolValue == false,
                SerializedPropertyType.Float => p.floatValue == 0f,
                SerializedPropertyType.String => string.IsNullOrEmpty(p.stringValue),
                SerializedPropertyType.ObjectReference => p.objectReferenceValue == null,
                SerializedPropertyType.Enum => p.enumValueIndex == 0,
                SerializedPropertyType.Vector2 => p.vector2Value == Vector2.zero,
                SerializedPropertyType.Vector3 => p.vector3Value == Vector3.zero,
                SerializedPropertyType.Vector4 => p.vector4Value == Vector4.zero,
                SerializedPropertyType.Color => p.colorValue == default,
                SerializedPropertyType.ArraySize => p.intValue == 0,
                SerializedPropertyType.LayerMask => p.intValue == 0,
                SerializedPropertyType.Vector2Int => p.vector2IntValue == Vector2Int.zero,
                SerializedPropertyType.Vector3Int => p.vector3IntValue == Vector3Int.zero,
                _ => false
            };
        }
    }
}
