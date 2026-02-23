using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core.Attributes;
using MonoFSM.CustomAttributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
// using MonoFSM.Runtime.Mono;
using Object = UnityEngine.Object;

namespace MonoFSM.Core
{
    [DrawerPriority(0, 1, 0.25)]
    public class SOConfigAttributeDrawer : OdinAttributeDrawer<SOConfigAttribute>
    {
        private static Type GetCollectionElementType(Type collectionType)
        {
            return collectionType.GetGenericArguments()[0];
        }
        /// <summary>
        /// 取得可用的路徑選項
        /// </summary>
        private List<string> GetAvailablePaths()
        {
            var paths = new List<string> { "Assets" };
            var localPackages = PackageHelper.GetLocalPackagePaths();
            paths.AddRange(localPackages);
            return paths;
        }

        /// <summary>
        /// 產生統一格式的檔案名稱
        /// </summary>
        private string GenerateFileName(string fileName)
        {
            //FIXME: vartag的話應該用原本變數的名字, 但名字誰先誰後？
            var parentObject = Property.ParentValues[0];
            var propertyName = Property.Name;
            // var actualType = GetActualConfigType(configType);
            // var typeOfValue = Property.ValueEntry.TypeOfValue; //這是用property type, 而不是實際想要的type
            var prefix = "d"; //scriptableObject 前綴
            //FIXME: 用interface做？
            // if (configType == typeof(VariableTag))
            // {
            //     prefix = "v";
            // }
            // else if (configType == typeof(MonoEntityTag))
            // {
            //     prefix = "E";
            // }

            var postfix = fileName;

            // if (parentObject is ScriptableObject sObj)
            // {
            //     postfix = sObj.name;
            //     // return $"[{configType.Name}]_{sObj.name}";
            // }
            // else if (parentObject is Component parentComp)
            // {
            //     if (parentComp)
            //     {
            //         var gObj = parentComp.gameObject;
            //         postfix = gObj.name;
            //         // return $"[{configType.Name}]_{gObj.name}";
            //     }
            //     else
            //     {
            //         postfix = Property.Name;
            //         // return $"[{configType.Name}]_0_{Property.Name}";
            //     }
            // }
            // else
            // {
            //
            //     // return $"[{configType.Name}]_Unknown";
            // }
            return $"{prefix}_{postfix}";
        }

        /// <summary>
        /// 使用路徑配置建立 ScriptableObject
        /// </summary>
        private ScriptableObject CreateScriptableObjectWithSelectedPath(
            Type configType,
            string defaultFileName
        )
        {
            var config = SOPathSettingConfig.Instance;

            // 使用新的分離式 API
            var basePath = config.GetBasePathForType(configType);
            var relativePath = config.GetRelativePathForType(configType, Attribute.SubFolderPath);

            // 建立 ScriptableObject
            return configType.CreateScriptableObjectAt(basePath, relativePath, defaultFileName);
        }

        /// <summary>
        /// 取得要建立的實際類型（考慮VarTag的RestrictType）
        /// </summary>
        private Type GetActualConfigType(Type defaultConfigType)
        {
            if (!Attribute.UseVarTagRestrictType)
            {
                // Debug.Log("Using default config type: " + defaultConfigType.Name+" property:"+Property.Name);
                return defaultConfigType;
            }
            //FIXME: 怎麼擴充？

            // 嘗試從父物件取得VarTag的RestrictType
            var parentObject = Property.ParentValues[0];
            if (parentObject is IConfigTypeProvider configTypeProvider)
            {
                var restrictType = configTypeProvider.GetRestrictType();
                if (restrictType != null && typeof(ScriptableObject).IsAssignableFrom(restrictType))
                {
                    // Debug.Log("Using RestrictType from ConfigTypeProvider: " + restrictType.Name);
                    return restrictType;
                }
            }

            // if (parentObject is AbstractMonoVariable monoVar && monoVar._varTag != null)
            // {
            //     var restrictType = monoVar._varTag.ValueFilterType;
            //     if (restrictType != null && typeof(ScriptableObject).IsAssignableFrom(restrictType))
            //     {
            //         // Debug.Log("Using RestrictType from VarTag: " + restrictType.Name);
            //         return restrictType;
            //     }
            // }

            // Debug.Log("Using default config type: " + defaultConfigType.Name+"parentObject: " + parentObject);
            return defaultConfigType;
        }

        private void CreateSOForSO()
        {
            var defaultConfigType = Property.ValueEntry.TypeOfValue;
            var actualConfigType = GetActualConfigType(defaultConfigType);
            //FIXME: 自動命名？
            var fileName = GenerateFileName(actualConfigType.Name);
            var myScriptableObject = CreateScriptableObjectWithSelectedPath(
                actualConfigType,
                fileName
            );

            Property.ValueEntry.WeakSmartValue = myScriptableObject;
        }

        private void CreateSOForMonoBehavior()
        {
            var defaultConfigType = Property.ValueEntry.TypeOfValue;
            var actualConfigType = GetActualConfigType(defaultConfigType);
            var parentComp = Property.ParentValues[0] as Component;
            var fileName = GenerateFileName(parentComp.name);
            var myScriptableObject = CreateScriptableObjectWithSelectedPath(
                actualConfigType,
                fileName
            );
            Property.ValueEntry.WeakSmartValue = myScriptableObject;

            // 執行後處理方法
            if (!string.IsNullOrEmpty(Attribute.PostProcessMethodName))
            {
                Debug.Log("PostProcessMethodName: " + Attribute.PostProcessMethodName);
                Debug.Log("parentComp: " + parentComp, parentComp);
                var type = parentComp.GetType();
                var method = type.GetMethod(
                    Attribute.PostProcessMethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );
                if (method != null)
                    method.Invoke(parentComp, new object[] { });
                else
                    Debug.LogError(
                        "PostProcessMethodName not found: " + Attribute.PostProcessMethodName
                    );
            }
        }

