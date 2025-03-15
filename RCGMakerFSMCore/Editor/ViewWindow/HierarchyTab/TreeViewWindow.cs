using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
// using RelationsInspector.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace RCGMaker.Core.Editor
{
    public class TreeViewWindow : OdinEditorWindow
    {
        [HideInInspector] [SerializeField] private StyleSheet uss;

        private static List<TreeViewWindow> _windows = new List<TreeViewWindow>();

        [MenuItem("GameObject/檢視模式 Open Isolated Hierarchy #_T", false, -1)]
        public static void ShowMonoHierarchyTab()
        {
            if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject)))
            {
                FolderWindow.ShowWindow();
                return;
            }
            if (Selection.activeGameObject == null)
                return;
      
                


            // var window = GetWindow<TreeViewWindow>();

            var window = CreateInstance<TreeViewWindow>();

            var guidComp = Selection.activeGameObject.GetComponent<GuidComponent>();
            if (guidComp == null)
            {
                guidComp = (GuidComponent)Undo.AddComponent(Selection.activeGameObject, typeof(GuidComponent));
            }

            var gObj = window.FramedGameObject = Selection.activeGameObject;
            var windowName = gObj.name;

            window.guidReference = new GuidReference(guidComp);
            window._instanceId = gObj.GetInstanceID();
            var titleContent = new GUIContent(windowName);
            window.titleContent = titleContent;

            // window.hideFlags = HideFlags.DontUnloadUnusedAsset;
            window.Show();
            // _windows.Add(window);
        }

        protected override void OnGUI()
        {
            // base.OnGUI();
        }
        
