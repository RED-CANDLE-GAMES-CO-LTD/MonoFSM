using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Animation
{
    /// <summary>
    /// 角色招式位移：以 root motion delta 模式播放 clip 位移。
    /// 編輯期把 clip 的 root node position/rotation 曲線烘進序列化 AnimationCurve（prefab save 自動烘），
    /// Simulate（tick）期間逐 tick evaluate「本 tick 的位移量 delta」餵給 IRootMotionReceiver
    /// （RigidbodyMotionCustomReceiver / CharacterMotionReceiver），走完整碰撞管線。
    /// Target warp：window 內每 tick 持續趨近「authored 的相對 target 位置」
    /// （clip 裡 root 相對 warp node 的關係；沒設 node 視同 node 在 authored 落點），
    /// 誤差按剩餘 window 時間分攤——入窗平滑、window 結束時剛好收斂，之後純 root motion 不再追 target。
    /// 不需記 state enter 錨點（每 tick 是 networked 位置 + tick 的純函數，resim-safe），
    /// 被碰撞擋偏後也會自動重新修正。
    /// 視覺骨架動畫照舊由 AnimatorPlayAction 在 Render 播放。
    /// </summary>
    [Searchable]
    public class RootMotionSimulateClipMoveAction : AbstractDescriptionBehaviour, IUpdateSimulate,
        ISceneAwake, IClipPlayProgress
    {
        public override string Description =>
            $"RootMotion [{(_clip != null ? _clip.name : "?")}]"
            + (_targetTransform != null ? $" warp→ [{_targetTransform.name}]" : "");

        protected override string DescriptionTag => "RootMotionClip";


        [TitleGroup("Clip")] [Required] [ValueDropdown(nameof(GetAnimatorClips))] [SerializeField]
        private AnimationClip _clip;

        // ValueDropdown 引用此方法，attribute 在 build 也存在，因此方法不可包在 #if UNITY_EDITOR 內
        private IEnumerable<AnimationClip> GetAnimatorClips()
        {
            if (_animatorRoot == null) return null;
            var animator = _animatorRoot.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return null;
            return animator.runtimeAnimatorController.animationClips;
        }

        [TitleGroup("Warp")]
        [Tooltip("要趨近的目標點，可由程式在 runtime 設定（TargetTransform property）。不設定就是純 root motion")]
        [SerializeField]
        private Transform _targetTransform;

        [TitleGroup("Warp")]
        [Tooltip(
            "warp window 開始（clip normalized time）。在此之前純 root motion、不做誤差修正。可由 _warpWindowMarker 烘焙")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _warpWindowStart;

        [TitleGroup("Warp")]
        [Tooltip(
            "warp window 結束＝對齊時間點（clip normalized time）。播放到此 frame 時剛好貼齊目標。可由 _warpWindowMarker 烘焙")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _warpNormalizedTime = 1f;

        [TitleGroup("Warp")]
        [Tooltip("動畫 authoring 時假想 target 所在的參考節點（須為 _animatorRoot 的子物件、相對 rig 靜止）。"
                 + "warp window 內會持續維持「clip 裡 root 相對此節點」的相對位置（例如前搖遠離時保持 authored 距離）。"
                 + "不設定＝視同節點擺在 authored 落點，window 結束時 root 貼齊 target.position")]
        [SerializeField]
        private Transform _warpTargetNode;

        [TitleGroup("Warp")]
        [Tooltip("整個 warp window 的累計修正量上限（公尺）。target 超出範圍時盡力靠近但不保證貼齊。0＝不限制")]
        [SerializeField]
        private float _maxWarpDistance;

        [TitleGroup("Warp")]
        [Tooltip("是否連朝向也導引到 targetTransform.rotation（對齊 target 的朝向角度）")]
        [SerializeField]
        private bool _warpRotation;

        [TitleGroup("Warp")]
        [Tooltip("warp window 內持續讓角色看向 _targetTransform.position（適合衝刺追蹤）")]
        [SerializeField]
        private bool _faceTargetPosition;

        [TitleGroup("Warp")]
        [ShowIf(nameof(_faceTargetPosition))]
        [Tooltip("facing 只繞 Y 軸旋轉（忽略高低差，避免角色仰頭/低頭）")]
        [SerializeField]
        private bool _faceTargetFlattenY = true;

        [TitleGroup("Warp")]
        [ShowIf(nameof(_faceTargetPosition))]
        [Tooltip("facing 最大角速度（度/秒）。用 RotateTowards，不會過頭或振盪。0 = 瞬間對齊")]
        [SerializeField]
        private float _faceTargetMaxRotSpeed = 360f;

        [TitleGroup("Warp")] [Tooltip("忽略垂直方向的誤差修正（地面角色建議開啟，避免和重力/貼地打架）")] [SerializeField]
        private bool _flattenYError = true;

        [AutoParent] private MonoStateBehaviour _stateBehaviour;

        [TitleGroup("Clip")]
        [Required]
        [Tooltip("rig root：clip 曲線路徑的基準節點，IRootMotionReceiver 也掛在這上面")]
        [SerializeField]
        private Transform
            _animatorRoot; //FIXME: animator上面應該會掛 AnimatorControlModeHandle，還要要做成必要的dependency?

        // receiver cache（EnterSceneAwake 建立，lazy fallback 保險）
        [ShowInInspector] private IRootMotionReceiver _receiver;
        // private Transform _charTransform;

        private IRootMotionReceiver Receiver
        {
            get
            {
                if (_receiver == null)
                    CacheReceiver();
                return _receiver;
            }
        }

        private void CacheReceiver()
        {
            if (_animatorRoot == null)
                return;
            _receiver = _animatorRoot.GetComponent<IRootMotionReceiver>();
            if (_receiver == null)
                Debug.LogError(
                    $"[RootMotionClipMoveAction] _animatorRoot [{_animatorRoot.name}] 上找不到 IRootMotionReceiver",
                    this);
        }

        public void EnterSceneAwake()
        {
            CacheReceiver();
        }

        // ===== 烘焙的 root node 曲線（rig local space）=====
        [TitleGroup("Baked Curves")]
        [Tooltip("由 _clip 烘焙（prefab save 自動執行），runtime 只讀這些曲線")]
        [SerializeField]
        private AnimationCurve _posX, _posY, _posZ;

        [SerializeField] private AnimationCurve _rotX, _rotY, _rotZ, _rotW;

        // ===== 烘焙的 GameObject active 開關（例如攻擊 collider 開關）=====
        [System.Serializable]
        private class BakedActiveTrack
        {
            [Tooltip("要被開關的 GameObject（由 clip 曲線路徑在烘焙時解析）")]
            public GameObject _target;

            [Tooltip("clip 的 m_IsActive 曲線（discrete，>0.5 = active）")]
            public AnimationCurve _curve;
        }

        [TitleGroup("Baked Curves")]
        [Tooltip("由 clip 裡的 GameObject m_IsActive 曲線烘焙（prefab save 自動執行）。"
                 + "Simulate 每 tick 以 StateTime evaluate 後 SetActive——是 networked StateTime 的純函數，resim-safe")]
        [SerializeField]
        private BakedActiveTrack[] _activeTracks;

        private void ApplyActiveTracks(float t)
        {
            if (_activeTracks == null)
                return;
            foreach (var track in _activeTracks)
            {
                if (track?._target == null || track._curve == null || track._curve.length == 0)
                    continue;
                var active = track._curve.Evaluate(t) > 0.5f;
                if (track._target.activeSelf != active)
                    track._target.SetActive(active);
            }
        }

        public Transform TargetTransform
        {
            get => _targetTransform;
            set => _targetTransform = value;
        }

        public bool IsActiveState =>
            _stateBehaviour != null
            && _stateBehaviour.Owner != null
            && _stateBehaviour.Owner.IsCurrentState(_stateBehaviour);

        /// <summary>給 done transition 用</summary>
        public bool IsDone => _clip != null && _stateBehaviour.StateTime >= _clip.length;

        public bool IsProgressPassedRatio(float ratio) =>
            _clip != null && _stateBehaviour.StateTime >= _clip.length * ratio;

        // Debug 觀察用
        [PreviewInDebugMode] private Vector3 _debugAnimDelta;
        [PreviewInDebugMode] private Vector3 _debugWarpCorrection;
        [PreviewInDebugMode] private float _debugStateTime;

        private Vector3 EvalLocalPos(float t)
        {
            return new Vector3(
                _posX != null && _posX.length > 0 ? _posX.Evaluate(t) : 0f,
                _posY != null && _posY.length > 0 ? _posY.Evaluate(t) : 0f,
                _posZ != null && _posZ.length > 0 ? _posZ.Evaluate(t) : 0f);
        }

        private Quaternion EvalLocalRot(float t)
        {
            if (_rotW == null || _rotW.length == 0)
                return Quaternion.identity;
            var q = new Quaternion(
                _rotX.Evaluate(t),
                _rotY.Evaluate(t),
                _rotZ.Evaluate(t),
                _rotW.Evaluate(t));
            return Quaternion.Normalize(q);
        }

        public void Simulate(float deltaTime)
        {
            if (!isActiveAndEnabled || !IsActiveState || _clip == null || Receiver == null)
            {
                // Debug.Log(
                //     $"[RootMotionClipMoveAction] Simulate skipped: active={isActiveAndEnabled} IsActiveState={IsActiveState} clip={(_clip != null ? _clip.name : "null")} receiver={(Receiver != null ? Receiver.ToString() : "null")}",
                //     this);
                return;
            }


            var clipLength = _clip.length;
            var t1 = Mathf.Min(_stateBehaviour.StateTime, clipLength);
            var t0 = Mathf.Clamp(_stateBehaviour.StateTime - deltaTime, 0f, clipLength);
            _debugStateTime = t1;

            // GameObject 開關（collider 等）：純 StateTime 函數 + idempotent，resim 重播會重新套用
            ApplyActiveTracks(t1);

            if (t1 <= t0)
            {
                // Debug.Log(
                //     $"[RootMotionClipMoveAction] Simulate skipped: no time advance (t0={t0:F2} t1={t1:F2} deltaTime={deltaTime:F2})",
                //     this);
                return;
            }


            var charRot = _animatorRoot.rotation;

            // 動畫本身的 delta（rig local → world，rig 前方 = 角色 +Z）
            var animDeltaLocal = EvalLocalPos(t1) - EvalLocalPos(t0);
            var worldDelta = charRot * animDeltaLocal;
            var animDeltaRotLocal = Quaternion.Inverse(EvalLocalRot(t0)) * EvalLocalRot(t1);
            var deltaRotLocal = animDeltaRotLocal;
            _debugAnimDelta = worldDelta;
            _debugWarpCorrection = Vector3.zero;

            // Target warp：window 內每 tick 持續趨近「當下時間點 authored 的相對位置」，
            // 入窗誤差按剩餘 window 時間分攤平滑收斂，window 結束後不再追 target
            var warpStartTime = _warpWindowStart * clipLength;
            var warpTime = _warpNormalizedTime * clipLength;
            if (_targetTransform != null && t1 >= warpStartTime && t1 < warpTime)
            {
                // fraction：本 tick 該吃掉的誤差比例（最後一個 tick ≈ 1 → 完全收斂）
                var fraction =
                    Mathf.Clamp01(deltaTime / Mathf.Max(warpTime - t1 + deltaTime, deltaTime));

                // 假想 target 的 rig space 位置：
                // node 是 rig 的靜止子物件，InverseTransformPoint 結果為常數（resim-safe）；
                // 沒設定＝視同擺在 authored 落點 → window 結束時 root 貼齊 target.position
                var authoredTargetLocal = _warpTargetNode != null
                    ? _animatorRoot.InverseTransformPoint(_warpTargetNode.position)
                    : EvalLocalPos(warpTime);

                // 當下時間點 authored 的「root 相對假想 target」位置，對應實際 target
                var desired = _targetTransform.position
                              + charRot * (EvalLocalPos(t1) - authoredTargetLocal);
                var error = desired - (_animatorRoot.position + worldDelta);
                if (_flattenYError)
                    error.y = 0f;
                var correction = error * fraction;
                if (_maxWarpDistance > 0f)
                {
                    // 每 tick 修正預算 = 上限 × (dt / window 長度)。
                    // 閉迴路天生把總修正量均勻分攤到每 tick，所以累計修正 ≤ 上限
                    var windowDuration = Mathf.Max(warpTime - warpStartTime, deltaTime);
                    correction = Vector3.ClampMagnitude(
                        correction, _maxWarpDistance * deltaTime / windowDuration);
                }

                _debugWarpCorrection = correction;
                worldDelta += _debugWarpCorrection;

                if (_warpRotation)
                {
                    // 預測 warp frame 朝向 = 當前朝向 * 剩餘動畫旋轉
                    var remainingRot =
                        Quaternion.Inverse(EvalLocalRot(t1)) * EvalLocalRot(warpTime);
                    var predictedRot = charRot * animDeltaRotLocal * remainingRot;
                    var errorRot = _targetTransform.rotation * Quaternion.Inverse(predictedRot);
                    var extraWorld = Quaternion.Slerp(Quaternion.identity, errorRot, fraction);
                    // receiver 是 post-multiply（local delta），把 world 修正轉回 local 前置
                    var extraLocal = Quaternion.Inverse(charRot) * extraWorld * charRot;
                    deltaRotLocal = extraLocal * animDeltaRotLocal;
                }

                if (_faceTargetPosition)
                {
                    // 持續看向 target 位置（衝刺追蹤用）：RotateTowards 固定角速度，不會過頭/振盪
                    var toTarget = _targetTransform.position - _animatorRoot.position;
                    if (_faceTargetFlattenY) toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.001f)
                    {
                        var desiredRot = Quaternion.LookRotation(toTarget.normalized);
                        var currentRot = charRot * deltaRotLocal;
                        var maxDeg = _faceTargetMaxRotSpeed > 0f
                            ? _faceTargetMaxRotSpeed * deltaTime
                            : float.MaxValue;
                        var steered = Quaternion.RotateTowards(currentRot, desiredRot, maxDeg);
                        deltaRotLocal = Quaternion.Inverse(charRot) * steered;
                    }
                }
            }

            // Debug.Log(
            //     $"[RootMotionClipMoveAction] Simulate: time={_debugStateTime:F2} animDelta={_debugAnimDelta} warpCorrection={_debugWarpCorrection}",
            //     this);
            _receiver.OnProcessRootMotion(worldDelta, deltaRotLocal);
        }

        public void AfterUpdate()
        {
        }

#if UNITY_EDITOR
        // ===== Edit Mode Preview（AnimationMode 取樣，停止後自動還原 pose）=====

        private static bool _isPreviewOwnedByRootMotionAction;

        private static bool IsPreviewing =>
            _isPreviewOwnedByRootMotionAction && AnimationMode.InAnimationMode();

        [TitleGroup("Preview")]
        [ShowInInspector]
        [HideInPlayMode]
        [PropertyRange(0f, "@_clip != null ? _clip.length : 1f")]
        [OnValueChanged(nameof(PreviewSample))]
        [System.NonSerialized]
        private float _previewTime;

        private void PreviewSample()
        {
            if (_clip == null || _animatorRoot == null)
                return;
            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
                _isPreviewOwnedByRootMotionAction = true;
                Selection.selectionChanged -= StopPreview;
                Selection.selectionChanged += StopPreview;
            }

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(_animatorRoot.gameObject, _clip, _previewTime);
            AnimationMode.EndSampling();
            SceneView.RepaintAll();
        }

        [InfoBox(
            "⏺ AnimationMode Preview 進行中！Animator / Animation window 此時無法編輯。\n結束請按下方紅色按鈕（或切換選取物件會自動結束）",
            InfoMessageType.Error,
            nameof(IsPreviewing)
        )]
        [TitleGroup("Preview")]
        [Button("⏹ 結束 Preview（還原 pose）", ButtonSizes.Large)]
        [GUIColor(1f, 0.3f, 0.3f)]
        [ShowIf(nameof(IsPreviewing))]
        [HideInPlayMode]
        private void StopPreview()
        {
            Selection.selectionChanged -= StopPreview;
            _isPreviewOwnedByRootMotionAction = false;
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
            SceneView.RepaintAll();
        }

        [TitleGroup("Preview")]
        [Button("編輯動畫")]
        [HideInPlayMode]
        private void EditClip()
        {
            if (_animatorRoot == null)
            {
                Debug.LogError("_animatorRoot 未設定，無法編輯動畫", this);
                return;
            }

            if (_clip == null)
            {
                Debug.LogError("_clip 未指定，無法編輯動畫", this);
                return;
            }

            StopPreview();

            Selection.activeObject = _animatorRoot.gameObject;
            var clip = _clip;
            EditorApplication.delayCall += () =>
            {
                var animationWindow = EditorWindow.GetWindow<AnimationWindow>(false);
                animationWindow.animationClip = clip;
                if (animationWindow.animationClip == clip)
                    animationWindow.previewing = true;
                animationWindow.Repaint();
            };
        }

        // ===== 編輯期烘焙 =====

        [TitleGroup("Baked Curves")] [Tooltip("root motion 位移 key 所在的節點")] [SerializeField]
        private Transform _rootMotionNode;

        [TitleGroup("Warp")]
        [Tooltip("warp window 標記物件：在 clip 裡動畫此物件的 active 狀態，烘焙時取第一段 active 區間寫入 "
                 + "_warpWindowStart / _warpNormalizedTime。不設定則手動填上面兩個欄位")]
        [SerializeField]
        private GameObject _warpWindowMarker;

        private AnimationCurve GetCurve(string path, string property)
        {
            return AnimationUtility.GetEditorCurve(
                _clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property));
        }

        [TitleGroup("Baked Curves")]
        [Button("烘焙 Root Motion 曲線")]
        private void BakeCurves()
        {
            if (_clip == null || _animatorRoot == null || _rootMotionNode == null)
            {
                Debug.LogError("BakeCurves: _clip / _animatorRoot / _rootMotionNode 未設定", this);
                return;
            }

            var path = AnimationUtility.CalculateTransformPath(_rootMotionNode, _animatorRoot);
            _posX = GetCurve(path, "m_LocalPosition.x");
            _posY = GetCurve(path, "m_LocalPosition.y");
            _posZ = GetCurve(path, "m_LocalPosition.z");
            _rotX = GetCurve(path, "m_LocalRotation.x");
            _rotY = GetCurve(path, "m_LocalRotation.y");
            _rotZ = GetCurve(path, "m_LocalRotation.z");
            _rotW = GetCurve(path, "m_LocalRotation.w");

            // euler key 的 clip 沒有 quaternion 曲線，烘成 quaternion 四條
            if (_rotW == null)
            {
                var ex = GetCurve(path, "localEulerAnglesRaw.x");
                var ey = GetCurve(path, "localEulerAnglesRaw.y");
                var ez = GetCurve(path, "localEulerAnglesRaw.z");
                if (ex != null || ey != null || ez != null)
                    BakeEulerToQuaternion(ex, ey, ez);
            }

            if (_warpWindowMarker != null)
                BakeWarpWindow();

            BakeActiveTracks();

            EditorUtility.SetDirty(this);
            Debug.Log(
                $"[RootMotionClipMoveAction] 烘焙完成 clip={_clip.name} path=\"{path}\" "
                + $"pos curves={(_posZ != null ? "ok" : "none")} rot curves={(_rotW != null ? "ok" : "none")}",
                this);
        }

        private void BakeEulerToQuaternion(AnimationCurve ex, AnimationCurve ey, AnimationCurve ez)
        {
            const int samplesPerSecond = 30;
            var count = Mathf.Max(2, Mathf.CeilToInt(_clip.length * samplesPerSecond) + 1);
            _rotX = new AnimationCurve();
            _rotY = new AnimationCurve();
            _rotZ = new AnimationCurve();
            _rotW = new AnimationCurve();
            for (var i = 0; i < count; i++)
            {
                var t = _clip.length * i / (count - 1);
                var euler = new Vector3(
                    ex?.Evaluate(t) ?? 0f, ey?.Evaluate(t) ?? 0f, ez?.Evaluate(t) ?? 0f);
                var q = Quaternion.Euler(euler);
                _rotX.AddKey(t, q.x);
                _rotY.AddKey(t, q.y);
                _rotZ.AddKey(t, q.z);
                _rotW.AddKey(t, q.w);
            }
        }

        private void BakeActiveTracks()
        {
            var tracks = new List<BakedActiveTrack>();
            foreach (var binding in AnimationUtility.GetCurveBindings(_clip))
            {
                if (binding.type != typeof(GameObject) || binding.propertyName != "m_IsActive")
                    continue;

                var node = string.IsNullOrEmpty(binding.path)
                    ? _animatorRoot
                    : _animatorRoot.Find(binding.path);
                if (node == null)
                {
                    Debug.LogWarning(
                        $"[RootMotionClipMoveAction] clip [{_clip.name}] 的 m_IsActive 曲線路徑 [{binding.path}] 在 _animatorRoot 底下找不到節點，略過",
                        this);
                    continue;
                }

                // warp window 標記是給 BakeWarpWindow 用的，不當成 runtime 開關
                if (_warpWindowMarker != null && node.gameObject == _warpWindowMarker)
                    continue;

                var curve = AnimationUtility.GetEditorCurve(_clip, binding);
                // GameObject active 是 discrete float 曲線，部分版本要用 DiscreteCurve binding 才撈得到
                if (curve == null)
                    curve = AnimationUtility.GetEditorCurve(
                        _clip,
                        EditorCurveBinding.DiscreteCurve(
                            binding.path, typeof(GameObject), "m_IsActive"));
                if (curve == null || curve.length == 0)
                    continue;

                tracks.Add(new BakedActiveTrack { _target = node.gameObject, _curve = curve });
            }

            _activeTracks = tracks.ToArray();
            if (_activeTracks.Length > 0)
                Debug.Log(
                    $"[RootMotionClipMoveAction] active track 烘焙完成，共 {_activeTracks.Length} 個節點",
                    this);
        }

        private void BakeWarpWindow()
        {
            var path = AnimationUtility.CalculateTransformPath(
                _warpWindowMarker.transform, _animatorRoot);
            // GameObject active 是 discrete float 曲線，部分版本要用 DiscreteCurve binding 才撈得到
            var curve = AnimationUtility.GetEditorCurve(
                _clip, EditorCurveBinding.DiscreteCurve(path, typeof(GameObject), "m_IsActive"));
            if (curve == null)
                curve = AnimationUtility.GetEditorCurve(
                    _clip, EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive"));
            if (curve == null || curve.length == 0)
            {
                Debug.LogWarning(
                    $"[RootMotionClipMoveAction] clip [{_clip.name}] 裡找不到 [{path}] 的 m_IsActive 曲線，warp window 維持手動值",
                    this);
                return;
            }

            // 取第一段 active 區間：第一個 value>0.5 的 key 到下一個 value<=0.5 的 key（沒有則到 clip 結尾）
            float? start = null;
            var end = _clip.length;
            foreach (var key in curve.keys)
            {
                if (start == null && key.value > 0.5f)
                {
                    start = key.time;
                }
                else if (start != null && key.value <= 0.5f)
                {
                    end = key.time;
                    break;
                }
            }

            if (start == null)
            {
                Debug.LogWarning(
                    $"[RootMotionClipMoveAction] [{path}] 的 m_IsActive 曲線沒有 active 區間，warp window 維持手動值",
                    this);
                return;
            }

            _warpWindowStart = Mathf.Clamp01(start.Value / _clip.length);
            _warpNormalizedTime = Mathf.Clamp01(end / _clip.length);
            Debug.Log(
                $"[RootMotionClipMoveAction] warp window 烘焙完成 [{_warpWindowStart:F2} ~ {_warpNormalizedTime:F2}] (normalized)",
                this);
        }

        public override void OnBeforePrefabSave()
        {
            if (isActiveAndEnabled && _clip != null && _animatorRoot != null &&
                _rootMotionNode != null)
                BakeCurves();
            base.OnBeforePrefabSave();
        }

        // ===== Gizmo：以角色當前 pose 畫出 clip 原樣的落點與 target =====
        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying || _clip == null || _posZ == null)
                return;
            var charTransform = _animatorRoot;
            if (charTransform == null)
                return;

            var warpTime = _warpNormalizedTime * _clip.length;
            var landingLocal = EvalLocalPos(warpTime) - EvalLocalPos(0f);
            var landingPos = charTransform.position + charTransform.rotation * landingLocal;
            var landingRot = charTransform.rotation * Quaternion.Inverse(EvalLocalRot(0f)) *
                             EvalLocalRot(warpTime);

            // 黃色：clip 原樣（無 warp）的落點
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(landingPos, 0.2f);
            Gizmos.DrawLine(landingPos, landingPos + landingRot * Vector3.forward * 0.5f);
            Gizmos.DrawLine(charTransform.position, landingPos);

            // 黃色小球：warp window 開始位置（authored 軌跡上）
            if (_warpWindowStart > 0f)
            {
                var windowStartLocal =
                    EvalLocalPos(_warpWindowStart * _clip.length) - EvalLocalPos(0f);
                Gizmos.DrawWireSphere(
                    charTransform.position + charTransform.rotation * windowStartLocal, 0.1f);
            }

            // 青色：假想 target 節點
            if (_warpTargetNode != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_warpTargetNode.position, 0.15f);
                Gizmos.DrawLine(landingPos, _warpTargetNode.position);
            }

            // 綠色：target 與 desired 落點連線 = warp 量
            if (_targetTransform != null)
            {
                var desired = _targetTransform.position;
                if (_warpTargetNode != null)
                {
                    var authoredTargetLocal =
                        charTransform.InverseTransformPoint(_warpTargetNode.position);
                    desired += charTransform.rotation *
                               (EvalLocalPos(warpTime) - authoredTargetLocal);
                }

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_targetTransform.position, 0.2f);
                Gizmos.DrawWireSphere(desired, 0.15f);
                Gizmos.DrawLine(landingPos, desired);
            }
        }
#endif
    }
}
