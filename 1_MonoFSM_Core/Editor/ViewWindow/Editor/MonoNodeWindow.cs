using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// using Object = UnityEngine.Object;
namespace MonoFSM.Core.Editor
{
    [InitializeOnLoad]
    public static class MonoNodeManager
    {
        static MonoNodeManager()
        {
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStateClosing;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            
        }


        private static void OnPrefabStageOpened(PrefabStage stage)
        {
            var prefab = stage.prefabContentsRoot;
            MonoNodeWindow.CreatePrefabWindow(prefab);
            // Debug.Log("Prefab Root Changed:" + lastSelectedEntity);
            // DisplayEntity(lastSelectedEntity);
        }

        private static void OnPrefabStateClosing(PrefabStage obj)
        {
            // Close();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            MonoNodeWindow.UpdateCurrentWindow();
            
        }
    }


    //TODO: static PrefabStage?
    
    
    public class MonoNodeWindow : OdinEditorWindow
    {
        [MenuItem("Window/赤燭RCG/ShowMonoHierarchyTab Prefab ")]
        public static void ShowMonoHierarchyTab()
        {
            // animCont.layers[0].stateMachine.states[0].state;
            // This method is called when the user selects the menu item in the Editor
            // var wnd = CreateInstance<MonoNodeWindow>();

            // EditorWindow wnd = GetWindow<MonoNodeWindow>("Mono", );
            if (currentWindow && currentWindow.lastSelectedEntity == FindNearestPrefab())
            {
                EditorGUIUtility.PingObject(currentWindow.lastSelectedEntity);
            }
            else
            {
                var wnd = CreateDockWindow();
                currentWindow = wnd;
                wnd.ShowTab();
                wnd.SelectNearestPrefab();
            }
        }

        // [MenuItem("Window/赤燭RCG/ShowMonoHierarchyTab Any #%_T")]
        [MenuItem("Window/赤燭RCG/ShowMonoHierarchyTab Any")]
        public static void ShowMonoHierarchyTabAny()
        {
            // animCont.layers[0].stateMachine.states[0].state;
            // This method is called when the user selects the menu item in the Editor
            // var wnd = CreateInstance<MonoNodeWindow>();

            // EditorWindow wnd = GetWindow<MonoNodeWindow>("Mono", );
            if (currentWindow && currentWindow.lastSelectedEntity == Selection.activeGameObject)
            {
                EditorGUIUtility.PingObject(currentWindow.lastSelectedEntity);
            }
            else
            {
                currentPrefab = Selection.activeGameObject;
                var wnd = CreateDockWindow();
                currentWindow = wnd;
                wnd.ShowTab();
            }
        }

        private static MonoNodeWindow CreateDockWindow()
        {
//             var types = new List<Type>()
//             { 
//                 // first add your preferences
//                 typeof(SceneView), 
//                 typeof(Editor).Assembly.GetType("UnityEditor.GameView"),
//                 typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow"),
//                 typeof(Editor).Assembly.GetType("UnityEditor.ConsoleWindow"), 
//                 typeof(Editor).Assembly.GetType("UnityEditor.ProjectBrowser"), 
//                 typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow")
//             };
//
// // and then add all others as fallback (who cares about duplicates at this point ? ;) )
//             types.AddRange(AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(type =>type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(EditorWindow))));
            Type dockNextToType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ConsoleWindow");
            // Type dockNextToType = EditorWindow.focusedWindow.GetType();
            if (HasOpenInstances<MonoNodeWindow>())
            {
                dockNextToType = typeof(MonoNodeWindow);
            }


            var wnd = CreateWindow<MonoNodeWindow>("Mono", dockNextToType);
            return wnd;
        }
    private static GameObject currentPrefab;

    public static MonoNodeWindow CreatePrefabWindow(GameObject prefab)
    {
        //先不要自動彈出來，有點煩

        // currentPrefab = prefab;
        // var wnd = CreateDockWindow();
        // PrefabStage.prefabStageClosing += wnd.OnPrefabStateClosing;
        // currentWindow = wnd;
        // wnd.ShowTab();
        // return wnd;
        return null;
    }

