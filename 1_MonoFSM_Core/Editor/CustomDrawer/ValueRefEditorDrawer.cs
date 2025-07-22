using System;
using System.Linq;
using System.Reflection;
using MonoDebugSetting;
using MonoFSM.Core.DataProvider;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core.Editor
{
    /// <summary>
    /// 為ValueRef類別提供特化的簡化編輯器
    /// 支援_valueProvider選擇和欄位路徑編輯
    /// </summary>
    [DrawerPriority(3, 0, 0)] // 優先於SimpleFieldPathEditorDrawer
    public class ValueRefEditorDrawer : BasePathEditorDrawer<ValueRef>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var target = ValueEntry.SmartValue;
            if (target == null)
            {
                CallNextDrawer(label);
                return;
            }

            SirenixEditorGUI.BeginBox();

            // 繪製UseSimplePathEditor勾選框
            var useSimpleEditor = GetUseSimplePathEditor(target);

            // EditorGUI.BeginChangeCheck();
            // var newUseSimpleEditor = EditorGUILayout.Toggle("使用簡化路徑編輯器 (A.B.C)", useSimpleEditor);
            // if (EditorGUI.EndChangeCheck())
            // {
            //     Undo.RecordObject(target, "切換路徑編輯器模式");
            //     SetUseSimplePathEditor(target, newUseSimpleEditor);
            //     EditorUtility.SetDirty(target);
            // }
            //
            // EditorGUILayout.Space(3);

            if (useSimpleEditor)
            {
                DrawSimplifiedEditor(target, label);
            }
            else
            {
                // 繪製原始的詳細編輯器，但不包含最外層的Box（避免雙重boxing）
                SirenixEditorGUI.EndBox();
                CallNextDrawer(label);
                return;
            }

            SirenixEditorGUI.EndBox();
        }

        /// <summary>
        /// 繪製簡化編輯器（包含valueProvider和fieldPath）
        /// </summary>
        private void DrawSimplifiedEditor(ValueRef target, GUIContent _ = null)
        {
            // 顯示ValueProvider資訊
            DrawValueProviderInfo(target);

            EditorGUILayout.Space(5);

            // 繪製ValueProvider選擇器
            DrawValueProviderSelector(target);

            EditorGUILayout.Space(5);

            // 繪製fieldPath編輯器
            var valueProvider = GetValueProvider(target);
            if (valueProvider != null)
                DrawSimplifiedPathEditor(target, valueProvider.ValueType, "請先選擇數值提供者");
            else
                SirenixEditorGUI.ErrorMessageBox("請先選擇數值提供者");
        }

        /// <summary>
        /// 顯示ValueProvider資訊
        /// </summary>
        private void DrawValueProviderInfo(ValueRef target)
        {
            EditorGUILayout.LabelField("起始型別資訊", EditorStyles.boldLabel);

            var valueProvider = GetValueProvider(target);
            var displayInfo = "未選擇來源";

            try
            {
                if (valueProvider != null)
                {
                    var providerType = valueProvider.ValueType;
                    var description = valueProvider.Description;

                    displayInfo = !string.IsNullOrEmpty(description)
                        ? $"{description} (型別: {providerType?.Name ?? "未知"})"
                        : $"{valueProvider.GetType().Name} (型別: {providerType?.Name ?? "未知"})";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"無法獲取ValueProvider資訊: {e.Message}");
                displayInfo = "獲取失敗";
            }

            var style = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { textColor = new Color(0.1f, 0.5f, 0.8f) }, // 藍色文字
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField($"來源: {displayInfo}", style);
        }

        /// <summary>
        /// 繪製ValueProvider選擇器（使用原始的DropDownRef邏輯）
        /// </summary>
        private void DrawValueProviderSelector(ValueRef _)
        {
            EditorGUILayout.LabelField("數值提供者選擇", EditorStyles.boldLabel);

            // 在OdinValueDrawer中，我們需要直接調用下一個drawer來處理DropDownRef
            // 找到_valueProvider欄位並讓它使用原本的繪製邏輯
            var targetProperty = ValueEntry.Property;
            var valueProviderProperty = targetProperty.Children.FirstOrDefault(p => p.Name == "_valueProvider");
            // var dropdownAttr = valueProviderProperty.GetAttribute<DropDownRefAttribute>();
            // valueProviderProperty.PushDraw();
            // Debug.Log("Is dropdownRef: " + (dropdownAttr != null));
            // DropDownRefAttributeDrawer

            if (valueProviderProperty != null)
                // valueProviderProperty.IncrementDrawerChainIndex();
                // valueProviderProperty.IncrementDrawerChainIndex();
                // EditorGUILayout.HelpBox(valueProviderProperty.DrawerChainIndex.ToString(), MessageType.Info);
                valueProviderProperty.Draw();

            else
                EditorGUILayout.HelpBox("無法找到_valueProvider欄位", MessageType.Warning);
            // 直接繪製_valueProvider欄位，讓DropDownRef attribute處理

            //     // 使用 InlineProperty 來限制繪製範圍
            //     using (var scope = valueProviderProperty.ValueEntry)
            //     {
            //         scope.Property.Draw();
            //     }
            // else
            //     EditorGUILayout.HelpBox("無法找到_valueProvider欄位", MessageType.Warning);

            // else
            // EditorGUILayout.HelpBox("無法找到_valueProvider欄位", MessageType.Warning);
        }


        /// <summary>
        /// 獲取ValueProvider
        /// </summary>
        private PropertyOfTypeProvider GetValueProvider(ValueRef target)
        {
            var field = target.GetType().GetField("_valueProvider",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(target) as PropertyOfTypeProvider;
        }
    }
}