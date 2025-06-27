using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core.PlayerEditor
{
    [UsedImplicitly]
    public class ObjectPropertyDrawer : OdinValueDrawer<Object>, IDefinesGenericMenuItems
    {
        // protected override void DrawPropertyLayout(GUIContent label)
        // {
        //     this.
        //     var rect = EditorGUILayout.GetControlRect();
        //     SirenixEditorFields.UnityObjectField(rect, label);
        // }

        protected override void Initialize()
        {
            base.Initialize();
            SkipWhenDrawing = true;
           
        }

        public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu)
        {
            //FIXME: Sprite 沒有辦法做這件事...?
            genericMenu.AddItem(new GUIContent("Paste Reference 貼上引用"), false,
                () => { BugReportUtility.QuickPaste(property); });
        }
    }
}