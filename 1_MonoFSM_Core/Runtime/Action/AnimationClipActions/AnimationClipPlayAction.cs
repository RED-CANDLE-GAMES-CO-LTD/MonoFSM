using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Animation
{
    /// <summary>
    /// 在 FSM State 期間以 StateTime 手動取樣 AnimationClip（不經過 Animator runtime）。
    /// StateTime 是 tick-based（StateChangeTick 起算），FSM 狀態有網路同步時，動畫在各端 deterministic、自動對齊。
    /// 編輯期照常用 Animator + Animation window 編輯 clip，runtime 由此 Action 取樣播放。
    /// 需要 kinematic Rigidbody 物理推擠時，搭配 AnimationClipPhysicsSampler（Simulate 階段取樣）。
    /// </summary>
    [Searchable]
    public class AnimationClipPlayAction : AbstractDescriptionBehaviour, IRenderBehaiour, ISceneAwake
    {
        public override string Description =>
            $"Clip [{(_clip != null ? _clip.name : "?")}] on [{(SampleRoot != null ? SampleRoot.name : "?")}]";

        protected override string DescriptionTag => "PlayClip";

        [TitleGroup("Clip")]
        [Required]
        [SerializeField]
        private AnimationClip _clip;

        [TitleGroup("Clip")]
        [Tooltip("取樣根節點（clip 內曲線相對路徑的基準）。有設 PhysicsSampler 時可留空，直接用 sampler 的 transform")]
        [SerializeField]
        private Transform _sampleRoot;

        [TitleGroup("Clip")]
        [Tooltip("需要 kinematic Rigidbody 物理推擠時指定，pose 會在 Simulate（tick）階段取樣")]
        [DropDownRef]
        [SerializeField]
        private AnimationClipPhysicsSampler _physicsSampler;

        [TitleGroup("Clip")]
        [SerializeField]
        private float _speed = 1f;

        [TitleGroup("Clip")]
        [SerializeField]
        private bool _loop;

        [TitleGroup("Clip")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _startNormalizedTimeOffset;

        [AutoParent]
        private MonoStateBehaviour _stateBehaviour;

        public AnimationClip Clip => _clip;

        private Transform SampleRoot =>
            _sampleRoot != null ? _sampleRoot
            : _physicsSampler != null ? _physicsSampler.transform
            : null;

        public bool IsActiveState =>
            _stateBehaviour != null
            && _stateBehaviour.Owner != null
            && _stateBehaviour.Owner.IsCurrentState(_stateBehaviour);

        /// <summary>tick-based 取樣時間（Simulate 階段用，resim-safe）</summary>
        public float LogicSampleTime => WrapTime(_stateBehaviour.StateTime);

        /// <summary>render 插值取樣時間（StateMachine.StateTime 內含 interpolation tick 處理）</summary>
        private float RenderSampleTime =>
            WrapTime(_stateBehaviour.Machine != null ? _stateBehaviour.Machine.StateTime : _stateBehaviour.StateTime);

        private float WrapTime(float stateTime)
        {
            if (_clip == null)
                return 0f;
            var time = stateTime * _speed + _startNormalizedTimeOffset * _clip.length;
            return _loop ? Mathf.Repeat(time, _clip.length) : Mathf.Min(time, _clip.length);
        }

        /// <summary>給 done transition 用（同 AnimatorPlayAction.IsDone）</summary>
        public bool IsDone => _clip != null && _stateBehaviour.StateTime * _speed >= _clip.length;

        public bool IsProgressPassedRatio(float ratio) =>
            _clip != null && _stateBehaviour.StateTime * _speed >= _clip.length * ratio;

        public void EnterSceneAwake()
        {
            if (_physicsSampler != null)
                _physicsSampler.Register(this);
        }

        // IRenderBehaiour：視覺取樣（proxy 端也會跑，吃插值後的 StateTime）
        public void OnEnterRender()
        {
            Debug.Log("[AnimationClipPlayAction] OnEnterRender: " + Description, this);
            SampleVisual();
        }

        public void OnRender() => SampleVisual();

        private void SampleVisual()
        {
            var root = SampleRoot;
            if (_clip == null || root == null)
                return;
            _clip.SampleAnimation(root.gameObject, RenderSampleTime);
        }

#if UNITY_EDITOR
        // ===== Edit Mode Preview（AnimationMode 取樣，停止後自動還原 pose，不會弄髒 scene）=====

        // 是否由本類別啟動的 preview（避免和 Animation window 自己的 preview 混淆）
        private static bool _isPreviewOwnedByClipAction;

        private static bool IsPreviewing =>
            _isPreviewOwnedByClipAction && AnimationMode.InAnimationMode();

        [TitleGroup("Preview")]
        [InfoBox(
            "⏺ AnimationMode Preview 進行中！Animator / Animation window 此時無法編輯。\n結束請按下方紅色按鈕（或切換選取物件會自動結束）",
            InfoMessageType.Error,
            nameof(IsPreviewing)
        )]
        [ShowInInspector]
        [HideInPlayMode]
        [PropertyRange(0f, "@_clip != null ? _clip.length : 1f")]
        [OnValueChanged(nameof(PreviewSample))]
        [System.NonSerialized]
        private float _previewTime;

        private void PreviewSample()
        {
            var root = SampleRoot;
            if (_clip == null || root == null)
                return;
            if (!AnimationMode.InAnimationMode())
            {
                AnimationMode.StartAnimationMode();
                _isPreviewOwnedByClipAction = true;
                // 切換選取物件就自動結束 preview，避免忘記關導致 Animator 卡住不能編輯
                Selection.selectionChanged -= StopPreview;
                Selection.selectionChanged += StopPreview;
            }
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(root.gameObject, _clip, _previewTime);
            AnimationMode.EndSampling();
            SceneView.RepaintAll();
        }

        [TitleGroup("Preview")]
        [Button("⏹ 結束 Preview（還原 pose）", ButtonSizes.Large)]
        [GUIColor(1f, 0.3f, 0.3f)]
        [ShowIf(nameof(IsPreviewing))]
        [HideInPlayMode]
        private void StopPreview()
        {
            Selection.selectionChanged -= StopPreview;
            _isPreviewOwnedByClipAction = false;
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
            SceneView.RepaintAll();
        }

        [TitleGroup("Preview")]
        [Button("編輯動畫")]
        private void EditClip()
        {
            var root = SampleRoot;
            if (root == null)
            {
                Debug.LogError("SampleRoot 未設定（_sampleRoot 或 _physicsSampler 擇一），無法編輯動畫", this);
                return;
            }
            if (_clip == null)
            {
                Debug.LogError("_clip 未指定，無法編輯動畫", this);
                return;
            }
            if (root.GetComponent<Animator>() == null)
                Debug.LogWarning("SampleRoot 上沒有 Animator，Animation window 無法錄製編輯（請在 root 上加 Animator，僅供編輯期使用）", root);

            // Animation window 的 preview 和 AnimationMode preview 會互卡，先結束自己的
            StopPreview();

            Selection.activeObject = root.gameObject;
            var clip = _clip;

            // 延遲到下一個 Editor update，等 AnimationWindow 處理完 selection change 後再設定 clip
            EditorApplication.delayCall += () =>
            {
                var animationWindow = EditorWindow.GetWindow<AnimationWindow>(false);
                animationWindow.animationClip = clip;
                if (animationWindow.animationClip == clip) // clip 沒被視窗接受時不要強開 preview（會 NRE）
                    animationWindow.previewing = true;
                animationWindow.Repaint();
            };
        }
#endif
    }
}
