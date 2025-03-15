using UnityEditor.SceneManagement;

namespace RCGMaker.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sirenix.OdinInspector;
    using Sirenix.OdinInspector.Editor;
    using Sirenix.Utilities.Editor;
    using UnityEngine;
    public class DropDownRefCompSelector : OdinSelector<MonoBehaviour>
    {
     
        private Type _filterType;
        Component _forComp;
        Type _parentType;
        public DropDownRefCompSelector(Component forComp, Type filterType, Type parentType = null)
        {
            if(forComp == null)
                throw new ArgumentNullException(nameof(forComp));
            _forComp = forComp;
            _filterType = filterType;
            DrawConfirmSelectionButton = true;
            _parentType = parentType;
            
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Config.DrawSearchToolbar = true;
     
            // tree.Selection.SupportsMultiSelect = this.supportsMultiSelect;
            
            Component[] comps;
            if(_parentType == null)
                _parentType = typeof(IVariableOwner);
            
            //1. prefab裏直接找root下的所有_filterType component
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                var root = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                comps = root.GetComponentsInChildren(_filterType, true);
                
            }
            //2. scene裏找所有 IVariableOwner parent 下的所有_filterType component
            else
                comps =  _forComp.GetComponentsOfSiblingAll(typeof(IVariableOwner),_filterType);
        
            // var types = filterType.FilterSubClassOrImplementationFromDomain();
            foreach (var type in comps)
            {
                tree.Add(type.name+ " (" + type.GetType().Name+")", type);
                // Debug.Log("Add type " + type);
            }

            tree.Config.SelectMenuItemsOnMouseDown = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
            
        }

        [OnInspectorGUI]
        private void DrawInfoAboutSelectedItem() //單點後，額外顯示
        {
            var selected = this.GetCurrentSelection().FirstOrDefault();

            if (selected != null)
            {
                GUILayout.Label("Selected: " + selected.name);
                
                // GUILayout.Label("Data: " + selected.Data);
            }
        }

        //FIXME: 單點選擇後，自動確認選擇...hack code
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