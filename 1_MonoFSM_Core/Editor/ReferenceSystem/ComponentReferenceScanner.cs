using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Animation;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Runtime.Action;
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
                var elemType = GetCollectionElementType(field.FieldType);
                if (elemType == null)
                {
                    ScanValue(comp, field.GetValue(comp), field.FieldType, field.Name);
                    continue;
                }

                // 陣列 / List 欄位：逐個元素掃，路徑帶 index
                if (!IsScannableType(elemType)) continue;
                if (field.GetValue(comp) is not IEnumerable enumerable) continue;

                var index = 0;
                foreach (var item in enumerable)
                {
                    ScanValue(comp, item, elemType, $"{field.Name}[{index}]");
                    index++;
                }
            }
        }

        //取得陣列 / List<> 的元素型別，不是集合則回 null
        private static Type GetCollectionElementType(Type fieldType)
        {
            if (fieldType.IsArray)
                return fieldType.GetElementType();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                return fieldType.GetGenericArguments()[0];
            return null;
        }

        private static bool IsScannableType(Type type) =>
            typeof(Object).IsAssignableFrom(type)
            || typeof(AbstractVarWrapper).IsAssignableFrom(type)
            || typeof(ValueProvider).IsAssignableFrom(type);

        private static void ScanValue(Component comp, object value, Type declaredType, string fieldPath)
        {
            if (value == null) return;

            // 1. 直接引用：型別是 UnityEngine.Object 子類（Component, ScriptableObject 等）
            if (typeof(Object).IsAssignableFrom(declaredType))
                ScanDirectValue(comp, value, fieldPath);
            // 2. Variable 特殊：VarWrapper 間接引用
            else if (typeof(AbstractVarWrapper).IsAssignableFrom(declaredType))
                ScanVarWrapperValue(comp, value, fieldPath);
            // 3. Variable 特殊：ValueProvider 間接引用
            else if (typeof(ValueProvider).IsAssignableFrom(declaredType))
                ScanValueProviderValue(comp, value, fieldPath);
        }

        private static void ScanDirectValue(Component comp, object rawValue, string fieldPath)
        {
            var value = rawValue as Object;
            if (value == null) return;
            if (ReferenceEquals(value, comp)) return;

            AddToCache(value, CreateInfo(value, comp, fieldPath, ReferenceType.DirectField));
        }

        private static void ScanVarWrapperValue(Component comp, object wrapper, string fieldPath)
        {
            if (wrapper == null) return;

            var varField = FindFieldInHierarchy(wrapper.GetType(), "_var");
            if (varField == null) return;

            var variable = varField.GetValue(wrapper) as AbstractMonoVariable;
            if (variable == null) return;

            AddToCache(variable,
                CreateInfo(variable, comp, $"{fieldPath}._var", ReferenceType.VarWrapper));
        }

        private static void ScanValueProviderValue(Component comp, object rawProvider, string fieldPath)
        {
            if (rawProvider is not ValueProvider provider) return;

            var varRaw = provider.VarRaw;
            if (varRaw == null) return;

            AddToCache(varRaw,
                CreateInfo(varRaw, comp, $"{fieldPath} (ValueProvider)", ReferenceType.ValueProvider));
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
                OwnerEntity = ownerEntity,
                Category = GetCategory(referencingComponent, type)
            };
        }

        private static ReferenceCategory GetCategory(Component comp, ReferenceType type)
        {
            // AnimatorPlayAction 不繼承 AbstractStateAction，需另外判斷
            if (comp is AbstractStateAction || comp is AnimatorPlayAction)
                return ReferenceCategory.Action;
            if (comp is AbstractConditionBehaviour)
                return ReferenceCategory.Condition;
            // 透過 VarWrapper / ValueProvider 引用的非 Action/Condition 元件視為讀值
            if (type is ReferenceType.VarWrapper or ReferenceType.ValueProvider)
                return ReferenceCategory.Getter;
            return ReferenceCategory.Other;
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
