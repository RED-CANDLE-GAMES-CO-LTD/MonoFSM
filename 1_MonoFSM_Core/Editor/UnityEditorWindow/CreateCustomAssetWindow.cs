using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MonoFSM.Editor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine.UIElements;
using AnimatorController = UnityEditor.Animations.AnimatorController;

/// <summary>
/// 快捷在Project視窗建立各種資源的視窗
/// </summary>
public class CreateCustomAssetWindow : EditorWindow
{
    // public string searchQuery = "";

    // public string[] assetTypes = new string[]
    // {
    //     "AnimationClip", "AnimatorController", "AnimatorOverrideController", "Folder", "Scene", "C# Script", "Shader",
    //     "Material", "Texture", "Prefab"
    // };


// #if CUSTOM_CREATE_WINDOW
    // [MenuItem("Assets/Create Custom... #&_C", false, 0)]
    [MenuItem("Assets/Create Custom... #_N", false, 0)]
    public static void OpenCreateAssetWindow()
    {
        if (Selection.activeObject is GameObject)
        {
            Selection.activeGameObject.AddChildrenGameObject("GameObject");
            return;
        }

        var items = MenuWrapper.Instance.ExtractSubmenus("Assets/Create");
        foreach (var item in items)
        {
            Debug.Log(item);
        }
    
        
        var popup = CreateInstance<CreateCustomAssetWindow>();
        // popup.CreateAssetDataDict();
        var position = popup.position;
        position.center = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height).center;

        popup.ShowAsDropDown(position, new Vector2(250, 250));
        popup.position = position;
        popup.Focus();
        // popup.Show();
    }

