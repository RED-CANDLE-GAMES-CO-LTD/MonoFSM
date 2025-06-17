using System;
using System.Linq;
using JetBrains.Annotations;
using MonoFSM.Editor.CustomDrawer.DropDownRef;
using MonoFSM.Runtime.Attributes;
using MonoFSM.Core;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Editor.CustomDrawer
{
    [UsedImplicitly]
    [DrawerPriority(0.0, 2.0, 0.25)]
    public class DropDownAssetDrawer:OdinAttributeDrawer<DropDownAssetAttribute>
    {
        private string error;
        private ValueResolver<object> rawGetter;
        private Func<Type> getter;
        protected override void Initialize()
        {
            base.Initialize();
            //想要從使用Attribute的那個class來找到某個function
            this.rawGetter = ValueResolver.Get<object>(this.Property, this.Attribute.FilterGetter);
            this.error = this.rawGetter.ErrorMessage;
            getter = () =>
            {
                object source = this.rawGetter.GetValue();
                return source as Type;
            };
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            SirenixEditorGUI.BeginBox();
            var widthRatio = 1f; //0.75f
            // var option = GUILayout.Width(EditorGUIUtility.currentViewWidth * widthRatio);
            // CallNextDrawer(label);
            // SirenixEditorGUI.BeginInlineBox();
            // AppendNextDrawer();
            GUILayout.BeginHorizontal();
            // CallNextDrawer(label);
            if(label != null)
                GUILayout.Label(label, GUILayout.Width(EditorGUIUtility.labelWidth * widthRatio));
            // ShowSelector();
            
            {
                GUI.backgroundColor = Property.ValueEntry.WeakSmartValue == null
                    ? new Color(0.2f, 0.2f, 0.3f, 0.1f)
                    : new Color(0.35f, 0.3f, 0.1f, 0.2f);

       
                Property.ValueEntry.WeakSmartValue = SirenixEditorFields.UnityObjectField(Property.ValueEntry.WeakSmartValue as UnityEngine.Object,
                    getter(), false); //GUILayout.Width(EditorGUIUtility.currentViewWidth) 這個會太肥噴掉
                GUI.backgroundColor = Color.white;
            }
       
            // }
            SirenixEditorGUI.EndBox();
            GUILayout.EndHorizontal();
        }
        
        void ShowSelector()
        {
            
            // var type = Property.ValueEntry.BaseValueType; //FIXME: Base Type?
            // if (type.IsArray)
            // {
            //     type = type.GetElementType();
            // }

            var currentComp = Property.ValueEntry.WeakSmartValue as Object;
        
            var buttonText = currentComp ? currentComp.name : "None";
            if(SirenixEditorGUI.SDFIconButton(buttonText,16,SdfIconType.CaretDownFill,IconAlignment.RightEdge))
                // if (GUILayout.Button())
            {
                // Debug.Log("Parent Value:" + baseMemberProperty.ParentValues[0]);
                // Debug.Log("BindComp:" + _bindComp+"type"+type);
                //FIXME: 指定，或是fallback?
                var selector = new DropDownAssetSelector(getter().Name);
                // selector.EnableSingleClickTselector.EnableSingleClickToConfirm();oSelect();
                // selector.SelectionChanged += col => { Debug.Log("SelectionChanged" + col.FirstOrDefault()); };
            
                selector.SelectionChanged += col =>
                {
                    selector.SelectionTree.Selection.ConfirmSelection();
                    // Debug.Log(col);
       
                    // var type = col.FirstOrDefault();
                    //FIXME: 應該晚點再load object?
                    var path = col.FirstOrDefault();
                    var assetObj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    Property.ValueEntry.WeakSmartValue = assetObj;
                    // selector.pop
                };

                // selector.EnableSingleClickToConfirm();
                selector.ShowInPopup();
            }
        }
    }
}