    public static void UpdateCurrentWindow()
    {
        if (currentWindow)
            currentWindow.DrawHierarchyList();
    }

    // private void Awake()
    // {
    //     Debug.Log("MonoNodeWindow Awake" + lastSelectedEntity);
    // }
    [HideIf("prefab")] [PropertyOrder(-1)]
    public GuidReference monoRef = new();

    private GameObject prefab;

    [HideIf("@monoRef.gameObject")]
    [PropertyOrder(-1)]
    [ReadOnly]
    [ShowInInspector]
    public GameObject Prefab
    {
        get => prefab;
        set
        {
            prefab = value;
            titleContent.text = $"@ {value.name}";
            titleContent.image = AssetPreview.GetMiniThumbnail(value);
        }
    }

    GuidReference MonoRef
    {
        get => monoRef;
        set
        {
            if (value == null)
                return;
            monoRef = value;
            if (currentWindow == this) titleContent.text = $"{lastSelectedEntity.name}";
            titleContent.image = AssetPreview.GetMiniThumbnail(lastSelectedEntity);
        }
    }

    [HorizontalGroup("ModuleEntity")]
    [LabelText("編輯實體")]
    [ReadOnly]
    GameObject lastSelectedEntity => prefab ? prefab : monoRef?.gameObject;
    // {
    //     get => _lastSelectedEntity;
    //     set
    //     {
    //         if (value == null)
    //             return;
    //         _lastSelectedEntity = value;
    //         var guidRef = value.GetComponent<GuidReference>();
    //         monoRef = guidRef;
    //
    //     }
    // }

    private void OnBecameVisible()
    {
        currentWindow = this;
        if (currentPrefab != null)
        {
            Prefab = currentPrefab;
            currentPrefab = null;
        }
            
        if (listView == null)
            return;

        DrawHierarchyList();
    }

    private static MonoNodeWindow currentWindow;

    [HorizontalGroup("ModuleEntity", 60)]
    [ShowInInspector]
    [PreviewField]
    [HideLabel]
    public GameObject preview => lastSelectedEntity;
    
    private bool IsSyncSelectionInHierarchy = false;
    private DropdownField FilterTypeDropDown;
    // public Type[] filterTypes = new[] {typeof(IRCGSignal),typeof(EncapsuleExclusionTag), typeof(Component), typeof(Collider),typeof(RCGSignalType),typeof(AbstractVariable) };
    // [SerializeField]
    // Type[] filterTypes = { typeof(Collider2D),typeof(Collider)};


    // Type[] FilterTypes => overridePreset ? overridePreset.allowTypes : new[] { typeof(Component) };
    private Type[] FilterTypes { get; } = { typeof(Component) };

    //TODO: 拿什麼當做物件本體？
    private static Type ModuleEntityType => typeof(Component);
    void UpdateFilterEntry()
    {
        //TODO: 存檔, recompile 也會call過來？ Undo Redo...
        // Debug.Log("UpdateFilterEntry");

        //TODO: 換preset就應該要更新？
        // if (TypeFilterEntries == null || TypeFilterEntries.Length == 0)
            TypeFilterEntries = FilterTypes.Where(x => x != null)
                .Select(x => new TypeFilterEntry() { type = x, Enabled = true }).ToArray();
            
       
    }

    // [InlineEditor] [OnValueChanged("UpdateFilterEntry")]
    // public FilterPreset overridePreset;
    // [OnValueChanged("UpdateView")]
    // [ValueDropdown("FilterTypes")]
    // public Type currentType;
    

    
    //TODO: readonly, 可以改isOn
    // [OnValueChanged("UpdateView")]
    // [ValueDropdown("FilterTypes")] 
    
    
    [ListDrawerSettings(HideRemoveButton = true, HideAddButton = true)] [OdinSerialize]
    public TypeFilterEntry[] TypeFilterEntries = Array.Empty<TypeFilterEntry>(); //TODO: 用Editor Pref 記住

  
    // [Toggle("Enabled")]
    public struct TypeFilterEntry
    {
        
