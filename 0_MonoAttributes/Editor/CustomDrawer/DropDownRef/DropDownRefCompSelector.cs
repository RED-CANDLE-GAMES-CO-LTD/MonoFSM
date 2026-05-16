using System;
using System.Linq;
using MonoFSM.CustomAttributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MonoFSM.Core
{
    //FIXME: 如果也有帶ValueTypeValidate，可以過濾更細
    public class DropDownRefCompSelector : OdinSelector<Component>
    {
        private Type _filterType;
        private Component _forComp;

        // private Type _parentType;
        DropDownRefAttribute _attribute;

        public DropDownRefCompSelector(
            Component forComp,
            Type filterType,
            DropDownRefAttribute attribute
        )
        {
            if (forComp == null)
                throw new ArgumentNullException(nameof(forComp));
            _forComp = forComp;
            _filterType = filterType;
            DrawConfirmSelectionButton = true;
            _attribute = attribute;
            // _parentType = _attribute._parentType;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            ComponentDropdownTreeUtility.Build(
                tree,
                _forComp,
                _filterType,
                _attribute._parentType,
                _attribute._findFromParentTransform);
        }

        [OnInspectorGUI]
        private void DrawInfoAboutSelectedItem() //單點後，額外顯示
        {
            var selected = GetCurrentSelection().FirstOrDefault();

            if (selected != null)
                GUILayout.Label("Selected: " + selected.name);
            // GUILayout.Label("Data: " + selected.Data);
        }

        //FIXME: 單點選擇後���自動確認選擇...hack code
        public void EnableSingleClickToConfirm()
        {
            SelectionTree.EnumerateTree(x =>
            {
                x.OnDrawItem -= EnableSingleClickToConfirm;
                x.OnDrawItem += EnableSingleClickToConfirm;
            });
        }

        private void EnableSingleClickToConfirm(OdinMenuItem obj)
        {
            var type = Event.current.type;
            if (type == EventType.Layout || !obj.Rect.Contains(Event.current.mousePosition))
                return;
            GUIHelper.RequestRepaint();

            // if (Event.current.type == UnityEngine.EventType.MouseDrag && obj is T && this.IsValidSelection(Enumerable.Repeat<T>((T) obj.Value, 1)))
            //     obj.Select();
            if (type != EventType.MouseUp || obj.ChildMenuItems.Count != 0)
                return;
            obj.Select();
            // Debug.Log("ConfirmSelection" + obj.Name);
            obj.MenuTree.Selection.ConfirmSelection();

            Event.current.Use();
        }
    }
}


// {
//         // private readonly List<AbstractStateAction> source;
//         private readonly bool supportsMultiSelect;
//
//         public StateActionSelector(Type baseType, bool supportsMultiSelect)
//         {
//             // this.source = source;
//             this.supportsMultiSelect = supportsMultiSelect;
//         }
//
//         protected override void BuildSelectionTree(OdinMenuTree tree)
//         {
//             tree.Config.DrawSearchToolbar = true;
//             tree.Selection.SupportsMultiSelect = this.supportsMultiSelect;
//
//             var types = typeof(AbstractStateAction).FilterSubClassFromDomain();
//             foreach (var type in types)
//             {
//                 tree.Add(type.Name, type);
//             }
//             // tree.Add("Defaults/A", new AbstractStateAction());
//             // tree.Add("Defaults/B", new AbstractStateAction());
//
//             // tree.AddRange(this.source, x => x.Path, x => x.SomeTexture);
//         }
//
//         [OnInspectorGUI]
//         private void DrawInfoAboutSelectedItem()
//         {
//             Type selected = this.GetCurrentSelection().FirstOrDefault();
//
//             if (selected != null)
//             {
//                 GUILayout.Label("Name: " + selected.Name);
//                 // GUILayout.Label("Data: " + selected.Data);
//             }
//         }
//     }
// }
