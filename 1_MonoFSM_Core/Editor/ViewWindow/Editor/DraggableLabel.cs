using System;
using MonoFSM.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class DraggableLabel : Label
{
    GameObject _bindGameObject;

    public GameObject BindGo
    {
        get => _bindGameObject;
        set
        {
            _bindGameObject = value;
            var labelText = _bindGameObject.name;

            var currentSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);

            if (EditorGUIUtility.isProSkin)
                currentSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
            //[]: 改字的顏色，會造成選取後的顏色被直接蓋掉，用style?

            // Get the text color for labels

            // label.style.color = currentSkin.label.normal.textColor;
            
            label.style.backgroundColor = Color.clear;
            commentLabel.text = "";
            commentLabel.style.display = DisplayStyle.None;
            if (_bindGameObject.TryGetComponent<Note>(out var note))
            {
                commentLabel.text = note.note;
                var color = note.bgColor;
                color.a = 0.3f;
                commentLabel.style.backgroundColor = color;
                commentLabel.style.display = DisplayStyle.Flex;
            }

            label.text = labelText;
            label.SetEnabled(_bindGameObject.activeInHierarchy);
            
            subIconImage.image = null;
            // var icon = AssetPreview.GetMiniThumbnail(_bindGameObject);
            // var texture = EditorGUIUtility.GetIconForObject(value);
            var icon = EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image as Texture2D;
            if (PrefabUtility.IsAnyPrefabInstanceRoot(_bindGameObject)) //我是prefab 的root
            {
                // label.style.color = prefabLabelStyle.normal.textColor;
                icon = EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
            }
            

            if (PrefabUtility.IsAddedGameObjectOverride(_bindGameObject)) //有加號，新增的GameObject
            {
                var addIcon = EditorGUIUtility.IconContent("PrefabOverlayAdded Icon").image as Texture2D;
                subIconImage.image = addIcon;

                // label.style.color = currentSkin.label.normal.textColor;
            }

            else if (PrefabUtility.IsPartOfAnyPrefab(_bindGameObject)) //我是prefab的一部分
            {
                subIconImage.image = null;
                // label.style.color = prefabLabelColor;
                var overrides = PrefabUtility.GetObjectOverrides(_bindGameObject);

                // Debug.Log(_bindGameObject + ":override.count:" + overrides.Count);

                //TODO: 這個可以cache起來？但修改又要馬上update
                //TODO: 還有他的component也要檢查
                //如果有override在這個gameobject上，給個icon
                if (overrides.Count > 0)
                    foreach (var objectOverride in overrides)
                        if (objectOverride.instanceObject == _bindGameObject)
                        {
                            // Debug.Log("objectOverride.instanceObject" + objectOverride.instanceObject,
                            // objectOverride.instanceObject);
                            var modifyIcon =
                                EditorGUIUtility.IconContent("PrefabOverlayModified Icon").image as Texture2D;
                            subIconImage.image = modifyIcon;
                            break;
                        }
            }
            else
            {
                subIconImage.image = null;
            }
            
            iconImage.image = icon;
            iconImage.SetEnabled(_bindGameObject.activeInHierarchy);
            subIconImage.SetEnabled(_bindGameObject.activeInHierarchy);
        }
    }

    private static GUIStyle prefabLabelStyle =>
        EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).GetStyle("PR PrefabLabel");

    private Color prefabLabelColor => prefabLabelStyle.normal.textColor;

    private Object _obj;

    public Object bindObj
    {
        get => _obj;
        set
        {
            _obj = value;
            var labelText = _obj.name;
            label.text = labelText;
            label.SetEnabled(true);
            iconImage.image = AssetPreview.GetMiniThumbnail(_obj);
            // icon = EditorGUIUtility.IconContent("FolderOpened Icon").image as Texture2D;
            //EditorGUIUtility.ObjectContent(null, _obj.GetType()).image as Texture2D;
        }
    }
    //new("PR PrefabLabel");
    public Component bindComp
    {
        get => _comp;
        set
        {
            _comp = value;
            var t = _comp.name;
            if (_comp.TryGetComponent<ComponentNote>(out var note)) t += " " + note.Note;
            // text = t;
            var type = _comp.GetType();
            // label.text = t;
            
            
            label.text = t + ": " + type.Name;
            label.style.color = Color.black;

            //grey out if not active
            if (!_comp.gameObject.activeInHierarchy)
                label.style.color = Color.grey;
            else
                label.style.color = Color.black;
            
            
            // style.backgroundColor = new StyleColor(Color.green);
            //顏色，icon, 
            //TODO: show Component Type Name toggle
            // var icon = EditorGUIUtility.GetIconForObject(_comp);
            // var icon = EditorIconUtility.GetIcon(_comp);
            var icon = AssetPreview.GetMiniThumbnail(_comp);

            // var texture = EditorGUIUtility.GetIconForObject(value);
            // var icon = EditorGUIUtility.ObjectContent(null, type).image as Texture2D;
            iconImage.image = icon;
        }
    }

    private Component _comp;
    // public static string s_DragDataType = "DraggableLabel";
 
    private bool m_GotMouseDown;
    private Vector2 m_MouseOffset;
    public Action OnSelect;
    public DraggableLabel()
    {
        // RegisterCallback<KeyUpEvent>(evt =>
        // {
        //     if(evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.DownArrow)
        //         OnSelect?.Invoke();
        //    
        //     
        //     // Debug.Log("KeyDownEvent"+bindComp);
        // });
        //
        //
        //
        RegisterCallback<PointerDownEvent>(OnPointerDownEvent);
        RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
        RegisterCallback<PointerUpEvent>(OnPointerUpEvent);
        // RegisterCallback<ClickEvent>(evt =>
        // {
        //     // EditorGUIUtility.PingObject(bindComp.gameObject);    
        //
        //
        //     Selection.activeGameObject = BindGo;
        // });

        iconImage = new Image()
        {
            style =
            {
                //icon align left, and text align right
                // alignSelf = Align.FlexStart,
                marginRight = 0,
                marginLeft = 0,
                width = 16,
                minWidth = 16
                // height = 20
            }
        };
        subIconImage = new Image()
        {
            style =
            {
                position = Position.Absolute,
                //icon align left, and text align right
                // alignSelf = Align.FlexStart,

                marginRight = 0,
                marginLeft = 0,
                width = 16,
                minWidth = 16,
                height = 16
            }
        };
//text align after icon

        iconImage.pickingMode = PickingMode.Ignore;
        Add(iconImage);
        subIconImage.pickingMode = PickingMode.Ignore;
        Add(subIconImage);

        label = new Label();
        
        Add(label);
        label.focusable = false;
        label.pickingMode = PickingMode.Ignore;
        commentLabel = new Label();
        commentLabel.pickingMode = PickingMode.Ignore;
        Add(commentLabel);
        // commentLabel.style.flexDirection = FlexDirection.RowReverse;
        commentLabel.style.alignContent = Align.FlexEnd;
        commentLabel.style.alignSelf = Align.FlexEnd;
        // commentLabel.style.marginLeft = auto
        commentLabel.style.flexShrink = 0;

        commentLabel.style.marginLeft = StyleKeyword.Auto;
        commentLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        
        renameField = new TextField();
        renameField.style.flexGrow = 1;
        renameField.style.display = DisplayStyle.None;
        renameField.AddManipulator(new KeyboardNavigationManipulator((operation, evt) => { evt.StopPropagation(); }));
        renameField.RegisterCallback<PointerDownEvent>(evt =>
        {
            Debug.Log("PointerDownEvent");
            evt.StopPropagation();
        });
        renameField.RegisterCallback<PointerMoveEvent>(evt => { evt.StopPropagation(); });
        renameField.RegisterCallback<PointerUpEvent>(evt =>
        {
            Debug.Log("PointerUpEvent");
            evt.StopPropagation();
        });
        // renameField.style.display = DisplayStyle.None;

        renameField.RegisterCallback<FocusOutEvent>(evt =>
        {
            Debug.Log("OnFocusOutEvent");
            label.text = renameField.value;
            Undo.RecordObject(BindGo, "Rename");
            BindGo.name = renameField.value;
            label.style.display = DisplayStyle.Flex;
            renameField.style.display = DisplayStyle.None;
            // Remove(renameField);
            // Add(label);

            Focus();
        });
        Add(renameField);
        
        //flex
        style.flexDirection = FlexDirection.Row;
        
        
        
    }

    public void Rename()
    {
        // Remove(label);
        label.style.display = DisplayStyle.None;
        renameField.SetValueWithoutNotify(BindGo.name);
        renameField.style.display = DisplayStyle.Flex;
        // Add(renameField);
        // renameField.focusable = true;
        renameField.Focus();
    }

    TextField renameField;

    private Image iconImage;
    private Image subIconImage;
    private Label label;
    private Label commentLabel;
 
    void OnPointerDownEvent(PointerDownEvent e)
    {
        if (e.target == this && e.isPrimary && e.button == 0)
        {
            deltaTotal = Vector3.zero;
            m_GotMouseDown = true;
            m_MouseOffset = e.localPosition;
        }
    }

    private Vector3 deltaTotal;

    private void OnPointerMoveEvent(PointerMoveEvent e)
    {
        deltaTotal += e.deltaPosition;
        if (m_GotMouseDown && e.isPrimary && e.pressedButtons == 1 && deltaTotal.magnitude > 5)
        {
            StartDraggingBox();
            m_GotMouseDown = false;
        }
        // if (m_GotMouseDown && e.isPrimary && e.pressedButtons == 1)
        // {
        //     StartDraggingBox();
        //     m_GotMouseDown = false;
        // }
    }

    void OnPointerLeaveEvent(PointerLeaveEvent e)
    {
        if (m_GotMouseDown && e.isPrimary && e.pressedButtons == 1)
        {
            StartDraggingBox();
            m_GotMouseDown = false;
        }
       
    }
 
    void OnPointerUpEvent(PointerUpEvent e)
    {
        if (m_GotMouseDown && e.isPrimary && e.button == 0)
        {
            m_GotMouseDown = false;
        }

        OnSelect?.Invoke();
    }
 
    public void StartDraggingBox()
    {
        // DragAndDrop.PrepareStartDrag();
        // DragAndDrop.SetGenericData(s_DragDataType, this);
        // DragAndDrop.StartDrag(text);
        
        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        // Debug.Log("SetGenericData:"+comp.GetType().Name);
        DragAndDrop.PrepareStartDrag();
        // DragAndDropWindowTarget
        DragAndDrop.objectReferences = new Object[] { bindObj }; //拖拉的時候，帶什麼內容
        DragAndDrop.SetGenericData(bindObj.GetType().Name, bindObj);
        DragAndDrop.StartDrag(bindObj.name);
        
    }
 
    // public void StopDraggingBox(Vector2 mousePosition)
    // {
    //     style.top = -m_MouseOffset.y + mousePosition.y;
    //     style.left = -m_MouseOffset.x + mousePosition.x;
    // }
}