        // private string GetLabel()
        // {
        //     return "<icon name=UnityEditor.SceneAsset.Icon/>";
        // }
        
        // [LabelText("@type.Name")]
        [HorizontalGroup] [OnValueChanged("@$root.DrawHierarchyList()")]
        public bool Enabled; 
        
        [HorizontalGroup(),HideLabel]
        
        // [GUIColor(1,1,1,1)]
        // [GUIColor("black")]
        
        // [DisableIf("@true")]
        [EnableGUI]
        [ReadOnly]
        [ValueDropdown("@$root.FilterTypes")]
        public Type type;
    }
    public bool Lock;

    // private DropdownField entityDropDown;
    protected override void OnGUI()
    {
        // base.OnGUI();
    }

    //進入點
    public void CreateGUI()
    {
        Debug.Log("CreateGUI");
        // if (overridePreset == null)
        // {
        // overridePreset =
        //     AssetDatabase.LoadAssetAtPath("Assets/FilterPreset.asset", typeof(FilterPreset)) as
        //         FilterPreset;
        // }
        UpdateFilterEntry();
        
        
        // Create a two-pane view with the left pane being fixed with
        var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Vertical);
        FilterTypeDropDown = new DropdownField
        {
            choices = Array.ConvertAll(FilterTypes, type => type.Name).ToList(),
            index = 0
        };

        // CreateEntityDropDown();
      
        // rootVisualElement.Add(entityDropDown);
        
        FilterTypeDropDown.RegisterValueChangedCallback(evt =>
        {
            UpdateView();
        });
        // rootVisualElement.Add(FilterTypeDropDown);
        rootVisualElement.Add(splitView);
        _textField = new ToolbarSearchField();
        _textField.RegisterCallback<FocusInEvent>(evt =>
        {
            // Debug.Log("FocusIn");
            Input.imeCompositionMode = IMECompositionMode.On;
        });
        _textField.RegisterCallback<FocusOutEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.Auto; });
        
