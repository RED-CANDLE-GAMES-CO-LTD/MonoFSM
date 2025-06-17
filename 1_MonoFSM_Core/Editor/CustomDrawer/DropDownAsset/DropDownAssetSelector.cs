using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.CustomDrawer.DropDownRef
{
    /// <summary>
    /// DropDownAssetSelector
    /// 用Type來過濾ScriptableObject, 回傳path
    /// </summary>
    public class DropDownAssetSelector:OdinSelector<string>
    {
        private string _filterTypeName;
        public DropDownAssetSelector(string filterTypeName)
        {
            // throw new ArgumentNullException(nameof(filterType));
            //scriptable object
            _filterTypeName = filterTypeName ?? "ScriptableObject";
            DrawConfirmSelectionButton = true;
        }
        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Config.DrawSearchToolbar = true;
            tree.Selection.SupportsMultiSelect = false;
            var filterStr = "t:" + _filterTypeName;
            Debug.Log("FilterStr:" + filterStr);
            var assets = AssetDatabase.FindAssets(filterStr);
            foreach (var asset in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                var objName = path.Substring(path.LastIndexOf('/') + 1);
                // var assetObj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                tree.Add(objName, path); //這樣比較便宜？
            }
        }
    }
}