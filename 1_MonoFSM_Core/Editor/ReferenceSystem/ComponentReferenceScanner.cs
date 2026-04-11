using System;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Editor.ReferenceSystem
{
    /// <summary>
    /// 泛化的引用掃描器 — 掃描 root 底下所有 Component 的所有欄位，
    /// 建立 Object → 引用者 的反查快取。一次掃描涵蓋所有型別。
    /// </summary>
    public static class ComponentReferenceScanner
    {
        private static Dictionary<Object, List<ComponentReferenceInfo>> _cache = new();
        private static GameObject _cachedRoot;

        public static GameObject CachedRoot => _cachedRoot;
        public static bool HasValidCache => _cachedRoot != null && _cache.Count > 0;

        public static void ClearCache()
        {
            _cache.Clear();
            _cachedRoot = null;
        }

        public static void ScanFromRoot(GameObject root)
        {
            ClearCache();
            if (root == null) return;

            _cachedRoot = root;
            var components = root.GetComponentsInChildren<Component>(true);

            foreach (var comp in components)
            {
                if (comp == null) continue;
                ScanComponent(comp);
            }
        }

        public static List<ComponentReferenceInfo> GetReferences(Object target)
        {
            if (target == null)
                return new List<ComponentReferenceInfo>();

            return _cache.TryGetValue(target, out var list)
                ? list
                : new List<ComponentReferenceInfo>();
        }

        public static IEnumerable<Object> GetAllReferencedTargets() => _cache.Keys;

        private static void ScanComponent(Component comp)
        {
            var compType = comp.GetType();
            var fields = compType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                // 1. 直接引用：欄位型別是 UnityEngine.Object 子類（Component, ScriptableObject 等）
                if (typeof(Object).IsAssignableFrom(field.FieldType))
                {
                    ScanDirectField(comp, field);
                }
                // 2. Variable 特殊：VarWrapper 間接引用
                else if (typeof(AbstractVarWrapper).IsAssignableFrom(field.FieldType))
                {
                    ScanVarWrapperField(comp, field);
                }
                // 3. Variable 特殊：ValueProvider 間接引用
                else if (typeof(ValueProvider).IsAssignableFrom(field.FieldType))
                {
                    ScanValueProviderField(comp, field);
                }
            }
        }

        private static void ScanDirectField(Component comp, FieldInfo field)
        {
            var value = field.GetValue(comp) as Object;
            if (value == null) return;
            if (ReferenceEquals(value, comp)) return;

            AddToCache(value, CreateInfo(value, comp, field.Name, ReferenceType.DirectField));
        }

        private static void ScanVarWrapperField(Component comp, FieldInfo wrapperField)
        {
            var wrapper = wrapperField.GetValue(comp);
            if (wrapper == null) return;

            var varField = FindFieldInHierarchy(wrapper.GetType(), "_var");
            if (varField == null) return;

            var variable = varField.GetValue(wrapper) as AbstractMonoVariable;
            if (variable == null) return;

            AddToCache(variable,
                CreateInfo(variable, comp, $"{wrapperField.Name}._var", ReferenceType.VarWrapper));
        }

        private static void ScanValueProviderField(Component comp, FieldInfo providerField)
        {
            var provider = providerField.GetValue(comp) as ValueProvider;
            if (provider == null) return;

            var varRaw = provider.VarRaw;
            if (varRaw == null) return;

            AddToCache(varRaw,
                CreateInfo(varRaw, comp, $"{providerField.Name} (ValueProvider)", ReferenceType.ValueProvider));
        }

        private static FieldInfo FindFieldInHierarchy(Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field;
                type = type.BaseType;
            }
            return null;
        }

        private static ComponentReferenceInfo CreateInfo(
            Object target, Component referencingComponent, string fieldPath, ReferenceType type)
        {
            var ownerEntity = referencingComponent.GetComponentInParent<MonoEntity>();
            var targetEntity = target is Component c ? c.GetComponentInParent<MonoEntity>() : null;

            return new ComponentReferenceInfo
            {
                Target = target,
                ReferencingComponent = referencingComponent,
                FieldPath = fieldPath,
                Type = type,
                Scope = (ownerEntity == targetEntity && ownerEntity != null)
                    ? ReferenceScope.Local
                    : ReferenceScope.CrossEntity,
                OwnerEntity = ownerEntity
            };
        }

        private static void AddToCache(Object target, ComponentReferenceInfo info)
        {
            if (!_cache.TryGetValue(target, out var list))
            {
                list = new List<ComponentReferenceInfo>();
                _cache[target] = list;
            }
            list.Add(info);
        }
    }
}
