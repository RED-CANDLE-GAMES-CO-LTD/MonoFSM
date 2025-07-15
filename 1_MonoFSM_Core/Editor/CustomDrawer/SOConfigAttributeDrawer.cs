using System.Collections;
using System;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core
{
    [DrawerPriority(0, 1, 0.25)]
    public class SOConfigAttributeDrawer : OdinAttributeDrawer<SOConfigAttribute>
    {
        private void CreateSOForSO()
        {
            //FIXME: case1: 想直接放在對方的旁邊..有需求再改
            var configType = Property.ValueEntry.TypeOfValue;
            var sObj = Property.ParentValues[0] as ScriptableObject;
            var creatorPath = AssetDatabase.GetAssetPath(sObj);

            var folderPath = System.IO.Path.GetDirectoryName(creatorPath).Replace("Assets/", "");
            //remove "Assets/" from path

            var path = folderPath + "/New " + sObj.name + "_" + configType.Name + ".asset";


            var myScriptableObject =
                configType.CreateScriptableObject(path);

            Property.ValueEntry.WeakSmartValue = myScriptableObject;
        }

        private void CreateSOForMonoBehavior()
        {
            var configType = Property.ValueEntry.TypeOfValue;
            var parentComp = Property.ParentValues[0] as Component;

            var path = "";

            if (parentComp)
            {
                var gObj = parentComp.gameObject;
                path = Attribute.GetPathFromOwnerObj(gObj, configType.Name);
            }
            else
            {
                path = Attribute.GetFilePath("0_" + configType.Name + Property.Name);
            }


            // var buttonRect = new Rect(rect.x + rect.width - 100, rect.y, 100, rect.height);


            var myScriptableObject =
                configType.CreateScriptableObject(path);

            Property.ValueEntry.WeakSmartValue = myScriptableObject;


            // Property.ValueEntry.Update();
            // Property.Update();
            //FIXME: 很失敗...? 有call但reference沒有更新
            if (Attribute.PostProcessMethodName != "")
            {
                //call method of parentComp using reflection of parentComp's type
                Debug.Log("PostProcessMethodName: " + Attribute.PostProcessMethodName);
                Debug.Log("parentComp: " + parentComp, parentComp);
                var type = parentComp.GetType();
                var method = type.GetMethod(Attribute.PostProcessMethodName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (method != null)
                    method.Invoke(parentComp, new object[] { });
                else
                    Debug.LogError("PostProcessMethodName not found" + Attribute.PostProcessMethodName);
            }
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            // 檢查是否為 List 類型
            var valueType = Property.ValueEntry.TypeOfValue;
            var isListType = typeof(IList).IsAssignableFrom(valueType);
            
            if (isListType)
            {
                // 對於 List 類型，直接使用預設繪製器
                CallNextDrawer(label);
                return;
            }
            
            // 原有的單一物件檢查
            if ((UnityEngine.Object)Property.ValueEntry.WeakSmartValue != null)
            {
                CallNextDrawer(label);
                return;
            }
            //TODO: 基本上和GameState Drawer差不多，可以共用，inline button結構，但是是先分左右兩區...


            // var controlRect = EditorGUILayout.GetControlRect(label != null);
            // var position = controlRect.AlignRight(40f);
            // var rect1 = controlRect.SetXMax(position.xMin - 5f);
            // rect1.x += rect1.width - 38f;
            // rect1.width = 20f;
            // EditorGUILayout.BeginHorizontal();
            // EditorGUILayout.BeginVertical();
            // BeginDrawCreateSO(rect1);
            CallNextDrawer(label);
            // EndDrawCreateSO(rect1);
            // EditorGUILayout.EndVertical();
            var guiContent = new GUIContent("Create", null, "Create a new ScriptableObject for this field");

            var buttonClicked = SirenixEditorGUI.SDFIconButton(
                EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight),
                guiContent,
                SdfIconType.FileEarmarkSpreadsheet,
                IconAlignment.LeftEdge);

            if (buttonClicked)
            {
                if (Property.ParentValues[0] is ScriptableObject)
                {
                    CreateSOForSO();
                }
                else if (Property.ParentValues[0] is Component)
                {
                    CreateSOForMonoBehavior();
                }
            }


            // EditorGUILayout.EndHorizontal();

            // SirenixEditorGUI.EndInlineBox();

            // SirenixEditorGUI.EndBox();
        }
    }
}