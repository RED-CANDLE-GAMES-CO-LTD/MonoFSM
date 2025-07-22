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
    /// 為PropertyOfTypeProvider類別提供簡化的欄位路徑編輯器
    /// 當UseSimplePathEditor為true時，顯示A.B.C風格的編輯介面
    /// </summary>
    [DrawerPriority(2, 0, 0)]
    [CustomEditor(typeof(PropertyOfTypeProvider), true)]
    public class SimpleFieldPathEditorDrawer : BasePathEditorDrawer<PropertyOfTypeProvider>
    {
        // protected override bool CanDrawValueProperty(InspectorProperty property)
        // {
        //     // return false;
        //     // Debug.Log(
        //     //     $"SimpleFieldPathEditorDrawer: Checking if can draw property {property.Name} of type {property.ParentType.Name}");
        //     if (property.ParentType == typeof(ValueRef))
        //         return false;
        //     return true;
        // }
        //
        // public override bool CanDrawTypeFilter(Type type)
        // {
        //     // Debug.Log($"SimpleFieldPathEditorDrawer: Checking if can draw type {type.Name}");
        //     // 排除 ValueRef 及其子類型
        //     return type != typeof(ValueRef);
        //     return !typeof(ValueRef).IsAssignableFrom(type) && base.CanDrawTypeFilter(type);
        // }


        // protected override void DrawPropertyLayout(GUIContent label)
        // {
        //     var target = ValueEntry.SmartValue;
        //     if (target == null)
        //     {
        //         CallNextDrawer(label);
        //         return;
        //     }
        //
        //     // 如果是 ValueRef 類型，讓其他 drawer 處理
        //     if (target is ValueRef)
        //     {
        //         Debug.Log("SimpleFieldPathEditorDrawer: ValueRef detected, delegating to next drawer.");
        //         CallNextDrawer(label);
        //         return;
        //     }
        //
        //     SirenixEditorGUI.BeginBox();
        //
        //     // 繪製UseSimplePathEditor勾選框
        //     var useSimpleEditor = GetUseSimplePathEditor(target);
        //
        //     // EditorGUI.BeginChangeCheck();
        //     // var newUseSimpleEditor = EditorGUILayout.Toggle("使用簡化路徑編輯器 (A.B.C)", useSimpleEditor);
        //     // if (EditorGUI.EndChangeCheck())
        //     // {
        //     //     Undo.RecordObject(target, "切換路徑編輯器模式");
        //     //     SetUseSimplePathEditor(target, newUseSimpleEditor);
        //     //     EditorUtility.SetDirty(target);
        //     // }
        //     //
        //     // EditorGUILayout.Space(3);
        //
        //     if (useSimpleEditor)
        //     {
        //         DrawSimplifiedEditor(target, label);
        //     }
        //     else
        //     {
        //         // 繪製原始的詳細編輯器，但不包含最外層的Box（避免雙重boxing）
        //         SirenixEditorGUI.EndBox();
        //         CallNextDrawer(label);
        //         return;
        //     }
        //
        //     SirenixEditorGUI.EndBox();
        // }
        protected override void DrawTree()
        {
            // public override void OnInspectorGUI()
            // {
            // base.OnInspectorGUI();
            var target = (PropertyOfTypeProvider)serializedObject.targetObject;
            if (target == null)
            {
                EditorGUILayout.HelpBox("目標物件為空，無法繪製編輯器。", MessageType.Warning);
                return;
            }

            // 如果是 ValueRef 類型，讓其他 drawer 處理
            if (target is ValueRef)
            {
                // Debug.Log("SimpleFieldPathEditorDrawer: ValueRef detected, delegating to next drawer.");
                // CallNextDrawer();
                return;
            }

            SirenixEditorGUI.BeginBox();

            // 繪製UseSimplePathEditor勾選框
            var useSimpleEditor = GetUseSimplePathEditor(target);


            if (useSimpleEditor)
            {
                DrawSimplifiedEditor(target);
            }
            else
            {
                // 繪製原始的詳細編輯器，但不包含最外層的Box（避免雙重boxing）
                SirenixEditorGUI.EndBox();
                // CallNextDrawer(label);
                base.DrawTree();
                return;
            }
            
            SirenixEditorGUI.EndBox();
            
        }

        /// <summary>
        /// 繪製簡化編輯器（包含varTag和fieldPath）
        /// </summary>
        private void DrawSimplifiedEditor(PropertyOfTypeProvider target)
        {
            // 顯示Root Object資訊
            DrawRootObjectInfo(target);

            EditorGUILayout.Space(5);

            // 只有在有varTag欄位時才顯示varTag選擇器
            if (HasVarTagField(target))
            {
                DrawVarTagSelector(target);
                EditorGUILayout.Space(5);
            }

            // 繪製fieldPath編輯器
            DrawSimplifiedPathEditor(target, target.GetObjectType);
        }

        /// <summary>
        /// 繪製varTag選擇器
        /// </summary>
        private void DrawVarTagSelector(PropertyOfTypeProvider target)
        {
            EditorGUILayout.LabelField("變數標籤選擇", EditorStyles.boldLabel);

            var currentVarTag = GetVarTag(target);
            var displayText = currentVarTag != null ? currentVarTag.name : "-- 選擇變數 --";

            var selectorRect = EditorGUILayout.GetControlRect();
            if (GUI.Button(selectorRect, displayText, EditorStyles.popup))
                ShowVarTagSelector(target, currentVarTag, selectorRect);

            // 顯示當前變數的型別資訊
            if (currentVarTag != null)
            {
                var style = new GUIStyle(EditorStyles.helpBox)
                {
                    normal = { textColor = new Color(0.2f, 0.2f, 0.7f) }, // 藍色文字
                    fontStyle = FontStyle.Italic
                };
                EditorGUILayout.LabelField($"變數型別: {currentVarTag.ValueType?.Name ?? "未知"}", style);
            }
        }

        /// <summary>
        /// 獲取當前的VariableTag
        /// </summary>
        private VariableTag GetVarTag(PropertyOfTypeProvider target)
        {
            var field = target.GetType().GetField("_varTag",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(target) as VariableTag;
        }

        /// <summary>
        /// 設定VariableTag
        /// </summary>
        private void SetVarTag(PropertyOfTypeProvider target, VariableTag varTag)
        {
            var field = target.GetType().GetField("_varTag",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(target, varTag);
        }

        /// <summary>
        /// 顯示varTag選擇器
        /// </summary>
        private void ShowVarTagSelector(PropertyOfTypeProvider target, VariableTag currentVarTag, Rect rect)
        {
            // 創建varTag的選擇器
            var selector = new VarTagPropertySelector(target, currentVarTag);
            selector.ShowInPopup(rect, 400);

            selector.SelectionConfirmed += selection =>
            {
                var selectedVarTag = selection.FirstOrDefault();
                if (selectedVarTag != null || selection.Any()) // 允許選擇null來清空
                {
                    Undo.RecordObject(target, "修改變數標籤");
                    SetVarTag(target, selectedVarTag);

                    // 當變數改變時，清空fieldPath（因為型別可能不同）
                    SetPathEntries(target, new List<FieldPathEntry>());

                    EditorUtility.SetDirty(target);
                }
            };
        }


        /// <summary>
        /// 檢查目標物件是否有varTag欄位
        /// </summary>
        private bool HasVarTagField(PropertyOfTypeProvider target)
        {
            var field = target.GetType().GetField("_varTag",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return field != null;
        }

        /// <summary>
        /// 顯示GetObjectType資訊
        /// </summary>
        private void DrawRootObjectInfo(PropertyOfTypeProvider target)
        {
            EditorGUILayout.LabelField("起始型別資訊", EditorStyles.boldLabel);

            var objectType = target.GetObjectType;
            var displayInfo = "未知型別";

            // 顯示GetObjectType的型別資訊
            try
            {
                if (objectType != null)
                {
                    if (HasVarTagField(target))
                    {
                        var varTag = GetVarTag(target);
                        if (varTag != null)
                        {
                            // 有選擇varTag時，顯示變數資訊
                            displayInfo = $"{varTag.name} (型別: {objectType.Name})";
                        }
                        else
                        {
                            // 沒有選擇varTag時，顯示Entity型別
                            var entityName = GetEntityName(target);
                            displayInfo = entityName != null
                                ? $"{entityName} (型別: {objectType.Name})"
                                : $"Entity (型別: {objectType.Name})";
                        }
                    }
                    else
                    {
                        // 沒有varTag欄位時，直接顯示GetObjectType
                        displayInfo = $"物件型別: {objectType.Name}";
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"無法獲取GetObjectType資訊: {e.Message}");
                displayInfo = objectType?.Name ?? "獲取失敗";
            }

            var style = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = new Color(0.1f, 0.5f, 0.8f) }, // 藍色文字
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField($"來源: {displayInfo}", style);
        }

        /// <summary>
        /// 獲取Entity名稱
        /// </summary>
        private string GetEntityName(PropertyOfTypeProvider target)
        {
            try
            {
                //FIXME: 需要這個嗎？
                // 嘗試從其他可能的欄位獲取
                var entityProviderField = target.GetType().GetField("_monoEntityProvider",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var entityProvider = entityProviderField?.GetValue(target);
                if (entityProvider != null) return entityProvider.GetType().Name;

                // 嘗試從ParentEntity獲取名稱
                var parentEntityField = target.GetType().GetField("_parentEntity",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (parentEntityField?.GetValue(target) is MonoBehaviour parentEntity) return parentEntity.name;

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}