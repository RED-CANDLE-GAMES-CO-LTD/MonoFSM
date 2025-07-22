using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace MonoFSM.Core.Editor
{
    /// <summary>
    /// 用於選擇型別屬性的OdinSelector，支援搜尋功能
    /// </summary>
    public class FieldPathPropertySelector : OdinSelector<string>
    {
        private readonly Type _targetType;
        private readonly string _currentSelection;
        private readonly Dictionary<string, Type> _propertyTypeMap = new Dictionary<string, Type>();

        public FieldPathPropertySelector(Type targetType, string currentSelection = "")
        {
            _targetType = targetType;
            _currentSelection = currentSelection;
            DrawConfirmSelectionButton = false; // 單選不需要確認按鈕
            SelectionTree.Config.SelectMenuItemsOnMouseDown = true;
            SelectionTree.Config.ConfirmSelectionOnDoubleClick = true;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            // 啟用搜尋功能
            tree.Config.DrawSearchToolbar = true;
            
            if (_targetType == null)
            {
                tree.Add("錯誤/無法確定型別", "");
                return;
            }

            // 添加空選項
            tree.Add("-- 選擇屬性 --", "");

            // 建立屬性分組
            var propertyGroups = new Dictionary<string, List<(string name, Type type, MemberInfo member)>>();
            
            // 獲取所有可讀的公共屬性
            var properties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0) // 排除索引器
                .Select(p => (name: p.Name, type: p.PropertyType, member: (MemberInfo)p));

            // 獲取所有公共欄位
            var fields = _targetType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => (name: f.Name, type: f.FieldType, member: (MemberInfo)f));

            // 合併並分組
            var allMembers = properties.Concat(fields).ToList();
            
            foreach (var (name, type, member) in allMembers)
            {
                if (string.IsNullOrEmpty(name)) continue;
                
                // 根據型別分組
                var groupName = GetGroupName(type);
                if (!propertyGroups.ContainsKey(groupName))
                {
                    propertyGroups[groupName] = new List<(string, Type, MemberInfo)>();
                }
                
                propertyGroups[groupName].Add((name, type, member));
                _propertyTypeMap[name] = type;
            }

            // 按組別添加到樹中
            var sortedGroups = propertyGroups.OrderBy(g => GetGroupPriority(g.Key)).ToList();
            
            foreach (var group in sortedGroups)
            {
                var sortedMembers = group.Value.OrderBy(m => m.name).ToList();
                
                foreach (var (name, type, _) in sortedMembers)
                {
                    var displayName = $"{name} ({GetTypeDisplayName(type)})";
                    var path = group.Key == "其他" ? displayName : $"{group.Key}/{displayName}";
                    
                    tree.Add(path, name);
                }
            }

            // 如果沒有找到任何成員
            if (allMembers.Count == 0)
            {
                tree.Add("無可用屬性", "");
            }
        }

        /// <summary>
        /// 根據型別獲取分組名稱
        /// </summary>
        private string GetGroupName(Type type)
        {
            if (type == null) return "其他";
            
            // 基本型別
            if (type == typeof(string)) return "文字";
            if (type == typeof(int) || type == typeof(float) || type == typeof(double) || 
                type == typeof(long) || type == typeof(short) || type == typeof(byte)) return "數值";
            if (type == typeof(bool)) return "布林";
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4)) return "向量";
            if (type == typeof(Color) || type == typeof(Color32)) return "顏色";
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return "Unity物件";
            if (type.IsArray) return "陣列";
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return "列表";
            if (type.IsEnum) return "列舉";
            
            // MonoFSM 相關型別
            if (type.Name.StartsWith("Var")) return "變數";
            if (type.Name.Contains("MonoEntity")) return "實體";
            if (type.Name.Contains("Item") || type.Name.Contains("Slot")) return "物品系統";
            
            return "其他";
        }

        /// <summary>
        /// 獲取分組的優先順序（數字越小越靠前）
        /// </summary>
        private int GetGroupPriority(string groupName)
        {
            return groupName switch
            {
                "文字" => 1,
                "數值" => 2,
                "布林" => 3,
                "變數" => 4,
                "實體" => 5,
                "物品系統" => 6,
                "向量" => 7,
                "顏色" => 8,
                "陣列" => 9,
                "列表" => 10,
                "列舉" => 11,
                "Unity物件" => 12,
                _ => 99
            };
        }

        /// <summary>
        /// 獲取型別的顯示名稱
        /// </summary>
        private string GetTypeDisplayName(Type type)
        {
            if (type == null) return "unknown";
            
            if (type.IsArray)
                return GetTypeDisplayName(type.GetElementType()) + "[]";
                
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>))
                    return $"List<{GetTypeDisplayName(type.GetGenericArguments()[0])}>";
            }
            
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


        /// <summary>
        /// 獲取選中屬性的型別
        /// </summary>
        public Type GetSelectedPropertyType()
        {
            var selectedName = GetCurrentSelection().FirstOrDefault();
            if (string.IsNullOrEmpty(selectedName))
                return null;
                
            return _propertyTypeMap.TryGetValue(selectedName, out var type) ? type : null;
        }
    }
}