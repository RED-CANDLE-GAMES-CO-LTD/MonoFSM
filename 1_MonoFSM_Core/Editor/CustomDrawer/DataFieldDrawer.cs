using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace MonoFSM.Core
{
    //[]: 失敗了
    //從GameData中撈出fields來選
    public class DataFieldDrawer : OdinValueDrawer<FlagFieldEntryString>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var flag = Property.FindChild(childprop => childprop.Name == "flagName", false);
            var flagEntry = flag.ValueEntry.WeakSmartValue as FlagFieldEntryString;
//[]: 會有dirty問題？
            if (GUILayout.Button("選擇flag"))
            {
                //show drop down for all fields name to select
                var selector = new FieldSelector(flagEntry);
                selector.SelectionConfirmed += col =>
                {
                    var fieldName = col.FirstOrDefault();
                    flagEntry.fieldName = fieldName;
                    flag.ValueEntry.WeakSmartValue = flagEntry;
                };
                selector.ShowInPopup();
            }
        }
    }

    public class FieldSelector : OdinSelector<string>
    {
        private FlagFieldEntryString fieldEntry;

        public FieldSelector(FlagFieldEntryString fieldEntry)
        {
            this.fieldEntry = fieldEntry;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Config.DrawSearchToolbar = true;

            // tree.Selection.SupportsMultiSelect = this.supportsMultiSelect;

            var terms = fieldEntry.flagBase.GetType().GetFields();

            foreach (var term in terms) tree.Add(term.Name, term);

            tree.Config.SelectMenuItemsOnMouseDown = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;
        }
    }
}