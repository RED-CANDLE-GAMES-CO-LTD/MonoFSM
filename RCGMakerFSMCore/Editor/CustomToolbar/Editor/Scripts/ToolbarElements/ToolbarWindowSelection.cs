using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace UnityToolbarExtender.ToolbarElements
{
    public class MySelector : OdinSelector<Type>
    {
        private readonly List<Type> source;
        private readonly bool supportsMultiSelect;

        public MySelector(List<Type> source, bool supportsMultiSelect)
        {
            this.source = source;
            this.supportsMultiSelect = supportsMultiSelect;
        }

        protected override void BuildSelectionTree(OdinMenuTree tree)
        {
            tree.Config.DrawSearchToolbar = true;
            tree.Selection.SupportsMultiSelect = this.supportsMultiSelect;
            tree.Config.SelectMenuItemsOnMouseDown = true;
            tree.Config.ConfirmSelectionOnDoubleClick = true;

            foreach (var itemType in source)
            {
                tree.Add(itemType.Name, itemType);
            }
            // tree.Add("Defaults/None", null);
            // tree.Add("Defaults/A", new EditorWindow());
            // tree.Add("Defaults/B", new EditorWindow());

            // tree.AddRange(this.source, x => x.Path, x => x.SomeTexture);
        }

        [OnInspectorGUI]
        private void DrawInfoAboutSelectedItem()
        {
            Type selected = this.GetCurrentSelection().FirstOrDefault();

            if (selected != null)
            {
                // GUILayout.Label("Name: " + selected.Name);
                // GUILayout.Label("Data: " + selected.Data);
            }
        }
    }


    //FIXME: 應該要可以把這個抽出去，任何繼承BaseToolbarElement
    [Serializable]
    internal class ToolbarWindowSelection : BaseToolbarElement
    {
        public ToolbarWindowSelection()
        {
        }

        public override string NameInList => "[Button] Tool Window";
        private static GUIContent toolWindowBtn;
        private static GUIContent _pasteLinkBtn;

        public override void Init()
        {
            toolWindowBtn = EditorGUIUtility.IconContent("Settings@2x");
            toolWindowBtn.tooltip = "Open Tool Window";
            _pasteLinkBtn = EditorGUIUtility.IconContent("Clipboard");
            _pasteLinkBtn.tooltip = "Paste link";
            
        }

        protected override void OnDrawInList(Rect position)
        {
            
        }


        protected override void OnDrawInToolbar()
        {
            
            //偷懶了...每個工具都要寫一個BaseToolbarElement很麻煩，直接在這裡寫
            if (GUILayout.Button(_pasteLinkBtn, ToolbarStyles.commandButtonStyle))
            {
            }

            if (GUILayout.Button(toolWindowBtn, ToolbarStyles.commandButtonStyle))
            {
                var types = TypeCache.GetTypesDerivedFrom<IToolbarWindow>().ToList();
                // var types = new List<Type>() { typeof(IToolWindow) };
                MySelector selector = new MySelector(types, false);

                // selector.SetSelection(typeof(IToolbarWindow));

                selector.SelectionCancelled += () =>
                {
                }; // Occurs when the popup window is closed, and no slection was confirmed.
                selector.SelectionChanged += col =>
                {
                    
                };
                selector.SelectionConfirmed += col =>
                {
                    if (col.FirstOrDefault() == null)
                        return;
                    Debug.Log(col.FirstOrDefault());
                    var window = EditorWindow.GetWindow(col.FirstOrDefault());
                    window.Show();
                };

                selector.ShowInPopup(); // Returns the Odin Editor Window ins
                //show popup panel
                // var buttonRect = GUILayoutUtility.GetLastRect();
                //
                // var windowType = TypeCache.GetTypesDerivedFrom<PopupWindowContent>();
                // foreach (var type in windowType)
                // {
                //     if (type.Name == "MainToolWindow")
                //     {
                //         var popupWindow = (PopupWindowContent)Activator.CreateInstance(type);
                //         var size = popupWindow.GetWindowSize();
                //         // window.Show();
                //         var popupPosition = new Rect(buttonRect.xMax + 40, buttonRect.yMin + 20, 0, 0);
                //         PopupWindow.Show(popupPosition, popupWindow);
                //     }
                // }
            }

            //偷懶放label
            var isDebugMode = EditorPrefs.GetBool("DebugSetting.IsDebugMode", false);
            if (isDebugMode)
            {
                
                //green text
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = new Color(0.2f, 0.5f, 0.1f);
                style.fontSize = 12;
                style.alignment = TextAnchor.MiddleCenter;
                // style.padding = new RectOffset(0, 0, 0, 0);
                // style.margin = new RectOffset(0, 0, 0, 0);
                // style.fontStyle = FontStyle.Bold;
                // style.normal.background = null;
                GUILayout.Label("Debug Mode", style);
            }
             
        }
    }
}