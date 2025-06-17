using System.Linq;
using MonoFSM.Core.DataProvider;
using MonoFSM.Variable;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core
{
    [DrawerPriority(1, 0, 0)]
    public class VariableProviderDrawer : OdinValueDrawer<IVarTagProperty>
    {
        public bool _foldout = true;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var entry = ValueEntry.SmartValue;
            var property = Property;
            // property.State.Expanded = EditorGUILayout.Foldout(property.State.Expanded, label);

            if (property.State.Expanded)
            {
                // When expanded, draw all child properties as usual.
                this.CallNextDrawer(label);
            }
            else
            {
                // When collapsed, find and draw the specific field.
                // var targetField = property.Children.FirstOrDefault(child => child.Name == "varTag");
                // if (targetField != null)
                // {
                //     // You might want to wrap this in Begin/EndHorizontal if you need extra layout control.
                //     targetField.Draw();
                // }
                // SirenixEditorGUI.BeginInlineBox();
                EditorGUILayout.BeginHorizontal();
                property.State.Expanded = EditorGUILayout.Foldout(property.State.Expanded, label, true);
                // if (entry != null)
                entry.varTag =
                    SirenixEditorFields.UnityObjectField(entry.varTag, typeof(VariableTag), false) as VariableTag;
                EditorGUILayout.EndHorizontal();
                // SirenixEditorGUI.EndInlineBox();
            }

            // _foldout = SirenixEditorGUI.Foldout(_foldout, label);
            // if (_foldout)
            // {
            //     CallNextDrawer(label);
            // }
            // else
            // {

            // }
        }
    }
}