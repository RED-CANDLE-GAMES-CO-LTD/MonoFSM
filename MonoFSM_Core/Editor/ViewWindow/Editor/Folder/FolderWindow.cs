using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

//[]: Expand Folder icon


    public class FolderWindow : OdinEditorWindow
    {
        private TreeView _treeView;
        private ToolbarSearchField searchField;

        [MenuItem("Window/FrameFolder #%_T")]
        public static void ShowWindow()
        {
            Debug.Log("Show FrameFolder Window");
            if (Selection.activeObject == null) return;
            //check if it is a folder
            if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject)) == false) return;
            var title = Selection.activeObject.name;
            var window = CreateInstance<FolderWindow>();

            var content = EditorGUIUtility.IconContent(EditorResources.folderIconName);
            content.text = title;
            window.titleContent = content;
            window.folderObj = Selection.activeObject;
            window.Show();
            window.searchField.Focus();
        }

        protected override void OnGUI()
        {
        }

        private string folderPath => AssetDatabase.GetAssetPath(folderObj);

        private void Search(string value)
        {
            // Debug.Log("Search" + value);
            var guids = AssetDatabase.FindAssets(value, new[] { folderPath });
            var items = new TreeViewItemData<Object>[guids.Length];
            for (var i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                items[i] = new TreeViewItemData<Object>(obj.GetInstanceID(), obj);
            }

            _treeView.SetRootItems(items);
            _treeView.RefreshItems();
        }

        private void CreateGUI()
        {
            var container = new IMGUIContainer();
            container.onGUIHandler = () =>
            {
                var tree = PropertyTree.Create(this);
                tree.Draw();
            };
            rootVisualElement.Add(container);

            searchField = new ToolbarSearchField();
            searchField.RegisterValueChangedCallback((evt) =>
            {
                // _treeView.searchString = evt.newValue;
            });
            searchField.style.width = StyleKeyword.Auto;
            // searchField.style.alignSelf = Align.Auto;
            // searchField.style.flexDirection = FlexDirection.Row;
            // searchField.style.flexGrow = 1;
            searchField.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue == "")
                {
                    _treeView.SetRootItems(new[] { rootTreeViewItemData });
                    _treeView.RefreshItems();
                }
                else
                {
                    Search(evt.newValue);
                }
            });
            rootVisualElement.Add(searchField);
            _treeView = new TreeView()
            {
                makeItem = () => new DraggableLabel()
                {
                    focusable = true,
                    OnSelect = () =>
                    {
                        // if(onSelectObjects == null)
                        //     return;

                        var objs = _treeView.selectedItems.Select((obj) => obj as Object).ToArray();
                        // Debug.Log("ListViewItem OnSelect" + objs[0]);
                        Selection.objects = objs;

                        //TODO: 2021版是不是不能這樣選？
                        // Selection.activeGameObject = objs[0];
                    }
                },
                
                fixedItemHeight = 16,
                showBorder = true,
                bindItem = (element, index) =>
                {
                    var label = element as DraggableLabel;
                    label.bindObj = _treeView.GetItemDataForIndex<Object>(index);
                    
                }
            };
            
            // _treeView.reorderable = true;

            // _treeView.selectionChanged += objs =>
            // {
            //     Debug.Log("onSelectionChange");
            //     // Debug.Log(objs[0]);
            //     objs = objs.Where((obj) => obj != null).ToArray();
            //     Selection.objects = objs.Select((obj) => (Object)obj).ToArray();
            // };
            _treeView.itemsChosen += objs =>
            {
                objs = objs.Where((obj) => obj != null).ToArray();
                //open asset
                if (objs.Count() == 1)
                {
                    var array = objs.ToArray();
                    var obj = array[0] as Object;

                    if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)) == false)
                        AssetDatabase.OpenAsset(obj);
                }
            };
            
            
            _treeView.RegisterCallback<DragUpdatedEvent>((evt) =>
            {
                //別人拖進來？
                var objs = DragAndDrop.objectReferences;
                if (objs.Length == 0) return;
                var obj = objs[0];
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)) == false) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                evt.StopPropagation();
            });
            _treeView.RegisterCallback<DragPerformEvent>((evt) =>
            {
                var objs = DragAndDrop.objectReferences;
                if (objs.Length == 0) return;
                var obj = objs[0];
                if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)) == false) return;
                DragAndDrop.AcceptDrag();
                evt.StopPropagation();
            });
            _treeView.RegisterCallback<DragExitedEvent>((evt) =>
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
            });


            rootVisualElement.Add(_treeView);
            FetchAllUnderFolder();
        }

        // private void OnHierarchyChange()
        // {
        //     UpdateTreeView();
        // }

        private void OnProjectChange()
        {
            FetchAllUnderFolder();
        }

        private void FetchAllUnderFolder()
        {
            // Debug.Log(folderObj);
            // var id = 0;
            // var assetPath = AssetDatabase.GetAssetPath(folderObj);
            // folderPath = assetPath;


            // var files = AssetDatabase.FindAssets("", new[] { assetPath });
            // var paths = files.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            // //nested folder
            // var folders = paths.Where((path) => AssetDatabase.IsValidFolder(path)).ToArray();
            // //files under folder
            // var assets = paths.Where((path) => AssetDatabase.IsValidFolder(path) == false).ToArray();
            // foreach (var folder in folders)
            // {
            //     foreach (var asset in assets)
            //     {
            //         if(asset.Contains(folder))
            //         {
            //             Debug.Log("asset:" + asset);
            //         }
            //     }
            // }

            processedAssets = new HashSet<string>();
            rootTreeViewItemData = CreateTreeViewDataOfGameObject(folderObj);
            _treeView.SetRootItems(new[] { rootTreeViewItemData });
            _treeView.RefreshItems();
            _treeView.ExpandAll();
        }

        // public string folderPath;
        [HideInInspector] public Object folderObj;
        private HashSet<string> processedAssets;

        private TreeViewItemData<Object> CreateTreeViewDataOfGameObject(Object asset)
        {
            var childTreeItemData = new List<TreeViewItemData<Object>>();
            var assetPath = AssetDatabase.GetAssetPath(asset);

            var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

            if (assetType == typeof(DefaultAsset))
            {
                var guids = AssetDatabase.FindAssets("", new[] { assetPath });
                foreach (var guid in guids)
                {
                    if (processedAssets.Contains(guid)) continue;
                    processedAssets.Add(guid);

                    var projectItem =
                        AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object));
                    if (projectItem.GetType() == typeof(DefaultAsset))
                        //資料夾
                        childTreeItemData.Add(
                            CreateTreeViewDataOfGameObject(projectItem));
                    else
                        //檔案
                        childTreeItemData.Add(new TreeViewItemData<Object>(projectItem.GetInstanceID(), projectItem));
                }


                // var folders = AssetDatabase.GetSubFolders(assetPath);
                // //get files under folder
                // foreach (var folder in folders)
                // {
                //     if (processedAssets.Contains(folder)) continue;
                //     processedAssets.Add(folder);
                //     Debug.Log("folder:" + folder);
                //     childTreeItemData.Add(
                //         CreateTreeViewDataOfGameObject(AssetDatabase.LoadAssetAtPath<Object>(folder)));
                // }
                //
                //
                // var filePaths = AssetDatabase.FindAssets("", new[] { assetPath });
                // foreach (var guid in filePaths)
                // {
                //     // Debug.Log("guid:" + guid);
                //
                //     var path = AssetDatabase.GUIDToAssetPath(guid);
                //     //check if path is in folder
                //     if (processedAssets.Contains(path)) continue;
                //     processedAssets.Add(path);
                //     Debug.Log("path:" + path);
                //     var child = AssetDatabase.LoadAssetAtPath<Object>(path);
                //     if (child == null) continue;
                //     // childTreeItemData.Add(CreateTreeViewDataOfGameObject(child, ref id));
                //     childTreeItemData.Add(new TreeViewItemData<Object>(child.GetInstanceID(), child, null));
                // }
            }


            //file
            var item = new TreeViewItemData<Object>(asset.GetInstanceID(), asset, childTreeItemData);
            return item;


            // for (int i = 0; i < ; i++)
            // {
            //    var child = AssetDatabase.
            //     childTreeItemData.Add(CreateTreeViewDataOfGameObject(child, ref id));
            // }

            // var item = new TreeViewItemData<Object>(asset.GetInstanceID(), asset, childTreeItemData);
            //
            // return item;
        }

        private TreeViewItemData<Object> rootTreeViewItemData;
    }
