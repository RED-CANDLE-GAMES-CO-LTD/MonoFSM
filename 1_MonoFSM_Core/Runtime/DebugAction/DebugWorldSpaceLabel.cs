using MonoDebugSetting;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _0_MonoDebug.Gizmo
{
    [ExecuteAlways]
    public class DebugWorldSpaceLabel : AbstractWorldFollowBehaviour
    {
        protected override string DescriptionTag => "DebugLabel";
#if UNITY_EDITOR
        public Vector3 offset = new Vector3(0, 2f, 0);

        [OnValueChanged(nameof(ApplyStyle))] public int fontSize = 24;
        [OnValueChanged(nameof(ApplyStyle))] public Color fontColor = Color.green;

        public enum OutlineMode
        {
            None,
            Shadow,
            Outline
        }

        [Header("可讀性設定")]
        [Tooltip("外框模式。Shadow 使用 text-shadow，Outline 使用描邊")]
        [OnValueChanged(nameof(ApplyStyle))]
        public OutlineMode outlineMode = OutlineMode.Shadow;

        [ShowIf("@outlineMode != OutlineMode.None")]
        [Tooltip("外框顏色")]
        [OnValueChanged(nameof(ApplyStyle))]
        public Color outlineColor = Color.black;

        [ShowIf("@outlineMode != OutlineMode.None")] [Tooltip("外框透明度")] [Range(0f, 1f)]
        [OnValueChanged(nameof(ApplyStyle))]
        public float outlineAlpha = 1f;

        [ShowIf("outlineMode", OutlineMode.Shadow)]
        [Tooltip("陰影像素偏移")]
        [Range(1, 4)]
        [OnValueChanged(nameof(ApplyStyle))]
        public int shadowOffsetPx = 1;

        [ShowIf("outlineMode", OutlineMode.Outline)]
        [Tooltip("描邊寬度")]
        [Range(0.1f, 3f)]
        [OnValueChanged(nameof(ApplyStyle))]
        public float outlineWidth = 1f;

        [Space] [Tooltip("使用半透明背景")] [OnValueChanged(nameof(ApplyStyle))]
        public bool useBackground = false;

        [ShowIf("useBackground")] [OnValueChanged(nameof(ApplyStyle))]
        public Color backgroundColor = new Color(0, 0, 0, 0.7f);

        [Space] [Tooltip("在 Scene 視圖中可點擊選取")] public bool clickableInScene = true;

        // --- UI Toolkit (由 DebugLabelOverlay 管理) ---
        [ShowInInspector] VisualElement _container;
        [ShowInInspector] Label _label;
        bool _uiReady;

        // --- SceneView 用 GUIStyle (保留) ---
        GUIStyle _gizmoStyle;
        GUIStyle _gizmoOutlineStyle;
        GUIStyle _backgroundStyle;

        [ShowInDebugMode] Rigidbody _bindingRigidbody;

        public override Transform FollowTransform => _followTarget != null ? _followTarget
            : _bindingRigidbody != null ? _bindingRigidbody.transform : null;

        [HideIf("_externalVariable", null, false)] [Required] [AutoParent]
        public AbstractMonoVariable _variable;

        public AbstractMonoVariable _externalVariable;

        [ShowInInspector]
        public AbstractMonoVariable currentVariable
        {
            get
            {
                if (_externalVariable != null)
                    return _externalVariable;
                return _variable;
            }
        }

        public override string Description => currentVariable?.Description;

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying)
                return;
            _bindingRigidbody = ParentEntity?.GetCompCache<Rigidbody>();
            if (_bindingRigidbody == null)
                _bindingRigidbody = GetComponentInParent<Rigidbody>();
            if (_bindingRigidbody == null)
                Debug.LogError("DebugWorldSpaceLabel 找不到 Rigidbody，請確認 ParentEntity 有 Rigidbody 組件",
                    this);
            gameObject.layer = LayerMask.NameToLayer("World UI");
        }

        // ========== Lifecycle ==========

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RegisterOverlayLabel();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            UnregisterOverlayLabel();
        }

        void Update()
        {
            if (FollowTransform != null)
                transform.position = FollowTransform.position;

            UpdateOverlayLabel();
        }

        // ========== Screen Space Overlay Label ==========

        void RegisterOverlayLabel()
        {
            // EnsureInitialized 會觸發，但 root 可能還沒準備好
            // 此時 _uiReady = false，Update 中會自動重試
            DebugLabelOverlay.EnsureReady();
            TryBuildLabel();
        }

        [ShowInInspector] private GameObject overlay => DebugLabelOverlay._overlayGo;

        void TryBuildLabel()
        {
            if (_uiReady) return;

            var root = DebugLabelOverlay.GetRoot();
            if (root == null) return;

            _container = new VisualElement();
            _container.style.position = Position.Absolute;
            _container.style.alignItems = Align.Center;
            _container.style.justifyContent = Justify.Center;
            _container.style.paddingLeft = 5;
            _container.style.paddingRight = 5;
            _container.style.paddingTop = 2;
            _container.style.paddingBottom = 2;
            _container.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            _container.pickingMode = PickingMode.Position;

            _label = new Label();
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _label.style.whiteSpace = WhiteSpace.NoWrap;
            _label.pickingMode = PickingMode.Position;

            _container.Add(_label);
            root.Add(_container);

            // GameView 點擊選取
            _container.RegisterCallback<ClickEvent>(OnLabelClicked);

            _uiReady = true;
            ApplyStyle();
        }

        void UnregisterOverlayLabel()
        {
            _container?.RemoveFromHierarchy();
            _container = null;
            _label = null;
            _uiReady = false;
        }

        void UpdateOverlayLabel()
        {
            // Lazy init: root 可能在之後幾幀才準備好
            if (!_uiReady)
            {
                TryBuildLabel();
                if (!_uiReady) return;
            }

            bool show = RuntimeDebugSetting.IsDebugMode;
            _container.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;

            var cam = Camera.main;
            if (cam == null)
            {
                _container.style.display = DisplayStyle.None;
                return;
            }

            // 世界座標 → 螢幕座標
            var worldPos = transform.position + offset;
            var screenPos = cam.WorldToScreenPoint(worldPos);

            // 在攝影機背後
            if (screenPos.z <= 0)
            {
                _container.style.display = DisplayStyle.None;
                return;
            }

            // 螢幕座標轉 UI Toolkit 座標（Y 軸反轉）
            _container.style.left = screenPos.x;
            _container.style.top = cam.pixelHeight - screenPos.y;

            // 更新文字
            _label.text = currentVariable != null
                ? currentVariable.Description + ": " + currentVariable.StringValue
                : "";

            // 更新動態顏色
            _label.style.color = GetDynamicColor();
        }

        void ApplyStyle()
        {
            if (!_uiReady) return;

            _label.style.fontSize = fontSize;
            _label.style.color = GetDynamicColor();

            var oc = GetOutlineColor();

            // 重置
            _label.style.textShadow = new TextShadow();
            _label.style.unityTextOutlineColor = Color.clear;
            _label.style.unityTextOutlineWidth = 0;

            switch (outlineMode)
            {
                case OutlineMode.Shadow:
                    _label.style.textShadow = new TextShadow
                    {
                        offset = new Vector2(shadowOffsetPx, shadowOffsetPx),
                        blurRadius = 0,
                        color = oc
                    };
                    break;
                case OutlineMode.Outline:
                    _label.style.unityTextOutlineColor = oc;
                    _label.style.unityTextOutlineWidth = outlineWidth;
                    break;
            }

            _container.style.backgroundColor = useBackground ? backgroundColor : Color.clear;
        }

        void OnLabelClicked(ClickEvent evt)
        {
            if (evt.commandKey)
                SelectVariableGameObject();
        }

        // ========== 共用 ==========

        Color GetDynamicColor()
        {
            if (currentVariable is VarBool varBool)
                return varBool.Value ? Color.green : Color.red;
            return currentVariable != null ? Color.yellow : fontColor;
        }

        Color GetOutlineColor()
        {
            var c = outlineColor;
            c.a *= outlineAlpha;
            return c;
        }

        void SelectVariableGameObject()
        {
            var targetGo = currentVariable != null ? currentVariable.gameObject : gameObject;
            Selection.activeGameObject = targetGo;
            EditorGUIUtility.PingObject(targetGo);
        }

        // ========== SceneView (保留 Gizmos + Handles) ==========

        GUIStyle gizmoStyle
        {
            get
            {
                if (_gizmoStyle == null)
                {
                    _gizmoStyle = new GUIStyle();
                    _gizmoStyle.alignment = TextAnchor.MiddleCenter;
                }
                _gizmoStyle.fontSize = fontSize;
                _gizmoStyle.normal.textColor = GetDynamicColor();
                return _gizmoStyle;
            }
        }

        GUIStyle gizmoOutlineStyle
        {
            get
            {
                if (_gizmoOutlineStyle == null)
                    _gizmoOutlineStyle = new GUIStyle(gizmoStyle);
                _gizmoOutlineStyle.fontSize = fontSize;
                _gizmoOutlineStyle.alignment = TextAnchor.MiddleCenter;
                _gizmoOutlineStyle.normal.textColor = GetOutlineColor();
                return _gizmoOutlineStyle;
            }
        }

        GUIStyle backgroundGUIStyle
        {
            get
            {
                if (_backgroundStyle == null)
                {
                    _backgroundStyle = new GUIStyle();
                    _backgroundStyle.normal.background = Texture2D.whiteTexture;
                }

                return _backgroundStyle;
            }
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (!RuntimeDebugSetting.IsDebugMode || !clickableInScene)
                return;

            var cam = sceneView.camera;
            if (cam == null)
                return;

            Vector3 worldPosition = transform.position + offset;
            Vector3 sp = cam.WorldToScreenPoint(worldPosition);
            if (sp.z <= 0)
                return;

            string displayText = "";
            if (currentVariable != null)
                displayText = currentVariable.Description + ": " + currentVariable.StringValue;

            Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPosition);
            Vector2 size = gizmoStyle.CalcSize(new GUIContent(displayText));
            var rect = new Rect(guiPos.x - size.x * 0.5f, guiPos.y - size.y * 0.5f, size.x, size.y);

            Rect clickRect = useBackground
                ? new Rect(rect.x - 5, rect.y - 2, rect.width + 10, rect.height + 4)
                : rect;

            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && e.command &&
                clickRect.Contains(e.mousePosition))
            {
                e.Use();
                SelectVariableGameObject();
            }

            if (clickRect.Contains(e.mousePosition))
            {
                Handles.BeginGUI();

                var hintStyle = new GUIStyle(EditorStyles.helpBox);
                hintStyle.fontSize = 10;
                hintStyle.alignment = TextAnchor.MiddleCenter;
                var hintRect = new Rect(clickRect.x, clickRect.yMax + 2, clickRect.width, 16);
                var hintText = e.command ? "Click 選取" : "Command+Click 選取";

                if (e.command)
                {
                    EditorGUI.DrawRect(hintRect, new Color(0.2f, 0.6f, 0.9f, 1f));
                    hintStyle.normal.textColor = Color.white;
                }

                GUI.Label(hintRect, hintText, hintStyle);
                Handles.EndGUI();

                if (e.command)
                    EditorGUIUtility.AddCursorRect(clickRect, MouseCursor.Link);

                sceneView.Repaint();
            }
        }

        void OnDrawGizmos()
        {
            if (!RuntimeDebugSetting.IsDebugMode)
                return;

            var labelPosition = transform.position + offset;

            Gizmos.color = GetDynamicColor();
            Gizmos.DrawSphere(labelPosition, 0.1f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, labelPosition);

            string displayText = "";
            if (currentVariable != null)
                displayText = currentVariable.Description + ": " + currentVariable.StringValue;

            DrawSceneLabel(labelPosition, displayText);
        }

        void DrawSceneLabel(Vector3 worldPosition, string displayText)
        {
            Camera cam;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.camera != null)
                cam = sceneView.camera;
            else
            {
                cam = Camera.current;
                if (cam == null) return;
            }

            Vector3 sp = cam.WorldToScreenPoint(worldPosition);
            if (sp.z <= 0) return;

            Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPosition);
            Vector2 size = gizmoStyle.CalcSize(new GUIContent(displayText));
            var rect = new Rect(guiPos.x - size.x * 0.5f, guiPos.y - size.y * 0.5f, size.x, size.y);

            Handles.BeginGUI();

            if (useBackground)
            {
                var backgroundRect = new Rect(rect);
                backgroundRect.xMin -= 5;
                backgroundRect.xMax += 5;
                backgroundRect.yMin -= 2;
                backgroundRect.yMax += 2;

                Color oldColor = GUI.color;
                GUI.color = backgroundColor;
                GUI.Label(backgroundRect, GUIContent.none, backgroundGUIStyle);
                GUI.color = oldColor;
            }

            if (outlineMode == OutlineMode.Shadow)
            {
                int o = Mathf.Max(1, shadowOffsetPx);
                GUI.Label(new Rect(rect.x + o, rect.y + o, rect.width, rect.height),
                    displayText, gizmoOutlineStyle);
            }
            else if (outlineMode == OutlineMode.Outline)
            {
                int o = Mathf.Max(1, Mathf.CeilToInt(outlineWidth));
                GUI.Label(new Rect(rect.x - o, rect.y, rect.width, rect.height), displayText,
                    gizmoOutlineStyle);
                GUI.Label(new Rect(rect.x + o, rect.y, rect.width, rect.height), displayText,
                    gizmoOutlineStyle);
                GUI.Label(new Rect(rect.x, rect.y - o, rect.width, rect.height), displayText,
                    gizmoOutlineStyle);
                GUI.Label(new Rect(rect.x, rect.y + o, rect.width, rect.height), displayText,
                    gizmoOutlineStyle);
            }

            GUI.Label(rect, displayText, gizmoStyle);

            Handles.EndGUI();
            Gizmos.DrawIcon(transform.position, "sv_label_4", true);
        }

        // ========== 共用 Screen Space Overlay（所有 Label 共享一個 UIDocument） ==========

        static class DebugLabelOverlay
        {
            public static GameObject _overlayGo;
            static UIDocument _uiDocument;
            static PanelSettings _panelSettings;
            static VisualElement _root;

            public static VisualElement GetRoot() => _root;

            public static void EnsureReady()
            {
                // 靜態欄位都還活著 → 已初始化
                if (_overlayGo != null && _uiDocument != null && _root != null)
                    return;

                // Domain reload 後靜態歸零，但舊 GO (DontSave) 還在場景中
                // PanelSettings (runtime ScriptableObject) 已被銷毀 → Missing
                // 解法：直接砍掉重建，不嘗試復用
                var stale = GameObject.Find("[DebugLabelOverlay]");
                if (stale != null)
                    Object.DestroyImmediate(stale);

                // 建立 PanelSettings（也標記 DontSave 避免存進場景）
                _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                _panelSettings.name = "DebugLabelOverlay";
                _panelSettings.hideFlags = HideFlags.HideAndDontSave;
                _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                _panelSettings.clearColor = false;
                _panelSettings.sortingOrder = 10000;


                // 建立 GO + UIDocument
                _overlayGo = new GameObject("[DebugLabelOverlay]");
                _overlayGo.hideFlags = HideFlags.HideAndDontSave;

                _uiDocument = _overlayGo.AddComponent<UIDocument>();
                _uiDocument.panelSettings = _panelSettings;

                // rootVisualElement 可能要等下一幀，TryBuildLabel 會在 Update 中重試
                if (_uiDocument.rootVisualElement != null)
                    SetupRoot();
            }

            static void SetupRoot()
            {
                _root = _uiDocument.rootVisualElement;
                _root.pickingMode = PickingMode.Ignore;
                _root.style.position = Position.Absolute;
                _root.style.left = 0;
                _root.style.top = 0;
                _root.style.right = 0;
                _root.style.bottom = 0;
            }
        }
#endif
    }
}