// A TwoPaneSplitView always needs exactly two child elements
        listView = new ListView
        {
            makeItem = () => new DraggableLabel(){focusable = true,OnSelect = () =>
            {
                // if(onSelectObjects == null)
                //     return;

                var objs = listView.selectedItems.Select((obj) => (obj as Component)?.gameObject).ToArray();
                // Debug.Log("ListViewItem OnSelect" + objs[0]);
                Selection.objects = objs;

                //TODO: 2021版是不是不能這樣選？
                // Selection.activeGameObject = objs[0];
            }},
            fixedItemHeight = 16,
            showBorder = true,
            // leftPane.reorderable = true;
            showBoundCollectionSize = true
        };
        
        listView.onItemsChosen += (objs) =>
        {
            Debug.Log(objs);
            var gameObjects = objs.Select((obj) => (obj as Component)?.gameObject).ToArray();
            Selection.objects = gameObjects;
            SceneView.FrameLastActiveSceneView();
            EditorGUIUtility.PingObject(gameObjects[0]);
            InternalEditorUtility.SetIsInspectorExpanded(objs.First() as Component, true);

            //TODO: show a popup inspector???
            
        };


        //用鍵盤做navigation時
        listView.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode is KeyCode.DownArrow or KeyCode.UpArrow)
            {
#if UNITY_2022_2_OR_NEWER
     //防止按上下時會嘟嘟叫
                 evt.StopImmediatePropagation();
//               evt.PreventDefault();
#endif
            }
        });


        var treeView = new TreeViewItemData<DraggableLabel>();
        
        
        // var treeView = rootVisualElement.Q<TreeView>();
        var topPanel = new VisualElement();
        // _textField.style.minHeight = 20;
        _textField.RegisterValueChangedCallback(evt => { UpdateView(); });
        topPanel.Add(_textField);
        topPanel.Add(listView);
        topPanel.style.minHeight = 40;
        listView.style.paddingLeft = 4;
        splitView.Add(topPanel);
        var scrollView = new ScrollView();
        var container = new IMGUIContainer();
        container.onGUIHandler = () =>
        {
            EnsureTree();
            tree.Draw();
        };
        
        scrollView.Add(container);
        scrollView.style.minHeight = 40;

        splitView.Add(scrollView);
        // var rightPane = new VisualElement();
        // splitView.Add(treeView);
        // rootVisualElement.Add(listView);
        DisplayEntity(lastSelectedEntity);
        
            

        var toggleButton = new Toggle("select in hierarchy");
        toggleButton.RegisterValueChangedCallback((evt) =>
        {
            IsSyncSelectionInHierarchy = evt.newValue;
        });
        toggleButton.value = IsSyncSelectionInHierarchy;
        rootVisualElement.Add(toggleButton);
        // var button = new Button(() =>
        // {
        //     var list = new List<int>();
        //     
        //     for (var i = 0; i < 3; i++)
        //     {
        //         list.Add(i);
        //     }
        //
        //     Debug.Log("SetSelection"+leftPane.childCount);
        //     leftPane.SetSelection(list);
        // });
        // button.text = "SelectAll";
        // rootVisualElement.Add(button);

        // if (overridePreset != null) UpdateFilterEntry();

        
        listView.selectionType = SelectionType.Multiple;
        listView.Clear();

    }
    private PropertyTree tree;
    private void EnsureTree()
    {
        tree ??= PropertyTree.Create(this);
    }


    private void DrawOdin()
    {
        EnsureTree();
        tree.Draw();
    }
    //TODO: 包成一個特別的field
    // DropdownField CreateEntityDropDown()
    // {
    //     entities = FindObjectsOfType(ModuleEntityType);
    //     entityDropDown = new DropdownField
    //     {
    //         choices = Array.ConvertAll(entities, (entity) => entity.name).ToList(),
    //             index = 0
    //     };
    //     entityDropDown.RegisterValueChangedCallback(evt =>
    //     {
    //         DisplaySceneEntity();
    //     });
    //     return entityDropDown;
    // }
    // [PreviewField]
    // public GameObject RegularPreviewField;
    // void UpdateEntities(Object[] _entiies)
    // {
    //     entities = _entiies;
    //     entityDropDown.choices = Array.ConvertAll(entities, (entity) => entity.name).ToList();
    //     entityDropDown.index = 0;
    // }

    // private Object[] entities;
    // private TreeView treeView;

    protected override void OnEnable()
    {
        base.OnEnable();
        Selection.selectionChanged += OnSelectionChangeAllTheTime;
    }

    void OnPrefabStateClosing(PrefabStage obj)
    {
        if (obj.prefabContentsRoot == lastSelectedEntity)
            Close();
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        Selection.selectionChanged -= OnSelectionChangeAllTheTime;

        // PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
        // PrefabStage.prefabStageClosing -= OnPrefabStateClosing;
        
        if ( tree != null )
        {
            tree.Dispose();
            tree = null;
        }

        // Debug.Log("OnDisable" + titleContent.text + lastSelectedEntity);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // Debug.Log("Destroyed:" + titleContent.text);
    }



    void UpdateView()
    {
        DisplayEntity(lastSelectedEntity);
    }

    void DisplayEntity(GameObject gObj)
    {
        if (currentWindow != this)
            return;
        Debug.Log("display entity" + gObj);
        // objectField.value = gObj;
        var comps = new List<Component>();
        
        if (gObj == null)
            UpdateHierarchy(comps);
        else
        {
            gObj.GetComponentsInChildren<Component>(true,comps);
            var filteredComps = FilterComponent(comps);
            if (filteredComps == null)
            {
                return;
            }
            
            Debug.Log("result:" + filteredComps.Count);
            UpdateHierarchy(filteredComps);    
            
            // treeView.SetRootItems(TreeRoots);
            // treeView.RefreshItems();
            // treeView.reorderable = true;
        }
        
    }

    void DrawHierarchyList()
    {
        Debug.Log("DrawHierarchy");
        DisplayEntity(lastSelectedEntity);
    }
    //FuzzySearch names of gameobjects
     
    

    List<Component> FilterComponent(List<Component> comps)
    {
        if(comps == null || comps.Count == 0)
            return null;
        Debug.Log("Filter Components" + comps.Count);
        try
        {
            var filteredComps = comps.Where((comp) =>
            {
                if (comp is Transform)
                    return false;
                //TODO: 這個很貴, 改用dictionary
                //要把entry轉換成dictionary
                foreach (var typeFilterEntry in TypeFilterEntries)
                    if (typeFilterEntry.Enabled)
                        if (typeFilterEntry.type.IsInstanceOfType(comp))
                            return true;
                
                // if (filteringType.IsInstanceOfType(comp))
                //     return true;
                return false;
            }).ToList();
            var typeFiltered = FilterSearchUtility.SearchForComponentType(filteredComps, _textField.value);
            var nameFiltered = FilterSearchUtility.SearchGameObjectsByTerm(filteredComps, _textField.value);
            //combine two list
            Debug.Log("typeFiltered:" + typeFiltered.Count());
            Debug.Log("nameFiltered:" + nameFiltered.Count());
            var result = typeFiltered.Union(nameFiltered).ToList();
            return result;
            // return filteredComps;
            // }
        }
        catch (Exception e)
        {
            Debug.Log("filteringType Exception"+e);
            return null;
        }

        // Debug.Log("filteringType: null"+FilterTypeDropDown.value);
        return null;
    }

    private ListView listView;
    private TreeView _treeView;
    private ToolbarSearchField _textField;

    // void OnPrefabStageChange()
    // {
    //     
    //     var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
    //     if (prefabStage == null)
    //         return;
    //     if (lastPrefabRoot == prefabStage.prefabContentsRoot)
    //         return;
    //    
    // }
    
    //find nearest parent prefab of selection
    static GameObject FindNearestPrefab()
    {
        var selection = Selection.activeGameObject;
        if (selection == null)
            return null;
        var entity = selection.GetComponentInParent(ModuleEntityType);
        
        //有entity
        if (entity != null)
        {
            // return entity.gameObject;
            //TODO: 什麼時候要往外找？
            var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(entity.gameObject);
            if (prefabRoot != null)
                return prefabRoot;
            return entity.gameObject;
            // return selection;
        }
        else
        {
            return selection;
        }
    }

    void SelectNearestPrefab()
    {
        //TODO: 如果在prefab mode....?

        var nearestPrefab = FindNearestPrefab();
        Debug.Log("SelectOutermostPrefab" + nearestPrefab);
        //TODO: lock邏輯要更細
        //TODO: 解開lock就要彈回來
        if (PrefabStageUtility.GetCurrentPrefabStage())
        {
            Prefab = nearestPrefab;
            DisplayEntity(lastSelectedEntity);
        }
        else
        {
            //Scene
            if (lastSelectedEntity != nearestPrefab)
            {
                var guidComp = nearestPrefab.GetComponent<GuidComponent>();
                if (guidComp == null)
                {
                    guidComp = Undo.AddComponent<GuidComponent>(nearestPrefab);
                    // guidComp = currentPrefab.AddComponent<GuidComponent>();
                }

                Debug.Log("SelectOutermostPrefab" + guidComp);
                MonoRef = new GuidReference(guidComp);
                DisplayEntity(lastSelectedEntity);
            }
        }
        
    }

    private void OnSelectionChangeAllTheTime()
    {
        //TODO: 
        // if(isPrefabMode)
        //     UpdateEntities();
        // Debug.Log("OnSelectionChange" + _lastSelectedEntity);
        if (Selection.activeGameObject == null)
            return;
        var selectedPrefab = PrefabUtility.GetNearestPrefabInstanceRoot(Selection.activeGameObject);

        if (lastSelectedEntity == null)
            Close();
        //find nearest parent prefab of selection
        if (selectedPrefab == lastSelectedEntity || Selection.activeGameObject == lastSelectedEntity)
        {
            if (currentWindow != this)
                ShowTab();
            Debug.Log("Selected Same Entity" + lastSelectedEntity);
        }
            
        
    }

   
    void AddANewContextMenu(VisualElement element)
    {
        // The manipulator handles the right click and sends a ContextualMenuPopulateEvent to the target element.
        // The callback argument passed to the constructor is automatically registered on the target element.
        element.AddManipulator(new ContextualMenuManipulator((evt) =>
        {
            evt.menu.AppendAction("First menu item", (x) => Debug.Log("First!!!!"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Second menu item", (x) => Debug.Log("Second!!!!"), DropdownMenuAction.AlwaysEnabled);
        }));
    }

    void InsertIntoAnExistingMenu(VisualElement element)
    {
        element.RegisterCallback<ContextualMenuPopulateEvent>((evt) =>
        {
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Another action", (x) => Debug.Log("Another Action!!!!"), DropdownMenuAction.AlwaysEnabled);
        });
    }
    
    void ReplaceContextMenu(VisualElement element)
    {
        element.RegisterCallback<ContextualMenuPopulateEvent>((evt) =>
        {
            evt.menu.MenuItems().Clear();
            evt.menu.AppendAction("The only action", (x) => Debug.Log("The only action!"), DropdownMenuAction.AlwaysEnabled);
        });
    }
    void UpdateHierarchy(List<Component> list)
    {
        //TODO: 是不是不用每次都要重新畫？
        listView.bindItem = (item, index) =>
        {
            
            if (index < list.Count)
            {
                var label = item as DraggableLabel;
                
                var comp = list[index];
                if (label != null && comp != null)
                {
                    label.bindComp = comp;
           
                    AddANewContextMenu(label);
                    InsertIntoAnExistingMenu(label);
                }
            }
            else
            {
                Debug.Log("Length:" + list.Count + "index:" + index);
            }
            
        };
        listView.selectionType = SelectionType.Multiple;
        listView.Clear();
        listView.itemsSource = list;
        listView.RefreshItems();
        // Debug.Log("Refresh ListView:" + list.Count);
    }

    private IList<TreeViewItemData<GameObject>> TreeRoots
    {
        get
        {
            if (lastSelectedEntity == null)
                return new List<TreeViewItemData<GameObject>>(0);
            var nodes = lastSelectedEntity.GetComponentsInChildren<Transform>();
            int id = 0;
            var roots = new List<TreeViewItemData<GameObject>>(nodes.Length);
            foreach (var t in nodes)
            {
                var comps = t.GetComponents<Component>();
                
                var group = new List<TreeViewItemData<GameObject>>(comps.Length);
                foreach (var comp in comps)
                {
                    group.Add(new TreeViewItemData<GameObject>(id++, comp.gameObject));
                }

                roots.Add(new TreeViewItemData<GameObject>(id++, t.gameObject, group));
            }
            return roots;
        }
    }
   
    class MouseEventLogger : Manipulator
    {
        protected override void RegisterCallbacksOnTarget()
        {
            // By default you handle events after children
            target.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            target.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            // Capture phase lets you handle events before children
            target.RegisterCallback<MouseUpEvent>(OnMouseUpEvent, TrickleDown.TrickleDown);
            target.RegisterCallback<MouseDownEvent>(OnMouseDownEvent, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDownEvent);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUpEvent, TrickleDown.TrickleDown);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDownEvent, TrickleDown.TrickleDown);
        }

        void OnMouseUpEvent(MouseEventBase<MouseUpEvent> evt)
        {
            Debug.Log("Receiving " + evt + " in " + evt.propagationPhase + " for target " + evt.target);
        }

        void OnMouseDownEvent(MouseEventBase<MouseDownEvent> evt)
        {
            Debug.Log("Receiving " + evt + " in " + evt.propagationPhase + " for target " + evt.target);
        }
    }
}
}
