using System;
using System.Collections.Generic;
using System.Linq;
using HierarchyIDEWindow.MonoFSM_HierarchyDrawer.Editor;
using RCGExtension;
using MonoFSM.Editor;
using RCGMakerFSMCore.Editor.ViewWindow.HierarchyTab;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using MonoFSM.InternalBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using static MonoFSM.Core.Editor.WrapperUtil;
namespace MonoFSM.Core.Editor
{
   
    public class TreeViewWindow : OdinEditorWindow
    {
        [HideInInspector] [SerializeField] private StyleSheet uss;

        private static readonly List<TreeViewWindow> _windows = new();
        static TreeViewWindow _currentWindow;
        // [MenuItem("GameObject/子樹檢視模式 搜尋 %_F", false, -1)]
        // public static void Find()
        // {
        //     _currentWindow._searchField.Focus();
        //     Debug.Log("Find");
        // }
        [MenuItem("GameObject/子樹檢視模式 Open Sub-Tree Hierarchy #_T", false, -1)]
        public static void ShowMonoHierarchyTab()
        {
            if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject)))
            {
                FolderWindow.ShowWindow();
                return;
            }

            if (Selection.activeGameObject == null)
                return;


            // var treeViewWindow = GetWindow<TreeViewWindow>();

            // if(treeViewWindow == null)
            var treeViewWindow = CreateInstance<TreeViewWindow>();
            
            // treeViewWindow = CreateInstance<TreeViewWindow>();

            var guidComp = Selection.activeGameObject.GetComponent<GuidComponent>();
            if (guidComp == null)
            {
                guidComp = (GuidComponent)Undo.AddComponent(Selection.activeGameObject, typeof(GuidComponent));
            }

            var gObj = treeViewWindow.SubTreeRoot = Selection.activeGameObject;
            var windowName = gObj.name;
            
            treeViewWindow.guidReference = new GuidReference(guidComp);
            treeViewWindow._instanceId = gObj.GetInstanceID();
            var titleContent = new GUIContent(windowName);
            treeViewWindow.titleContent = titleContent;

            // window.hideFlags = HideFlags.DontUnloadUnusedAsset;
            // window.ShowTab();
            treeViewWindow.Show();
            
            _currentWindow = treeViewWindow;
            // var hierarchyWindow = GetWindow(t_SceneHierarchyWindow);
            var hierarchyWindow = WindowDocker.GetSceneHierarchyWindow;
            if (hierarchyWindow == null)
            {
                Debug.LogError("SceneHierarchyWindow not found");
                return;
            }
            // var consoleWindow = GetWindow(typeof(SceneView));
            hierarchyWindow.Dock(treeViewWindow, WindowDocker.DockPosition.Top);
            // consoleWindow.DockTo(hierarchyWindow, WindowDocker.DockPosition.Top);
            // window.AddTab(hierarchyWindow);
            // hierarchyWindow.DockTo(window, Docker.DockPosition.Top);
            // var hierarchyWindowDockArea = new DockAreaWrapped(hierarchyWindow.GetFieldValue("m_Parent"));
            //
            // Debug.Log("DockArea" + hierarchyWindowDockArea);
            // hierarchyWindowDockArea.parentSplitViewWrapped.DropWindowAtTop(window);
            // _windows.Add(window);
        }
        
        // protected override void OnImGUI()
        // {
        //     base.OnImGUI();
        //     Debug.Log("OnImGUI");
        //     _currentWindow = this;
        //     // base.OnGUI();
        //    
        // }

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
            if(!_windows.Contains(this))
                _windows.Add(this);
            // EditorApplication.hierarchyChanged += MyOnHierarchyChange;
            _currentWindow = this;
            // var globalEventHandler = typeof(EditorApplication).GetFieldValue<EditorApplication.CallbackFunction>("globalEventHandler");
            // typeof(EditorApplication).SetFieldValue("globalEventHandler", Shortcuts + (globalEventHandler - Shortcuts));
        }
        static Type t_SceneHierarchyWindow = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        static Type t_SceneView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneView");
        
        //全域監聽
        static void Shortcuts()
        {
            // Debug.Log("Event:"+curEvent.type);
            if (!curEvent.isKeyDown) return;
            if (focusedWindow == null)
                return;
            if (focusedWindow.GetType() != typeof(TreeViewWindow) &&
                focusedWindow.GetType() != t_SceneHierarchyWindow && focusedWindow.GetType() != t_SceneView)
            {
                // Debug.Log("Not in TreeViewWindow");
                return;
            }
                
            if (_windows.Count == 0)
            {
                // Debug.Log("No windows");
                return;
            }
                
            if(PrefabStageUtility.GetCurrentPrefabStage() == null)
                return;
            
            // if (curEvent.keyCode == KeyCode.None) return;
            // if (curEvent.keyCode == KeyCode.Escape)
            // {
            //     //回到Hierarchy
            //     GetWindow(t_SceneHierarchyWindow).Focus();
            //     Selection.activeGameObject = HierarchyHighLightEditor.currentFindObject;
            //     Debug.Log("Escape:"+Selection.activeGameObject,Selection.activeGameObject);
            // }
            if (curEvent.keyCode == KeyCode.F && curEvent.e.command)
            {
                // Debug.Log("% F");
                HierarchyHighLightEditor.currentFindObject = Selection.activeGameObject;
                
                if (_currentWindow == null)
                {
                    _currentWindow = _windows.FirstOrDefault();
                }
                if (_currentWindow == null)
                {
                    Debug.Log("No windows"+_windows.Count);
                    _windows.RemoveAllNull();
                    return;
                }

                Debug.Log("_currentWindow" + _currentWindow);
                // Debug.Log("Custom search triggered!"+_currentWindow.name);
                _currentWindow.Focus();
                _currentWindow._searchField.Focus();
                
                
                //這個可以直接搶走，有點猛！
                curEvent.Use();
            }
            
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
        private GameObject SubTreeRoot
        {
            get => _subTreeRoot;
            set
            {
                Debug.Log("FramedGameObject set" + value);
                _subTreeRoot = value;
            }
            // _instanceId = value.GetInstanceID();
        } //這個會掉？

        [FormerlySerializedAs("_framedGameObject")] [ReadOnly]
        public GameObject _subTreeRoot;

        [HideInInspector] [ReadOnly] [SerializeField]
        public int _instanceId;

        [HideInInspector] [ReadOnly] [SerializeField]
        public GuidReference guidReference;

        private void RebindFrameGameObject()
        {
            if (SubTreeRoot != null)
            {
                return;
            }

            //有GUID的話，就用GUID
            // if (guidReference.gameObject != null)
            // {
            //     SubTreeRoot = guidReference.gameObject;
            //     _instanceId = SubTreeRoot.GetInstanceID();
            //     return;
            // }

            //用instanceId
            SubTreeRoot = UnityEditor.EditorUtility.InstanceIDToObject(_instanceId) as GameObject;
            if (SubTreeRoot == null)
            {
                Close();
                Debug.LogError("framedGameObject is null");
                return;
            }

            SubTreeRoot.GetComponentsInChildren(true, allCompsOfFramedGameObject);
        }

        private TreeView _treeView;

        public void CreateGUI()
        {
            Debug.Log("CreateGUI" + titleContent + " data:" + SubTreeRoot);
            RebindFrameGameObject();
            EditorSceneManager.sceneSaved += (scene) =>
            {
                // Debug.Log("sceneSaved");
                RebindFrameGameObject();
                CreateNestedTreeViewItemDataForTargetGameObject();
            };
            //title 的icon
            titleContent.image = AssetPreview.GetMiniThumbnail(SubTreeRoot);

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
        private FindToolbarSearchField _searchField;

        void CreateSearchField()
        {
            _searchField = new FindToolbarSearchField
            {
                style =
                {
                    width = StyleKeyword.Auto
                }
            };
            
            // _textField.RegisterCallback<FocusInEvent>(evt =>
            // {
            //     // Debug.Log("FocusIn");
            //     Input.imeCompositionMode = IMECompositionMode.On;
            // });
            // _textField.RegisterCallback<FocusOutEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.Auto; });
            _searchField.RegisterValueChangedCallback(OnSearchFieldChanged);
            _searchField.RegisterCallback<BlurEvent>(evt =>
            {
                //原本是想說要esc觸發
                //FIXME: 按enter也會觸發...哭哭XDD
                Debug.Log("BlurEvent:"+curEvent.keyCode);
                
                Selection.activeGameObject = HierarchyHighLightEditor.currentFindObject;
                GetWindow(t_SceneHierarchyWindow).Focus();
                // _textField.Focus();
                // _textField.value = "";
                // _textField.value = HierarchyHighLightEditor.searchToken;
            });
            _searchField.RegisterCallback<FocusEvent>(evt =>
            {
                Debug.Log("FocusEvent");
                // _textField.value = HierarchyHighLightEditor.searchToken;
                // _textField.Focus();
            });
            _searchField.RegisterCallback<NavigationCancelEvent>(evt =>
            {
                Debug.Log("NavigationCancelEvent");
                // _textField.Focus();
                // _textField.value = "";
                // _textField.value = HierarchyHighLightEditor.searchToken;
            });

            _searchField.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                 // Debug.Log("NavigationSubmitEvent");
                 //這個submit會變回編輯模式
                HierarchyHighLightEditor.FindNextObject();
                // _textField.Focus();
                evt.StopPropagation();
            },TrickleDown.TrickleDown); //stop propagation就擋掉了嗎？
            _searchField.RegisterCallback<NavigationMoveEvent>(OnNavigationMoveEvent);
            _searchField.RegisterCallback<KeyDownEvent>(evt =>
            {
                // evt.StopImmediatePropagation();
                // evt.StopPropagation();
                // Debug.Log("KeyDownEvent" + evt.keyCode); 
                // if (evt.keyCode == KeyCode.DownArrow)
                // {
                //     Debug.Log("DownArrow");
                //     
                //     // _treeView.Focus();
                //     // _treeView.SetSelection(0);
                //     //
                //     HierarchyHighLightEditor.FindNextObject();
                //     _textField.Focus();
                //     evt.StopPropagation();
                //     
                // }
                // else if (evt.keyCode == KeyCode.Return)
                // {
                //     Debug.Log("Return");
                //     HierarchyHighLightEditor.FindNextObject();
                //     _textField.Focus();
                //     evt.StopPropagation();
                //     // _treeView.Focus();
                //     // _treeView.SetSelection(0);
                //     // _treeView.ScrollToItem(_treeView.selectedIndex);
                //     // var label = _treeView.selectedItem as DraggableLabel;
                //     // label.Rename();
                // }
            });
            rootVisualElement.Add(_searchField);
        }

        private void OnNavigationMoveEvent(NavigationMoveEvent evt)
        {
            Debug.Log("NavigationMoveEvent " + evt.direction);
            if (evt.direction == NavigationMoveEvent.Direction.Down)
            {
                // _treeView.Focus();
                // _treeView.SetSelection(0);
                HierarchyHighLightEditor.FindNextObject();
                // _searchField.Focus();
                evt.StopPropagation();
            }
            else if (evt.direction == NavigationMoveEvent.Direction.Up)
            {
                // _treeView.Focus();
                // _treeView.SetSelection(0);
                HierarchyHighLightEditor.FindPreviousObject();
                // _searchField.Focus();
                evt.StopPropagation();
            }
        }

        int lastSelectedID = 0;

        void OnSearchFieldChanged(ChangeEvent<string> evt)
        {
            // HierarchyHighLightEditor.lastSearchToken = ;
            HierarchyHighLightEditor.FilterObjects(evt.newValue);
            EditorApplication.RepaintHierarchyWindow();
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
                SubTreeRoot.GetComponentsInChildren(true, allCompsOfFramedGameObject);
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
            _currentWindow = this;
            Debug.Log("OnFocus");
        }

        //
        void CreateNestedTreeViewItemDataForTargetGameObject()
        {
            int id = 0;
            if (SubTreeRoot == null)
            {
                RebindFrameGameObject(); // Close();)
                // return;
            }

            //dfs of transform hierarchy
            rootTreeViewItemData = CreateTreeViewDataOfGameObject(SubTreeRoot, ref id);

            if (_treeView == null)
                return;
            _treeView.SetRootItems(new[] { rootTreeViewItemData });
            _treeView.RefreshItems();

            // _treeView.ExpandAll();
            allGameObjects = SubTreeRoot.GetComponentsInChildren<Transform>(true).Select((t) => t.gameObject)
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