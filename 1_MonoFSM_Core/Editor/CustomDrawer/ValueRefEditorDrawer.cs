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
    [CustomEditor(typeof(ValueRef), true)]
    [DrawerPriority(3, 0, 0)] // 優先於SimpleFieldPathEditorDrawer
    public class ValueRefEditorDrawer : BasePathEditorDrawer<ValueRef>
    {
        private ValueRef _target;

        protected override void DrawTree()
        {
            //FIXME: 好像不該這樣弄，還是ValueDrawer比較好？針對path處理就好
            Tree.BeginDraw(true);
            _target = serializedObject.targetObject as ValueRef;
            if (DebugSetting.IsDebugMode)
            {
                // 繪製原始的Inspector內容
                base.DrawTree();
            }

            else
            {
                SirenixEditorGUI.BeginBox();
                // 繪製簡化編輯器
                DrawSimplifiedEditor(_target);
                SirenixEditorGUI.EndBox();    
            }

            Tree.EndDraw();
            
        }
        // protected override void DrawPropertyLayout(GUIContent label)
        // {
        //     var target = ValueEntry.SmartValue;
        //     if (target == null)
        //     {
        //         CallNextDrawer(label);
        //         return;
        //     }
        //
        //     SirenixEditorGUI.BeginBox();
        //
        //     // 繪製UseSimplePathEditor勾選框
        //     var useSimpleEditor = GetUseSimplePathEditor(target);
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

        /// <summary>
        /// 繪製簡化編輯器（包含valueProvider和fieldPath）
        /// </summary>
        private void DrawSimplifiedEditor(ValueRef valueRef, GUIContent _ = null)
        {
            // 顯示ValueProvider資訊
            DrawValueProviderInfo(valueRef);

            EditorGUILayout.Space(5);

            // 繪製ValueProvider選擇器
            DrawValueProviderSelector(valueRef);

            EditorGUILayout.Space(5);

            // 繪製fieldPath編輯器
            var valueProvider = GetValueProvider(valueRef);
            if (valueProvider != null)
                DrawSimplifiedPathEditor(valueRef, valueProvider.ValueType, "請先選擇數值提供者");
            else
                SirenixEditorGUI.ErrorMessageBox("請先選擇數值提供者");
        }

        /// <summary>
        /// 顯示ValueProvider資訊
        /// </summary>
        private void DrawValueProviderInfo(ValueRef valueRef)
        {
            EditorGUILayout.LabelField("起始型別資訊", EditorStyles.boldLabel);

            var valueProvider = GetValueProvider(valueRef);
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
            var targetProperty = Tree.RootProperty;
            var valueProviderProperty = targetProperty.Children.FirstOrDefault(p => p.Name == "_valueProvider");
            if (valueProviderProperty != null)
            {
                // EditorGUI.BeginChangeCheck();
                valueProviderProperty.Draw();
                // if (EditorGUI.EndChangeCheck())
                // {
                //     // 強制重繪當整個 Inspector 當值改變時
                //     Tree.UpdateTree();
                //     GUIHelper.RequestRepaint();
                // }
            }
            else
                EditorGUILayout.HelpBox("無法找到_valueProvider欄位", MessageType.Warning);
        }


        /// <summary>
        /// 獲取ValueProvider
        /// </summary>
        private PropertyOfTypeProvider GetValueProvider(ValueRef valueRef)
        {
            var field = valueRef.GetType().GetField("_valueProvider",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(valueRef) as PropertyOfTypeProvider;
        }
    }
}