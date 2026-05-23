using System;
using System.Collections.Generic;
using System.Linq;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core.Editor.PropertyDrawer
{
    /// <summary>
    /// 當 [CompRef] 欄位的元素型別是 AbstractConditionBehaviour 時，
    /// 在欄位上方顯示「常用 Condition」按鈕列，一鍵新增子物件並執行 preset 設值。
    /// 較高的 DrawerPriority 讓本 drawer 在 ComponentAttributeDrawer 之前畫。
    /// </summary>
    [DrawerPriority(2, 100, 0)]
    [AllowGUIEnabledForReadonly]
    public class CompRefConditionPresetDrawer : OdinAttributeDrawer<CompRefAttribute>
    {
        private bool _isConditionArray;
        private Type _elementType;
        private MonoBehaviour _bindComp;
        private List<ConditionPresetRegistry.Entry> _entries;

        protected override void Initialize()
        {
            var valueEntry = Property.ValueEntry;
            if (valueEntry == null) return;

            var type = valueEntry.TypeOfValue;
            if (!type.IsArray) return;

            var element = type.GetElementType();
            if (element == null) return;
            if (!typeof(AbstractConditionBehaviour).IsAssignableFrom(element)) return;

            _isConditionArray = true;
            _elementType = element;
            _entries = ConditionPresetRegistry.ForElementType(element).ToList();

            // 直接 parent 可能是 ConditionGroup 等非 MonoBehaviour 的 Serializable，
            // 要往上找到最近的 MonoBehaviour（與 ComponentAttributeDrawer 一致）
            var parents = Property.ParentValues;
            if (parents != null && parents.Count > 0)
                _bindComp = parents[0] as MonoBehaviour;

            if (_bindComp == null)
            {
                var mbProperty = Property.FindParent(
                    x => x.ParentType != null && x.ParentType.IsSubclassOf(typeof(MonoBehaviour)),
                    true);
                if (mbProperty != null && mbProperty.ParentValues.Count > 0)
                    _bindComp = mbProperty.ParentValues[0] as MonoBehaviour;
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (_isConditionArray && _entries != null && _entries.Count > 0 && _bindComp != null)
            {
                var prevEnabled = GUI.enabled;
                if (!prevEnabled) GUI.enabled = true;
                DrawPresetBar();
                GUI.enabled = prevEnabled;
            }

            CallNextDrawer(label);
        }

        private void DrawPresetBar()
        {
            const int maxInline = 5;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(GUIContent.none, GUILayout.Width(2));

            var inlineCount = Mathf.Min(maxInline, _entries.Count);
            for (var i = 0; i < inlineCount; i++)
                DrawPresetButton(_entries[i]);

            if (_entries.Count > maxInline)
                DrawMoreMenu(_entries.Skip(maxInline));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPresetButton(ConditionPresetRegistry.Entry e)
        {
            var prev = GUI.backgroundColor;
            if (!string.IsNullOrEmpty(e.ColorHex) &&
                ColorUtility.TryParseHtmlString(e.ColorHex, out var c))
                GUI.backgroundColor = c;

            var label = string.IsNullOrEmpty(e.Category)
                ? "+ " + e.DisplayName
                : "+ " + e.DisplayName;
            if (GUILayout.Button(new GUIContent(label, e.ConditionType.Name),
                    GUILayout.Height(20)))
                ApplyPreset(e);

            GUI.backgroundColor = prev;
        }

        private void DrawMoreMenu(IEnumerable<ConditionPresetRegistry.Entry> rest)
        {
            if (!GUILayout.Button(new GUIContent("⋯", "更多 Preset"),
                    GUILayout.Width(28), GUILayout.Height(20)))
                return;

            var menu = new GenericMenu();
            foreach (var e in rest)
            {
                var captured = e;
                var path = string.IsNullOrEmpty(e.Category)
                    ? e.DisplayName
                    : e.Category + "/" + e.DisplayName;
                menu.AddItem(new GUIContent(path), false, () => ApplyPreset(captured));
            }

            menu.ShowAsContext();
        }

        private void ApplyPreset(ConditionPresetRegistry.Entry e)
        {
            if (_bindComp == null) return;

            // 一律加在 child 物件上（符合 AutoChildren 慣例）
            var go = new GameObject(e.ConditionType.Name);
            Undo.RegisterCreatedObjectUndo(go, "Add Condition Preset");
            go.transform.SetParent(_bindComp.transform, false);

            var comp = Undo.AddComponent(go, e.ConditionType);
            if (comp == null)
            {
                Debug.LogError($"無法新增 Component: {e.ConditionType.Name}", _bindComp);
                return;
            }

            try
            {
                Undo.RecordObject(comp, "Apply Condition Preset");
                e.Setup.Invoke(null, new object[] { comp });
                EditorUtility.SetDirty(comp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Preset 執行失敗: {ex}", comp);
            }

            Selection.activeGameObject = go;
            GUIUtility.ExitGUI();
        }
    }
}