//         private void OnSelectionChange()
//         {
//             // OpenIDE();
//             // Debug.Log("Here is a log: " + "Open Needle Website".LinkTo("http://www.needle.tools"));
//             Debug.Log("OnSelectionChange");
//             // Selection.activeGameObject
//             if (Selection.activeGameObject == null)
//                 return;
//             var id = Selection.activeGameObject.GetInstanceID();
//
//             try
//             {
//                 var selectedObj = _treeView.GetItemDataForId<GameObject>(id);
//                 if (selectedObj == null)
//                     // Debug.Log("select 不在這個gameobject下面");
//                     return;
//
//                 _treeView.SetSelectionByIdWithoutNotify(new[] { id });
//                 _treeView.ScrollToItemById(id);
//                 lastSelectedID = id;    
//             }
//             catch (Exception e)
//             {
//                 // Debug.Log("select 不在這個gameobject下面");
//             }
//             
//             // _treeView.ScrollToItem(_treeView.selectedIndex);
// // _treeView.SetSelection();
//             //TODO: 要同步？
//         }

        private void OnHierarchyChange() 
        {
            if (Application.isPlaying)
                return;
            // Debug.Log("OnHierarchyChange");
            //有東西改變，重新生成View
            // RebindFrameGameObject();
            // CreateNestedTreeViewItemDataForTargetGameObject();
            // Debug.Log("OnHierarchyChange" + FramedGameObject + "id:" + _instanceId);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // EditorApplication.hierarchyChanged += MyOnHierarchyChange;
        }

        protected override void OnDestroy()
        {
            // Debug.Log("OnDestroy" + FramedGameObject + "id:" + _instanceId);
        }

        protected override void OnDisable()
        {
            // base.OnDisable();
            // EditorApplication.hierarchyChanged -= MyOnHierarchyChange;
            // Debug.Log("OnDisable" + FramedGameObject + "id:" + _instanceId);
        }

        [ReadOnly]
        public GameObject FramedGameObject
        {
            get => _framedGameObject;
            set
            {
                Debug.Log("FramedGameObject set" + value);
                _framedGameObject = value;
            }
            // _instanceId = value.GetInstanceID();
        } //這個會掉？

        [ReadOnly] public GameObject _framedGameObject;

        [HideInInspector] 
        [ReadOnly] [SerializeField] public int _instanceId;

        [HideInInspector]
        [ReadOnly] [SerializeField] public GuidReference guidReference;

        void RebindFrameGameObject()
        {
            if (FramedGameObject != null)
            {
                return;
            }

            //有GUID的話，就用GUID
            if (guidReference.gameObject != null)
            {
                FramedGameObject = guidReference.gameObject;
                _instanceId = FramedGameObject.GetInstanceID();
                return;
            }

            //用instanceId
            FramedGameObject = EditorUtility.InstanceIDToObject(_instanceId) as GameObject;
            if (FramedGameObject == null)
            {
                Close();
                Debug.LogError("framedGameObject is null");
                return;
            }

            FramedGameObject.GetComponentsInChildren(true, allCompsOfFramedGameObject);
        }

        private TreeView _treeView;

        public void CreateGUI()
        {
            Debug.Log("CreateGUI" + titleContent + " data:" + FramedGameObject);
            RebindFrameGameObject();
            EditorSceneManager.sceneSaved += (scene) =>
            {
                // Debug.Log("sceneSaved");
                RebindFrameGameObject();
                CreateNestedTreeViewItemDataForTargetGameObject();
            };
            //title 的icon
            titleContent.image = AssetPreview.GetMiniThumbnail(FramedGameObject);

            // Debug.Log("CreateGUI" + titleContent + " data:" + FramedGameObject);
            CreateSearchField();
            rootVisualElement.styleSheets.Add(uss);
            _treeView = new TreeView()
            {
                makeItem = () => new DraggableLabel()
                {
                    focusable = true,
                    // OnSelect = () =>
                    // {
                    //     var objs = _treeView.selectedItems.Select((obj) => (Object)obj).ToArray();
                    //     
                    //     // // Debug.Log("ListViewItem OnSelect" + objs[0]);
                    //     Selection.objects = objs;
                    //
                    //     //TODO: 2021版是不是不能這樣選？
                    //     // Selection.activeGameObject = objs[0];
                    // }
                },
                fixedItemHeight = 16,
                showBorder = true,
                // leftPane.reorderable = true;
            };


            var container = new IMGUIContainer();
            container.onGUIHandler = () =>
            {
                PropertyTree tree = PropertyTree.Create(this);
                tree.Draw();
            };
            rootVisualElement.Add(container);

            rootVisualElement.Add(_treeView);

            //selected GameObject as root item

            _treeView.bindItem = (element, index) =>
            {
                var label = (element as DraggableLabel);
                label.BindGo = _treeView.GetItemDataForIndex<GameObject>(index);
                treeViewElementDict.TryAdd(label.BindGo, label);
                // label.RegisterCallback<KeyDownEvent>(evt =>
                // {
                //     Debug.Log("Label KeyDownEvent");
                //     if (evt.keyCode == KeyCode.Return)
                //     {
                //         label.Rename();
                //
                //         // var label = _treeView.selectedItem as DraggableLabel;
                //         // label.Rename();
                //     }
                // });
            };
            _treeView.unbindItem = (element, index) =>
            {
                var label = (element as DraggableLabel);
                treeViewElementDict.Remove(label.BindGo, out _);
            };

            //select gameobject of treeview item\


            _treeView.selectionChanged += objs =>
            {
                // foreach (var obj in objs)
                // {
                //     Debug.Log("onSelectionChange" + obj);
                //     
                // }
                
                objs = objs.Where((obj) => obj != null);
                
                Selection.objects = objs.Select((obj) => (Object)obj).ToArray();
                if (Selection.objects != null && Selection.objects.Length > 0)
                    lastSelectedID = Selection.objects[0].GetInstanceID();
            };

            //快捷鍵也要自己寫唷
            _treeView.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Backspace && evt.commandKey)
                {
                    // Undo.DestroyObjectImmediate(Selection.activeGameObject);
                    Unsupported.DeleteGameObjectSelection();
                }
                //copy
                else if (evt.keyCode == KeyCode.D && evt.commandKey)
                {
                    Unsupported.CopyGameObjectsToPasteboard();
                    Unsupported.DuplicateGameObjectsUsingPasteboard();
                }
            });
            // _treeView.RegisterCallback<FocusInEvent>(evt => { Debug.Log("TreeView FocusInEvent"); });
            // _treeView.RegisterCallback<FocusOutEvent>(evt => { Debug.Log("TreeView FocusOutEvent"); });


            //enter, double click
            _treeView.itemsChosen += objs =>
            {
                //FIXME: textfield的enter被當作選擇物件了
                //1. 鎖住
                //2. 空降UI?
                // Debug.Log("onItemsChosen" + objs);
                
                // foreach (var obj in objs)
                // {
                //     Debug.Log("Rename" + obj);
                //     treeViewElementDict[obj as GameObject].Rename();
                // }
               
            };
            CreateNestedTreeViewItemDataForTargetGameObject();
        }

        Dictionary<GameObject, DraggableLabel> treeViewElementDict = new Dictionary<GameObject, DraggableLabel>();
        private ToolbarSearchField _textField;

        void CreateSearchField()
        {
            _textField = new ToolbarSearchField();
            _textField.style.width = StyleKeyword.Auto;
            // _textField.RegisterCallback<FocusInEvent>(evt =>
            // {
            //     // Debug.Log("FocusIn");
            //     Input.imeCompositionMode = IMECompositionMode.On;
            // });
            // _textField.RegisterCallback<FocusOutEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.Auto; });
            _textField.RegisterValueChangedCallback(OnSearchFieldChanged);
            _textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.DownArrow)
                {
                    _treeView.Focus();
                    _treeView.SetSelection(0);
                }
            });
            rootVisualElement.Add(_textField);
        }

        int lastSelectedID = 0;
        void OnSearchFieldChanged(ChangeEvent<string> evt)
        {
            if (evt.newValue.IsNullOrWhitespace())
            {
                // _treeView.SetItems(new List<GameObject>());
                // _treeView.RefreshItems();
                _treeView.SetRootItems(new[] { rootTreeViewItemData });
                _treeView.RefreshItems();
                _treeView.SetSelectionById(lastSelectedID);
                _treeView.ScrollToItemById(lastSelectedID);
                // Debug.Log("OnSearchFieldChanged" + evt.newValue);
                // Debug.Log("OnSearchFieldChanged lastSelectedID:" + lastSelectedID);
                return;
            }
            else
            {
                Search(evt.newValue);
            }

            // Search(evt.newValue);
        }

        private Dictionary<string, Type> typeLookup;

        private void TryBuildTypeLookupTable()
        {
            if (typeLookup != null)
                return;

            typeLookup = new Dictionary<string, Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Debug.Log(assembly.FullName);

                var types = assembly.GetTypes();
                foreach (var t in types)
                {
                    typeLookup[t.Name.ToLower()] = t;
                    // var type = assembly.GetType(className, false, true);
                    // if (type != null)
                    //     return type;
                }
            }
        }

        public Type GetTypeByName(string className)
        {
            if (className == "")
                return null;
            var lowerName = className.ToLower();
            // Search in all loaded assemblies

            TryBuildTypeLookupTable();
            if (typeLookup.TryGetValue(lowerName, out var type))
            {
                return type;
            }

            return null;
        }

        private List<Component> allCompsOfFramedGameObject = new();

        private Type SearchForType(string term)
        {
            if (allCompsOfFramedGameObject.Count == 0)
                FramedGameObject.GetComponentsInChildren(true, allCompsOfFramedGameObject);
            //search for component type if start with "t:", example t:BoxCollider
            if (term.StartsWith("t:"))
            {
                term = term.Substring(2).Replace(" ", "");

                var t = GetTypeByName(term);
                if (t == null)
                {
                    // Debug.LogError("no type match");
                    return null;
                }

                // Debug.Log(t);

                var filteredComps = FilterSearchUtility.SearchForComponentType(allCompsOfFramedGameObject, t);
                //get all gameobjects that have this component
                var filteredGObjs = filteredComps.Select((comp) => comp.gameObject).ToList();

                SetTreeViewItemDataForList(filteredGObjs);
                return t;
            }

            var rawT = GetTypeByName(term);
            if (rawT != null)
            {
                // Debug.Log("rawT:" + rawT);
                // Debug.Log(allCompsOfFramedGameObject.Count);
                var filteredComps = FilterSearchUtility.SearchForComponentType(allCompsOfFramedGameObject, rawT);
                //get all gameobjects that have this component
                var filteredGObjs = filteredComps.Select((comp) => comp.gameObject).ToList();
                SetTreeViewItemDataForList(filteredGObjs);
                return null;
            }

            return null;
        }

        private void Search(string term)
        {
            var type = SearchForType(term);
            if (type != null) //有指定type，就不要搜尋name了
            {
                Debug.Log("SearchForType:" + type);
                return;
            }


            var filteredObjects = FilterSearchUtility.SearchGameObjectsByTerm(allGameObjects, term);
            SetTreeViewItemDataForList(filteredObjects);
        }

        void SetTreeViewItemDataForList(IList<GameObject> list)
        {
            
            var treeViewItemData = new List<TreeViewItemData<GameObject>>();
            var lastID = 0;
            foreach (var item in list)
            {
                if (lastID == item.GetInstanceID())
                    continue;
                treeViewItemData.Add(new TreeViewItemData<GameObject>(item.GetInstanceID(), item));
                lastID = item.GetInstanceID();
            }

            _treeView.SetRootItems(treeViewItemData);
            _treeView.RefreshItems();
        }

        private void OnFocus()
        {
            // RebindFrameGameObject();
            CreateNestedTreeViewItemDataForTargetGameObject();
        }
        //
        void CreateNestedTreeViewItemDataForTargetGameObject()
        {
            int id = 0;
            if (FramedGameObject == null)
            {
                RebindFrameGameObject(); // Close();)
                // return;
            }
                
            //dfs of transform hierarchy
            rootTreeViewItemData = CreateTreeViewDataOfGameObject(FramedGameObject, ref id);


            _treeView.SetRootItems(new[] { rootTreeViewItemData });
            _treeView.RefreshItems();

            // _treeView.ExpandAll();
            allGameObjects = FramedGameObject.GetComponentsInChildren<Transform>(true).Select((t) => t.gameObject)
                .ToList();
        }

        List<GameObject> allGameObjects = new List<GameObject>();
        private TreeViewItemData<GameObject> rootTreeViewItemData;

        //write a function to create TreeViewItemData for gameObject when it has no children, using dfs to traverse and return a list of TreeViewItemData
        //iterate把gameObject轉成TreeViewItemData
        TreeViewItemData<GameObject> CreateTreeViewDataOfGameObject(GameObject gameObject, ref int id)
        {
            if (gameObject == null)
                return default;
            var childTreeItemData = new List<TreeViewItemData<GameObject>>();
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                var child = gameObject.transform.GetChild(i).gameObject;
                childTreeItemData.Add(CreateTreeViewDataOfGameObject(child, ref id));
            }

            var item = new TreeViewItemData<GameObject>(gameObject.GetInstanceID(), gameObject, childTreeItemData);

            return item;
        }
    }
}