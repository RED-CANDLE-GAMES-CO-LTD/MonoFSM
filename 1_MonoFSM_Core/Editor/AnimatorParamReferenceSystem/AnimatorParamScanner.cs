using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Animation;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Foundation;
using UnityEngine;

namespace MonoFSM.Editor.AnimatorParamReferenceSystem
{
    /// <summary>
    /// 掃描 Hierarchy 中所有設定 Animator Parameter 的 Action
    /// </summary>
    public static class AnimatorParamScanner
    {
        // paramName -> List<AnimatorParamInfo>
        private static Dictionary<string, List<AnimatorParamInfo>> _paramCache = new();
        private static GameObject _cachedRoot;

        public static GameObject CachedRoot => _cachedRoot;
        public static bool HasValidCache => _cachedRoot != null && _paramCache.Count > 0;

        public static void ClearCache()
        {
            _paramCache.Clear();
            _cachedRoot = null;
        }

        public static void ScanFromRoot(GameObject root)
        {
            ClearCache();
            if (root == null) return;

            _cachedRoot = root;

            // 掃描 AbstractAnimatorSetValueAction 子類
            var setValueActions =
                root.GetComponentsInChildren<AbstractAnimatorSetParamAction>(true);
            foreach (var action in setValueActions)
            {
                if (action == null) continue;
                ScanAbstractAnimatorSetValueAction(action);
            }

            // 掃描 AnimatorParameterSetValueAction
            var paramActions = root.GetComponentsInChildren<AnimatorParameterSetValueAction>(true);
            foreach (var action in paramActions)
            {
                if (action == null) continue;
                ScanAnimatorParameterSetValueAction(action);
            }

            // 掃描 AnimatorPlayAction
            var playActions = root.GetComponentsInChildren<AnimatorPlayAction>(true);
            foreach (var action in playActions)
            {
                if (action == null) continue;
                ScanAnimatorPlayAction(action);
            }
        }

        /// <summary>
        /// 取得所有參數名稱
        /// </summary>
        public static IEnumerable<string> GetAllParamNames()
        {
            return _paramCache.Keys;
        }

        /// <summary>
        /// 取得指定參數名稱的所有設定資訊
        /// </summary>
        public static List<AnimatorParamInfo> GetInfosByParam(string paramName)
        {
            if (paramName != null && _paramCache.TryGetValue(paramName, out var list))
                return list;
            return new List<AnimatorParamInfo>();
        }

        /// <summary>
        /// 取得所有快取資料 (用於全部列出)
        /// </summary>
        public static Dictionary<string, List<AnimatorParamInfo>> GetAllCache()
        {
            return _paramCache;
        }

        private static void ScanAbstractAnimatorSetValueAction(
            AbstractAnimatorSetParamAction action)
        {
            var paramName = action._parameterName;
            if (string.IsNullOrEmpty(paramName)) return;

            // 透過 reflection 取得 Animator property
            var animatorProp = typeof(AbstractAnimatorSetParamAction)
                .GetProperty("Animator", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var animator = animatorProp?.GetValue(action) as Animator;

            var stateName = GetStateName(action);
            var description = action.Description;

            var info = new AnimatorParamInfo
            {
                ParameterName = paramName,
                ActionComponent = action,
                TargetAnimator = animator,
                StateName = stateName,
                ActionTypeName = action.GetType().Name,
                ActionDescription = description
            };

            AddToCache(paramName, info);
        }

        private static void ScanAnimatorParameterSetValueAction(AnimatorParameterSetValueAction action)
        {
            var paramName = action.ParameterName;
            if (string.IsNullOrEmpty(paramName)) return;

            // 透過 reflection 取得 animator property
            var animatorProp = typeof(AnimatorParameterSetValueAction)
                .GetProperty("animator", BindingFlags.Instance | BindingFlags.NonPublic);
            var animator = animatorProp?.GetValue(action) as Animator;

            var stateName = GetStateName(action);
            var description = action.Description;

            var info = new AnimatorParamInfo
            {
                ParameterName = paramName,
                ActionComponent = action,
                TargetAnimator = animator,
                StateName = stateName,
                ActionTypeName = action.GetType().Name,
                ActionDescription = description
            };

            AddToCache(paramName, info);
        }

        private static void ScanAnimatorPlayAction(AnimatorPlayAction action)
        {
            var stateName = action.stateName;
            if (string.IsNullOrEmpty(stateName)) return;

            // AnimatorPlayAction 的 _animator 是 public field
            var animator = action._animator;

            var fsmStateName = GetStateName(action);
            var description = action.Description;

            var info = new AnimatorParamInfo
            {
                ParameterName = stateName,
                ActionComponent = action,
                TargetAnimator = animator,
                StateName = fsmStateName,
                ActionTypeName = nameof(AnimatorPlayAction),
                ActionDescription = description
            };

            AddToCache(stateName, info);
        }

        private static string GetStateName(Component action)
        {
            // Action 是 State 的 child，往上找 GeneralState
            var parent = action.transform.parent;
            if (parent != null)
                return parent.name;
            return "(Unknown State)";
        }

        private static void AddToCache(string paramName, AnimatorParamInfo info)
        {
            if (!_paramCache.TryGetValue(paramName, out var list))
            {
                list = new List<AnimatorParamInfo>();
                _paramCache[paramName] = list;
            }
            list.Add(info);
        }
    }
}
