using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.DataProvider;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace MonoFSM.Core.Editor
{
    /// <summary>
    /// 用於選擇VariableTag的OdinSelector，支援搜尋功能
    /// </summary>
    public class VarTagPropertySelector : OdinSelector<VariableTag>
    {
        private readonly PropertyOfTypeProvider _target;
        private readonly VariableTag _currentSelection;

        public VarTagPropertySelector(PropertyOfTypeProvider target, VariableTag currentSelection = null)
        {
            _target = target;
            _currentSelection = currentSelection;
            DrawConfirmSelectionButton = false; // 單選不需要確認按鈕
            SelectionTree.Config.SelectMenuItemsOnMouseDown = true;
            SelectionTree.Config.ConfirmSelectionOnDoubleClick = true;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            // 啟用搜尋功能
            tree.Config.DrawSearchToolbar = true;
            
            // 添加空選項
            tree.Add("-- 不選擇變數 --", (VariableTag)null);
            
            // 獲取可用的VariableTag選項
            var availableTags = GetAvailableVariableTags();
            
            if (availableTags == null || !availableTags.Any())
            {
                tree.Add("無可用變數", (VariableTag)null);
                return;
            }

            // 按型別分組
            var groupedTags = availableTags
                .Where(tag => tag != null)
                .GroupBy(tag => GetVariableTypeGroup(tag.ValueType))
                .OrderBy(g => GetGroupPriority(g.Key))
                .ToList();

            foreach (var group in groupedTags)
            {
                var sortedTags = group.OrderBy(tag => tag.name).ToList();
                
                foreach (var tag in sortedTags)
                {
                    var displayName = $"{tag.name} ({GetTypeDisplayName(tag.ValueType)})";
                    var path = group.Key == "其他" ? displayName : $"{group.Key}/{displayName}";
                    
                    tree.Add(path, tag);
                }
            }
        }

        /// <summary>
        /// 獲取可用的VariableTag列表
        /// </summary>
        private IEnumerable<VariableTag> GetAvailableVariableTags()
        {
            try
            {
                // 嘗試從target獲取變數標籤選項
                var method = _target.GetType().GetMethod("GetParentVariableTags", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    var result = method.Invoke(_target, null);
                    if (result is IEnumerable<ValueDropdownItem<VariableTag>> dropdownItems)
                    {
                        return dropdownItems.Select(item => item.Value).Where(tag => tag != null);
                    }
                }

                // 備用方案：從MonoEntity獲取
                if (_target is PropertyOfTypeProvider provider)
                {
                    // 嘗試獲取ParentEntity
                    var entityField = provider.GetType().GetField("_parentEntity", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (entityField?.GetValue(provider) is MonoEntity entity)
                    {
                        var options = entity.GetVarTagOptions();
                        return options?.Select(item => item.Value).Where(tag => tag != null) ?? new List<VariableTag>();
                    }
                }

                return new List<VariableTag>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting variable tags: {e.Message}");
                return new List<VariableTag>();
            }
        }

        /// <summary>
        /// 根據變數型別獲取分組名稱
        /// </summary>
        private string GetVariableTypeGroup(Type valueType)
        {
            if (valueType == null) return "其他";
            
            if (valueType == typeof(string)) return "文字變數";
            if (valueType == typeof(int) || valueType == typeof(float) || valueType == typeof(double)) return "數值變數";
            if (valueType == typeof(bool)) return "布林變數";
            if (valueType == typeof(Vector2) || valueType == typeof(Vector3) || valueType == typeof(Vector4)) return "向量變數";
            if (valueType == typeof(Color)) return "顏色變數";
            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType)) return "物件變數";
            if (valueType.IsEnum) return "列舉變數";
            
            // MonoFSM 特定型別
            if (valueType.Name.Contains("MonoEntity")) return "實體變數";
            if (valueType.Name.Contains("Item") || valueType.Name.Contains("Slot")) return "物品變數";
            
            return "其他變數";
        }

        /// <summary>
        /// 獲取分組的優先順序
        /// </summary>
        private int GetGroupPriority(string groupName)
        {
            return groupName switch
            {
                "文字變數" => 1,
                "數值變數" => 2,
                "布林變數" => 3,
                "實體變數" => 4,
                "物品變數" => 5,
                "向量變數" => 6,
                "顏色變數" => 7,
                "列舉變數" => 8,
                "物件變數" => 9,
                _ => 99
            };
        }

        /// <summary>
        /// 獲取型別的顯示名稱
        /// </summary>
        private string GetTypeDisplayName(Type type)
        {
            if (type == null) return "unknown";
            
            return type.Name switch
            {
                "String" => "string",
                "Int32" => "int",
                "Single" => "float",
                "Double" => "double",
                "Boolean" => "bool",
                _ => type.Name
            };
        }
    }
}