// #endif
    private ToolbarSearchField searchField;
    private ListView assetList;

    private string[] filteredAssetTypes;
    private string lastValue;
    private void OnEnable()
    {
        var root = rootVisualElement;
        // StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/CreateAssetWindow.uss");
        // root.styleSheets.Add(styleSheet);
        //
        // VisualTreeAsset visualTreeAsset =
        //     AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/CreateAssetWindow.uxml");
        // visualTreeAsset.CloneTree(root);

        // root.focusable = true;
        var label = new Label("Create Asset")
        {
            style =
            {
                fontSize = 14,
                unityTextAlign = TextAnchor.MiddleCenter
            }
        };
        root.Add(label);
        // searchField = root.Q<TextField>("search-field");
        searchField = new ToolbarSearchField();
        searchField.RegisterValueChangedCallback(SearchFilterChanged);
        searchField.RegisterCallback<GeometryChangedEvent>(evt => searchField.Focus());

        //按esc就會把searchField的值清空，這樣會馬上又判一次就出去了...

        searchField.RegisterCallback<KeyUpEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return)
            {
                //focus還在search上
                CreateAsset(filteredAssetTypes.FirstOrDefault());
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                assetList.Focus();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                // Debug.Log("Escape pressed" + searchField.value);
                evt.StopImmediatePropagation();
                evt.StopPropagation();
                evt.PreventDefault();

                if (String.IsNullOrEmpty(lastValue))
                {
                    // Debug.Log("Close" + searchField.value);
                    Close();
                    return;
                }
                else
                {
                    // Debug.Log("Clear" + String.IsNullOrEmpty(lastValue));
                    searchField.Focus();
                }

                lastValue = "";
                // searchField.SetValueWithoutNotify("");
                UpdateAssetList();
            }
        });

        // searchField.RegisterCallback<KeyDownEvent>(evt =>
        // {
        //     
        // });
        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                
                Close();
                return;
            }

            if (evt.eventTypeId == MouseDownEvent.TypeId())
            {
                Debug.Log("MouseDownEvent");
                searchField.Focus();
            }
        });
        // assetList = root.Q<VisualElement>("asset-list");
        assetList = new ListView
        {
            makeItem = () =>
            {
                var button = new Button();
                var iconImage = new Image
                {
                    style =
                    {
                        width = 30,
                        height = 30
                    }
                };
                // button.RegisterCallback<KeyDownEvent>(evt => { Debug.Log("button KeyDown" + evt.keyCode); });
                button.Add(iconImage);
                return button;
            },
            bindItem = (element, i) =>
            {
                var button = element as Button;
                button.clickable = null;
                var assetType = filteredAssetTypes[i];
                button.text = assetType;
                button.userData = assetType;
                //好像會有舊的

                button.clicked += () => CreateAsset(assetType);
                if (button.ElementAt(0) is Image iconImage) iconImage.image = assetDataDict[assetType].icon;
                // }
            },
            itemsSource = filteredAssetTypes
            // focusable = true
        };
        assetList.RegisterCallback<KeyUpEvent>(evt =>
        {
            Debug.Log("assetList KeyUp" + evt.keyCode);
            if (evt.keyCode == KeyCode.Return)
            {
                Debug.Log("assetList KeyDown Return");
                CreateAsset(filteredAssetTypes[assetList.selectedIndex]);
                return;
            }
            evt.StopImmediatePropagation();
            evt.PreventDefault();
            evt.StopPropagation();
            
            if (evt.keyCode == KeyCode.Escape)
            {
                searchField.Focus();
            }
        });
        assetList.RegisterCallback<KeyDownEvent>(evt =>
        {
            //FIXME: 這個會拿不到evt.KeyCode...用keyUp做掉

            if (evt.keyCode == KeyCode.Escape)
            {
                //TODO: preventDefault可以？
                // searchField.Focus();
            }
            evt.StopPropagation();
            // else if (evt.keyCode == KeyCode.UpArrow)
            // {
            //     searchField.Focus();
            // }
        });
        
        root.Add(searchField);
        root.Add(assetList);
        assetList.focusable = true;
        UpdateAssetList();
    }


    private void SearchFilterChanged(ChangeEvent<string> evt)
    {
        lastValue = evt.previousValue;
        UpdateAssetList();
    }

    private void UpdateAssetList()
    {
        if (assetDataDict == null) CreateAssetDataDict();
        filteredAssetTypes = Array.FindAll(assetDataDict.Keys.ToArray(),
            assetType => assetType.ToLower().Contains(searchField.value.ToLower()));

        assetList.itemsSource = filteredAssetTypes;
        assetList.RefreshItems();
        assetList.SetSelection(0);
    }

    // foreach (var assetType in filteredAssetTypes)
    // {
    //     var button = new Button(() => CreateAsset(assetType)) { text = assetType };
    //     assetList.Add(button);
    // }
    public class AssetData
    {
        public string extension;

        public Texture2D icon;

        // public Func<string, UnityEngine.Object> createFunc;
        public Type dataType;

        public AssetData(string extension, Texture2D icon, Type dataType = null)
        {
            this.extension = extension;
            this.icon = icon;
            this.dataType = dataType; //一些特別的不需要dataType (folder, scene)
            // this.createFunc = createFunc;
        }

        public AssetData(string extension, Type dataType)
        {
            this.extension = extension;
            this.dataType = dataType;
            icon = AssetPreview.GetMiniTypeThumbnail(dataType);
            // icon = EditorGUIUtility.ObjectContent(null, dataType).image as Texture2D;
            // this.createFunc = createFunc;
        }
    }

    private static Dictionary<string, AssetData> assetDataDict;

    void CreateAssetDataDict()
    {
        assetDataDict = new Dictionary<string, AssetData>
        {
            {
                "Timeline",
                new AssetData(".playable", EditorGUIUtility.IconContent("TimelineAsset Icon").image as Texture2D)
            },
            {
                "Assembly Definition",
                new AssetData(".asmdef", EditorGUIUtility.IconContent("Assembly Icon").image as Texture2D,
                    typeof(AssemblyDefinitionAsset))
            },
            {
                "AnimationClip",
                new AssetData(".anim", typeof(AnimationClip))
            },
            {
                "AnimatorController",
                new AssetData(".controller", typeof(AnimatorController))
            },
            {
                "AnimatorOverrideController",
                new AssetData(".overrideController",
                    typeof(AnimatorOverrideController)
                )
            },
            { "Folder", new AssetData("", EditorGUIUtility.IconContent("Folder Icon").image as Texture2D) },
            { "Scene", new AssetData(".unity", EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D) },
            {
                "C# Script",
                new AssetData(".cs", EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D)
            },
            { "Shader", new AssetData(".shader", EditorGUIUtility.IconContent("Shader Icon").image as Texture2D) },
            {
                "Material",
                new AssetData(".mat", EditorGUIUtility.IconContent("Material Icon").image as Texture2D)
            },
            {
                "Texture",
                new AssetData(".png", EditorGUIUtility.IconContent("Texture Icon").image as Texture2D)
            },
            {
                "Prefab",
                new AssetData(".prefab", EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D)
            }
            // {
            //     "GameConfig",
            //     new AssetData(".asset", typeof(GameConfigBase))
            // }
            
        };
        var scriptables = EditorMonoNodeExtension.GetAllScriptableAssetType();
        foreach (var scriptable in scriptables)
            if (!assetDataDict.ContainsKey(scriptable.Name))
                assetDataDict.Add(scriptable.Name, new AssetData(".asset", scriptable));
    }

    // private Dictionary<string, string> assetTypeExtensions = new()
    // {
    //     { "AnimationClip", ".anim" },
    //     { "AnimatorController", ".controller" },
    //     { "AnimatorOverrideController", ".overrideController" },
    //     { "Folder", "" },
    //     { "Scene", "" },
    //     { "C# Script", ".cs" },
    //     { "Shader", ".shader" },
    //     { "Material", ".mat" },
    //     { "Texture", ".png" },
    //     { "Prefab", ".prefab" }
    // };
    //
    // private Dictionary<string, Type> assetTypeClasses = new()
    // {
    //     { "AnimationClip", typeof(AnimationClip) },
    //     { "AnimatorController", typeof(AnimatorController) },
    //     { "AnimatorOverrideController", typeof(AnimatorOverrideController) },
    //     { "Folder", null },
    //     { "Scene", null },
    //     { "C# Script", null },
    //     { "Shader", typeof(Shader) },
    //     { "Material", typeof(Material) },
    //     { "Texture", null },
    //     { "Prefab", null }
    // };

    private void CreateAsset(string assetType)
    {
        Debug.Log("CreateAsset:" + assetType);
        if (!assetDataDict.ContainsKey(assetType))
        {
            Debug.LogError($"Asset type {assetType} is not supported.");
            return;
        }

//TODO: 選資料夾會跑到上面去...
        
        var selectedAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        //if the selected object is a folder, use it as the default path
        if (!string.IsNullOrEmpty(selectedAssetPath) && Directory.Exists(selectedAssetPath))
            selectedAssetPath += "/";
        
        var folderPath = selectedAssetPath.Length > 0
            ? Path.GetDirectoryName(selectedAssetPath)
            : "Assets";
        var assetName = "New " + assetType;
        var extension = assetDataDict[assetType].extension;
        var assetPath = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + assetName + extension);
        
        switch (assetType)
        {
            case "Assembly Definition":
                EditorApplication.ExecuteMenuItem("Assets/Create/Scripting/Assembly Definition");
                return;
            case "Folder":
                ProjectWindowUtil.CreateFolder();
                return;
            case "Scene":
                EditorApplication.ExecuteMenuItem("Assets/Create/Scene/Scene");
                return;
            case "Timeline":
                EditorApplication.ExecuteMenuItem("Assets/Create/Timeline/Timeline");
                return;
            case "C# Script":
                EditorApplication.ExecuteMenuItem("Assets/Create/C# Script");
                return;
            case "Material":
                EditorApplication.ExecuteMenuItem("Assets/Create/Material");
                return;
            case "Shader":
                EditorApplication.ExecuteMenuItem("Assets/Create/Shader/Standard Surface Shader");
                return;
            //TODO: 這個要從entry就決定嗎？
            case "AnimatorController":
                var controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
                
                Selection.activeObject = controller;
                return;
        }


   
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            folderPath = System.IO.Path.GetDirectoryName(folderPath);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogError($"Selected asset is not a folder: {folderPath}");
                return;
            }
            
        }

       

        var assetClass = assetDataDict[assetType].dataType;
        if (assetClass == null)
        {
            Debug.LogError($"Asset type {assetType} is not supported yet.");
            return;
        }


        // var constructorInfoObj = assetClass.GetConstructor(
        //     BindingFlags.Instance | BindingFlags.Public, null,
        //     CallingConventions.HasThis, Type.EmptyTypes, null);
        //
        // var asset = constructorInfoObj.Invoke(null) as UnityEngine.Object;

        var asset = Activator.CreateInstance(assetClass) as UnityEngine.Object;
        if (asset == null)
        {
            Debug.LogError($"Failed to create asset of type {assetType}.");
            return;
        }
        AssetDatabase.CreateAsset(asset, assetPath);
        Debug.Log("Created asset: " + assetPath);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
    }
 
}