        private void DrawCreateButtonForList()
        {
            var guiContent = new GUIContent(
                "Add DescriptableTag",
                null,
                "Create a new ScriptableObject and add to list"
            );

            var buttonClicked = SirenixEditorGUI.SDFIconButton(
                EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight),
                guiContent,
                SdfIconType.FileEarmarkPlus,
                IconAlignment.LeftEdge
            );

            if (buttonClicked)
                CreateSOForList();
        }

        private void CreateSOForList()
        {
            // 取得 List 的元素類型
            var listType = Property.ValueEntry.TypeOfValue;
            var defaultElementType = GetCollectionElementType(listType);
            var actualElementType = GetActualConfigType(defaultElementType);

            // 產生檔案名稱
            var fileName = GenerateFileName(actualElementType.Name);

            // 使用路徑配置建立新的 ScriptableObject
            var newSo = CreateScriptableObjectWithSelectedPath(actualElementType, fileName);

            // 將新建立的 ScriptableObject 加入到 List 中
            if (newSo != null)
            {
                var list = Property.ValueEntry.WeakSmartValue as IList;
                if (list == null)
                {
                    // 如果 List 為 null，建立新的 List
                    list = (IList)Activator.CreateInstance(listType);
                    Property.ValueEntry.WeakSmartValue = list;
                }

                list.Add(newSo);
                Property.ValueEntry.ApplyChanges();
            }
        }

        /// <summary>
        /// 繪製路徑選擇 GUI
        /// </summary>
        private void DrawPathSelector(Type configType)
        {
            var config = SOPathSettingConfig.Instance;
            var basePath = config.GetBasePathForType(configType);
            // Debug.Log("Current Base Path For Type: " + configType.Name + " is " + basePath);
            var availablePaths = GetAvailablePaths();

            EditorGUILayout.BeginHorizontal();

            // 路徑選擇下拉選單
            var selectedIndex = availablePaths.IndexOf(basePath);
            if (selectedIndex < 0)
            {
                selectedIndex = 0; // 預設選擇 Assets
            }

            EditorGUILayout.LabelField("建立路徑:", GUILayout.Width(60));
            var newIndex = EditorGUILayout.Popup(selectedIndex, availablePaths.ToArray());

            if (newIndex != selectedIndex && newIndex >= 0)
            {
                // 路徑變更時更新配置
                Debug.Log("Set Path For Type: Attribute.SubFolderPath " + Attribute.SubFolderPath);
                var newPath = availablePaths[newIndex];
                config.SetPathForType(configType, newPath, subPath: Attribute.SubFolderPath);

                EditorUtility.SetDirty(config);
            }

            EditorGUILayout.EndHorizontal();
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            // EditorGUILayout.BeginFadeGroup(1);
            SirenixEditorGUI.BeginBox();
            var valueType = Property.ValueEntry.TypeOfValue;

            if (valueType.IsArray)
            {
                SirenixEditorGUI.ErrorMessageBox("[SOConfig] 不支援陣列，請改用 List<T>");
                SirenixEditorGUI.EndBox();
                CallNextDrawer(label);
                return;
            }

            var isListType = typeof(IList).IsAssignableFrom(valueType);

            // 檢查 valueType 是否繼承自 ScriptableObject
            if (isListType)
            {
                var elementType = GetCollectionElementType(valueType);
                if (!typeof(ScriptableObject).IsAssignableFrom(elementType))
                {
                    SirenixEditorGUI.EndBox();
                    CallNextDrawer(label);
                    return;
                }
            }
            else
            {
                if (!typeof(ScriptableObject).IsAssignableFrom(valueType))
                {
                    SirenixEditorGUI.EndBox();
                    CallNextDrawer(label);
                    return;
                }
            }

            if (isListType)
            {
                // 對於 List 類型，繪製預設內容和 Create 按鈕
                CallNextDrawer(label);

                var defaultElementType = GetCollectionElementType(valueType);
                var actualElementType = GetActualConfigType(defaultElementType);
                DrawPathSelector(actualElementType);

                DrawCreateButtonForList();
                SirenixEditorGUI.EndBox();
                return;
            }

            // 原有的單一物件檢查
            if ((Object)Property.ValueEntry.WeakSmartValue != null)
            {
                CallNextDrawer(label);
                SirenixEditorGUI.EndBox();
                return;
            }

            CallNextDrawer(label);

            // 顯示路徑選擇器
            var defaultConfigType = Property.ValueEntry.TypeOfValue;
            var actualConfigType = GetActualConfigType(defaultConfigType);
            DrawPathSelector(actualConfigType);

            // 建立按鈕
            var guiContent = new GUIContent(
                "Create",
                null,
                "Create a new ScriptableObject for this field"
            );

            var buttonClicked = SirenixEditorGUI.SDFIconButton(
                EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight),
                guiContent,
                SdfIconType.FileEarmarkSpreadsheet,
                IconAlignment.LeftEdge
            );

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
            SirenixEditorGUI.EndBox();
            // EditorGUILayout.EndFadeGroup();
        }
    }
}
