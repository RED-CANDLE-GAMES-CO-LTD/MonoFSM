using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoDebugSetting;
using MonoFSM.Core.DataProvider;
using MonoFSM.Variable;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core.Editor
{
    /// <summary>
    /// 路徑編輯器的基礎類別，提供共用的路徑編輯功能
    /// </summary>
    public abstract class BasePathEditorDrawer<T> : OdinEditor where T : PropertyOfTypeProvider
    {
        

        protected Dictionary<string, List<string>> _memberCache = new();

        /// <summary>
        /// 獲取UseSimplePathEditor的值
        /// </summary>
        protected bool GetUseSimplePathEditor(PropertyOfTypeProvider target)
        {
            if (DebugSetting.IsDebugMode) return false;
            return true;
        }

        /// <summary>
        /// 設定UseSimplePathEditor的值
        /// </summary>
        protected void SetUseSimplePathEditor(PropertyOfTypeProvider target, bool value)
        {
            var field = target.GetType().GetField("UseSimplePathEditor",
                BindingFlags.Public | BindingFlags.Instance);
            field?.SetValue(target, value);
        }

        /// <summary>
        /// 獲取_pathEntries欄位
        /// </summary>
        protected List<FieldPathEntry> GetPathEntries(PropertyOfTypeProvider target)
        {
            var field = target.GetType().GetField("_pathEntries",
                BindingFlags.Public | BindingFlags.Instance);
            return field?.GetValue(target) as List<FieldPathEntry> ?? new List<FieldPathEntry>();
        }

        /// <summary>
        /// 設定_pathEntries欄位
        /// </summary>
        protected void SetPathEntries(PropertyOfTypeProvider target, List<FieldPathEntry> entries)
        {
            var field = target.GetType().GetField("_pathEntries",
                BindingFlags.Public | BindingFlags.Instance);
            field?.SetValue(target, entries);

            // 觸發OnPathEntriesChanged方法
            var method = target.GetType().GetMethod("OnPathEntriesChanged",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(target, null);
        }

        /// <summary>
        /// 繪製簡化的路徑編輯器
        /// </summary>
        protected void DrawSimplifiedPathEditor(PropertyOfTypeProvider target, Type rootType, string noRootTypeMessage = "無法確定根型別")
        {
            var pathEntries = GetPathEntries(target);

            if (rootType == null)
            {
                SirenixEditorGUI.ErrorMessageBox(noRootTypeMessage);
                return;
            }

            // 顯示標題
            EditorGUILayout.LabelField("欄位路徑 (A.B.C 風格)", EditorStyles.boldLabel);

            // 顯示當前路徑
            var currentPath = BuildPathString(pathEntries);
            if (!string.IsNullOrEmpty(currentPath))
            {
                var style = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(0.2f, 0.7f, 0.2f) }, // 綠色文字
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField("當前路徑: " + currentPath, style);
            }
            else
            {
                EditorGUILayout.LabelField("尚未設定路徑", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.Space(5);

            // 繪製路徑編輯器
            var newEntries = DrawPathSegments(pathEntries, rootType, target);

            // 如果路徑有變化，更新
            if (!ArePathEntriesEqual(pathEntries, newEntries))
            {
                Undo.RecordObject(target, "修改欄位路徑");
                SetPathEntries(target, newEntries);
                EditorUtility.SetDirty(target);
            }

            EditorGUILayout.Space(5);

            // 添加操作按鈕
            DrawPathOperationButtons(target, pathEntries);
        }

        /// <summary>
        /// 繪製路徑操作按鈕
        /// </summary>
        protected void DrawPathOperationButtons(PropertyOfTypeProvider target, List<FieldPathEntry> pathEntries)
        {
            if (pathEntries.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();

                // 移除最後一層級按鈕
                if (GUILayout.Button("⬅ 移除最後一層", EditorStyles.miniButton))
                {
                    Undo.RecordObject(target, "移除路徑最後一層");
                    var modifiedEntries = new List<FieldPathEntry>(pathEntries);
                    modifiedEntries.RemoveAt(modifiedEntries.Count - 1);
                    SetPathEntries(target, modifiedEntries);
                    EditorUtility.SetDirty(target);
                }

                // 清空全部按鈕
                var clearButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    normal = { textColor = Color.red }
                };
                if (GUILayout.Button("✕ 清空全部", clearButtonStyle))
                {
                    Undo.RecordObject(target, "清空欄位路徑");
                    SetPathEntries(target, new List<FieldPathEntry>());
                    EditorUtility.SetDirty(target);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 繪製路徑片段
        /// </summary>
        protected List<FieldPathEntry> DrawPathSegments(List<FieldPathEntry> currentEntries, Type rootType, PropertyOfTypeProvider target)
        {
            var currentType = rootType;
            const int maxSegments = 6; // 最多6個層級

            for (var i = 0; i < maxSegments && i < currentEntries.Count; i++)
            {
                if (currentType == null) break;

                var currentSegment = currentEntries[i];

                // 繪製屬性選擇器
                EditorGUILayout.BeginHorizontal();

                // 添加CanBeNull checkbox
                EditorGUI.BeginChangeCheck();
                var canBeNullContent = new GUIContent("", "勾選時允許此層級為null值，不會拋出NullReference異常");
                var newCanBeNull =
                    EditorGUILayout.Toggle(canBeNullContent, currentSegment._canBeNull, GUILayout.Width(20));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "修改CanBeNull設定");
                    currentSegment._canBeNull = newCanBeNull;
                    EditorUtility.SetDirty(target);
                }
                
                EditorGUILayout.LabelField($"層級 {i + 1}:", GUILayout.Width(50));

                // 使用OdinSelector來選擇屬性
                var currentPropertyName = currentSegment._propertyName ?? "";
                var displayText = string.IsNullOrEmpty(currentPropertyName) ? "-- 選擇屬性 --" : currentPropertyName;
                var selectorRect = EditorGUILayout.GetControlRect();

                if (GUI.Button(selectorRect, displayText, EditorStyles.popup))
                    ShowPropertySelector(currentType, currentPropertyName, i, currentEntries, target);

                // 檢查是否為陣列
                if (!string.IsNullOrEmpty(currentPropertyName))
                {
                    var memberType = GetMemberType(currentType, currentPropertyName);
                    if (memberType != null && memberType.IsArray)
                    {
                        // 顯示陣列索引輸入
                        EditorGUILayout.LabelField("[", GUILayout.Width(10));

                        EditorGUI.BeginChangeCheck();
                        var newIndex = EditorGUILayout.IntField(currentSegment.index, GUILayout.Width(30));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(target, "修改陣列索引");
                            currentSegment.index = Math.Max(0, newIndex);
                            EditorUtility.SetDirty(target);
                        }

                        EditorGUILayout.LabelField("]", GUILayout.Width(10));
                        currentType = memberType.GetElementType();
                    }
                    else
                    {
                        currentType = memberType;
                    }
                }
                else
                {
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            // 顯示添加新層級的按鈕
            if (currentEntries.Count < maxSegments && currentType != null && GetAvailableMembers(currentType).Count > 0)
            {
                EditorGUILayout.BeginHorizontal();

                // 新層級的CanBeNull checkbox（預設為false）
                var newLevelCanBeNullContent = new GUIContent("", "新層級的CanBeNull設定（預設為false）");
                EditorGUILayout.Toggle(newLevelCanBeNullContent, false, GUILayout.Width(20));
                EditorGUI.BeginDisabledGroup(true); // 禁用，因為還沒有實際的entry
                EditorGUILayout.LabelField($"層級 {currentEntries.Count + 1}:", GUILayout.Width(50));
                EditorGUI.EndDisabledGroup();

                var addButtonRect = EditorGUILayout.GetControlRect();
                if (GUI.Button(addButtonRect, "+ 添加屬性", EditorStyles.popup))
                    ShowPropertySelector(currentType, "", currentEntries.Count, currentEntries, target);

                EditorGUILayout.EndHorizontal();
            }

            return currentEntries; // 返回原始entries，修改會直接反映在原始列表中
        }

        /// <summary>
        /// 顯示屬性選擇器
        /// </summary>
        protected void ShowPropertySelector(Type currentType, string currentPropertyName, int levelIndex,
            List<FieldPathEntry> currentEntries, PropertyOfTypeProvider target)
        {
            var selector = new FieldPathPropertySelector(currentType, currentPropertyName);
            var rect = GUILayoutUtility.GetLastRect();
            selector.ShowInPopup(rect, 400);

            selector.SelectionConfirmed += selection =>
            {
                var selectedProperty = selection.FirstOrDefault();
                HandlePropertySelection(selectedProperty, levelIndex, currentEntries, currentType, target);
            };
        }

        /// <summary>
        /// 處理屬性選擇
        /// </summary>
        protected void HandlePropertySelection(string selectedProperty, int levelIndex,
            List<FieldPathEntry> currentEntries, Type currentType, PropertyOfTypeProvider target)
        {
            if (string.IsNullOrEmpty(selectedProperty))
                return;

            Undo.RecordObject(target, "修改欄位路徑");

            // 如果是修改現有層級
            if (levelIndex < currentEntries.Count)
            {
                // 修改當前層級
                currentEntries[levelIndex]._propertyName = selectedProperty;
                currentEntries[levelIndex].SetSerializedType(currentType);

                // 移除後續層級（因為型別可能改變了）
                while (currentEntries.Count > levelIndex + 1) currentEntries.RemoveAt(currentEntries.Count - 1);
            }
            else
            {
                // 添加新層級
                var newEntry = new FieldPathEntry();
                newEntry._propertyName = selectedProperty;
                newEntry.SetSerializedType(currentType);
                currentEntries.Add(newEntry);
            }

            // 觸發更新
            SetPathEntries(target, currentEntries);
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// 建構路徑字串
        /// </summary>
        protected string BuildPathString(List<FieldPathEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return "";

            var segments = entries.Select(entry =>
            {
                if (entry.IsArray)
                    return $"{entry._propertyName}[{entry.index}]";
                return entry._propertyName;
            });

            return string.Join(".", segments);
        }

        /// <summary>
        /// 比較兩個路徑列表是否相等
        /// </summary>
        protected bool ArePathEntriesEqual(List<FieldPathEntry> a, List<FieldPathEntry> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            for (var i = 0; i < a.Count; i++)
                if (a[i]._propertyName != b[i]._propertyName ||
                    a[i].index != b[i].index)
                    return false;

            return true;
        }

        /// <summary>
        /// 獲取型別的可用成員
        /// </summary>
        protected List<string> GetAvailableMembers(Type type)
        {
            if (type == null) return new List<string>();

            var key = type.FullName;
            if (_memberCache.ContainsKey(key))
                return _memberCache[key];

            var members = new List<string>();

            // 獲取所有可讀的公共屬性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0) // 排除索引器
                .Select(p => p.Name)
                .Where(name => !string.IsNullOrEmpty(name));
            members.AddRange(properties);

            // 獲取所有公共欄位
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => f.Name)
                .Where(name => !string.IsNullOrEmpty(name));
            members.AddRange(fields);

            // 排序並去重
            members = members.Distinct().OrderBy(m => m).ToList();
            _memberCache[key] = members;

            return members;
        }

        /// <summary>
        /// 獲取成員的型別
        /// </summary>
        protected Type GetMemberType(Type type, string memberName)
        {
            if (type == null || string.IsNullOrEmpty(memberName))
                return null;

            var property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null) return property.PropertyType;

            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.FieldType;

            return null;
        }
    }
}