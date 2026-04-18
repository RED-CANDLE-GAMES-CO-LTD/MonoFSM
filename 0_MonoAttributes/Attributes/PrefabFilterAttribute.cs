using System;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.Attributes
{
    /// <summary>
    /// 用於過濾Prefab的Attribute
    /// 可以根據Prefab上的Component類型來過濾下拉選單中的選項
    /// </summary>
    public class PrefabFilterAttribute : Attribute
    {
        /// <summary>
        /// 必須包含的Component類型
        /// </summary>
        public Type RequiredComponentType { get; }

        /// <summary>
        /// 是否允許繼承的Component類型
        /// </summary>
        public bool AllowInheritedTypes { get; }

        /// <summary>
        /// 自定義錯誤訊息
        /// </summary>
        public string CustomErrorMessage { get; }

        /// <summary>
        /// 是否只顯示啟用的GameObject
        /// </summary>
        public bool OnlyActiveGameObjects { get; }

        /// <summary>
        /// 無參數構造函數 - 不進行任何過濾，顯示所有Prefab
        /// </summary>
        public PrefabFilterAttribute()
            : this(null) { }

        /// <summary>
        /// 構造函數
        /// </summary>
        /// <param name="requiredComponentType">必須包含的Component類型</param>
        /// <param name="allowInheritedTypes">是否允許繼承的Component類型</param>
        /// <param name="onlyActiveGameObjects">是否只顯示啟用的GameObject</param>
        /// <param name="customErrorMessage">自定義錯誤訊息</param>
        public PrefabFilterAttribute(
            Type requiredComponentType,
            bool allowInheritedTypes = true,
            bool onlyActiveGameObjects = false,
            string customErrorMessage = null
        )
        {
            RequiredComponentType = requiredComponentType;
            AllowInheritedTypes = allowInheritedTypes;
            OnlyActiveGameObjects = onlyActiveGameObjects;
            CustomErrorMessage = customErrorMessage;
        }

        /// <summary>
        /// 驗證GameObject是否符合過濾條件
        /// </summary>
        public bool ValidatePrefab(GameObject prefab)
        {
            if (prefab == null)
                return false;

            // 如果沒有指定Component類型，則接受所有Prefab
            if (RequiredComponentType == null)
                return true;

            // 檢查是否為啟用狀態（如果有要求）
            if (OnlyActiveGameObjects && !prefab.activeInHierarchy)
                return false;

            // 檢查是否包含必要的Component
            if (AllowInheritedTypes)
            {
                return prefab.GetComponent(RequiredComponentType) != null;
            }
            else
            {
                // 嚴格類型匹配
                var component = prefab.GetComponent(RequiredComponentType);
                return component != null && component.GetType() == RequiredComponentType;
            }
        }

        /// <summary>
        /// 獲取Asset搜尋過濾器
        /// </summary>
        public string GetAssetSearchFilter()
        {
            return "t=MonoObj";
        }
    }
}
