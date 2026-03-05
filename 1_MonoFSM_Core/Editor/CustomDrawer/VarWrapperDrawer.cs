#if UNITY_EDITOR
using MonoFSM.Variable;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Variable.Editor
{
    public class VarWrapperDrawer : OdinValueDrawer<AbstractVarWrapper>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var varProp = Property.Children["_var"];
            var tempProp = Property.Children["_tempValue"];
            var hasVar = varProp?.ValueEntry?.WeakSmartValue != null;

            var rect = EditorGUILayout.GetControlRect();

            var foldoutRect = rect;
            foldoutRect.width = EditorGUIUtility.labelWidth;
            Property.State.Expanded = SirenixEditorGUI.Foldout(foldoutRect, Property.State.Expanded, label ?? GUIContent.none);

            if (!Property.State.Expanded)
            {
                var valueRect = rect;
                valueRect.xMin = EditorGUIUtility.labelWidth + 4f;

                EditorGUI.BeginChangeCheck();

                if (hasVar)
                {
                    var obj = varProp.ValueEntry.WeakSmartValue as Object;
                    var newObj = EditorGUI.ObjectField(valueRect, obj, varProp.ValueEntry.TypeOfValue, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        varProp.ValueEntry.WeakSmartValue = newObj;
                    }
                }
                else if (tempProp != null)
                {
                    DrawInlineValue(valueRect, tempProp);
                }
            }

            if (Property.State.Expanded)
            {
                EditorGUI.indentLevel++;
                foreach (var child in Property.Children)
                {
                    child.Draw();
                }
                EditorGUI.indentLevel--;
            }
        }

        private static void DrawInlineValue(Rect rect, InspectorProperty prop)
        {
            var type = prop.ValueEntry.TypeOfValue;
            EditorGUI.BeginChangeCheck();

            if (type == typeof(float))
            {
                var val = (float)prop.ValueEntry.WeakSmartValue;
                val = EditorGUI.FloatField(rect, val);
                if (EditorGUI.EndChangeCheck()) prop.ValueEntry.WeakSmartValue = val;
            }
            else if (type == typeof(int))
            {
                var val = (int)prop.ValueEntry.WeakSmartValue;
                val = EditorGUI.IntField(rect, val);
                if (EditorGUI.EndChangeCheck()) prop.ValueEntry.WeakSmartValue = val;
            }
            else if (type == typeof(bool))
            {
                var val = (bool)prop.ValueEntry.WeakSmartValue;
                val = EditorGUI.Toggle(rect, val);
                if (EditorGUI.EndChangeCheck()) prop.ValueEntry.WeakSmartValue = val;
            }
            else if (type == typeof(Vector3))
            {
                var val = (Vector3)prop.ValueEntry.WeakSmartValue;
                val = EditorGUI.Vector3Field(rect, GUIContent.none, val);
                if (EditorGUI.EndChangeCheck()) prop.ValueEntry.WeakSmartValue = val;
            }
            else
            {
                EditorGUI.EndChangeCheck();
                GUI.enabled = false;
                EditorGUI.LabelField(rect, prop.ValueEntry.WeakSmartValue?.ToString() ?? "null");
                GUI.enabled = true;
            }
        }
    }
}
#endif
