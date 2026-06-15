using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

namespace MonoFSM.Animation
{
    /// <summary>
    /// Clip 取樣完成後的 pose 後處理介面。
    /// AnimationClipPhysicsSampler（Simulate）與 AnimationClipPlayAction（Render）取樣後會依序呼叫。
    /// </summary>
    public interface IAnimationSampleModifier
    {
        void OnPostSample(GameObject sampleRoot, AnimationClipPlayAction action, float sampleTime);
    }

    /// <summary>
    /// Motion Warping：clip 以標準 root motion 授權——位移 key 在 root node（_warpedTransform）上，
    /// 從 local (0,0,0) 出發（pivot = 角色當前站位）。
    /// clip 空間的落點 = root node 在對齊時間點的 local pose（取樣 clip 自動推得並快取，不需 dummy marker）。
    /// 取樣後計算一個錨點座標系，使落點 pose 經映射後剛好貼齊 targetTransform，
    /// 再把 root node 在「自身 pivot 錨」與「目標錨」之間按播放進度混合——
    /// t=0 動畫原樣播放，到對齊時間點剛好貼齊目標，之後維持目標錨讓收招流暢走完。
    /// 快取內容是 clip 的純函數，無累積狀態，resim-safe。
    /// 掛在 sampleRoot 子層即可被 AnimationClipPhysicsSampler 自動收集。
    /// </summary>
    public class AnimationClipTargetWarper : MonoBehaviour, IAnimationSampleModifier
    {
        [Required]
        [Tooltip("root motion 位移 key 所在的節點（Animator root node），warp 會重新錨定這個節點")]
        [SerializeField]
        private Transform _warpedTransform;

        [Tooltip("要趨近的目標點（warp frame 的落點最終貼齊處），可由程式在 runtime 設定（TargetTransform property）")]
        [SerializeField]
        private Transform _targetTransform;

        [Tooltip("對齊時間點（clip normalized time）。播放到此 frame 時落點完全貼齊目標")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _warpNormalizedTime = 1f;

        [Tooltip("是否連朝向也對齊（落點朝向映射到 targetTransform.rotation）。關閉時只對齊位置，朝向維持自身 pivot")]
        [SerializeField]
        private bool _warpRotation;

        [Tooltip("錨點切換權重曲線（x: 播放進度 0~對齊時間點, y: 目標錨定比例 0~1）")]
        [SerializeField]
        private AnimationCurve _easeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // Debug 觀察用
        [ShowInInspector] private float _debugWeight;
        [ShowInInspector] private Vector3 _debugLocalPos;

        // warp frame 落點快取（clip 授權空間 local pose，只跟 clip + warpTime 有關）
        private AnimationClip _cachedClip;
        private float _cachedWarpTime = -1f;
        private Vector3 _cachedWarpLocalPos;
        private Quaternion _cachedWarpLocalRot;

        public Transform TargetTransform
        {
            get => _targetTransform;
            set => _targetTransform = value;
        }

        public void OnPostSample(GameObject sampleRoot, AnimationClipPlayAction action, float sampleTime)
        {
            if (_targetTransform == null || _warpedTransform == null)
                return;
            var clip = action.Clip;
            if (clip == null)
                return;

            var warpTime = _warpNormalizedTime * clip.length;
            var weight = warpTime <= 0f
                ? 1f
                : _easeCurve.Evaluate(Mathf.Clamp01(sampleTime / warpTime));
            _debugWeight = weight;
            if (weight <= 0f)
                return;

            EnsureWarpFramePose(sampleRoot, clip, warpTime, sampleTime);

            var root = sampleRoot.transform;

            // 當前取樣結果在 clip 授權空間的 local pose（相對自身 pivot）
            var localPos = root.InverseTransformPoint(_warpedTransform.position);
            var localRot = Quaternion.Inverse(root.rotation) * _warpedTransform.rotation;
            _debugLocalPos = localPos;

            if (_warpRotation)
            {
                // 錨點 A：使 warp frame 的落點 pose 經 A 映射後剛好等於 target pose
                var anchorRot = _targetTransform.rotation * Quaternion.Inverse(_cachedWarpLocalRot);
                var anchorPos = _targetTransform.position - anchorRot * _cachedWarpLocalPos;
                var desiredPos = anchorPos + anchorRot * localPos;
                var desiredRot = anchorRot * localRot;
                _warpedTransform.position =
                    Vector3.Lerp(_warpedTransform.position, desiredPos, weight);
                _warpedTransform.rotation =
                    Quaternion.Slerp(_warpedTransform.rotation, desiredRot, weight);
            }
            else
            {
                // 只對齊位置：錨點朝向維持自身 pivot
                var anchorPos = _targetTransform.position - root.rotation * _cachedWarpLocalPos;
                var desiredPos = anchorPos + root.rotation * localPos;
                _warpedTransform.position =
                    Vector3.Lerp(_warpedTransform.position, desiredPos, weight);
            }
        }

        /// <summary>
        /// 取樣 clip 在 warpTime 的 pose，記下 _warpedTransform 相對 sampleRoot 的落點 local pose，
        /// 再取樣回 sampleTime 還原當前 pose。結果只跟 clip + warpTime 有關，可快取（resim-safe）。
        /// </summary>
        private void EnsureWarpFramePose(GameObject sampleRoot, AnimationClip clip, float warpTime, float sampleTime)
        {
            if (_cachedClip == clip && Mathf.Approximately(_cachedWarpTime, warpTime))
                return;

            clip.SampleAnimation(sampleRoot, warpTime);
            var root = sampleRoot.transform;
            _cachedWarpLocalPos = root.InverseTransformPoint(_warpedTransform.position);
            _cachedWarpLocalRot = Quaternion.Inverse(root.rotation) * _warpedTransform.rotation;
            _cachedClip = clip;
            _cachedWarpTime = warpTime;
            clip.SampleAnimation(sampleRoot, sampleTime); // 還原回當前時間的 pose
            Debug.Log(
                $"[AnimationClipTargetWarper] 快取 warp frame 落點 clip={clip.name} warpTime={warpTime:F3} localPos={_cachedWarpLocalPos}",
                this);
        }

#if UNITY_EDITOR
        // ===== Editor Preview：用 AnimationUtility 讀曲線（不動場景 pose）畫出 warp frame 落點 gizmo =====

        [TitleGroup("Editor Preview")]
        [Tooltip("編輯期 gizmo 預覽用的 clip（runtime 不使用，落點以實際播放的 action clip 為準）")]
        [ValueDropdown(nameof(GetCandidateClips))]
        [SerializeField]
        private AnimationClip _previewClip;

        private IEnumerable<AnimationClip> GetCandidateClips()
        {
            var result = new List<AnimationClip>();
            foreach (var action in transform.root.GetComponentsInChildren<AnimationClipPlayAction>(true))
                if (action.Clip != null && !result.Contains(action.Clip))
                    result.Add(action.Clip);
            return result;
        }

        private Transform EditorSampleRoot
        {
            get
            {
                var sampler = GetComponentInParent<AnimationClipPhysicsSampler>();
                return sampler != null ? sampler.transform : transform.parent;
            }
        }

        private float EvalCurve(string path, string property, float time, float fallback)
        {
            var curve = AnimationUtility.GetEditorCurve(
                _previewClip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property));
            return curve != null ? curve.Evaluate(time) : fallback;
        }

        /// <summary>讀 _previewClip 曲線推出 warp frame 時 _warpedTransform 的 local pose（不取樣、不動場景）</summary>
        private bool TryEvaluateWarpFrameLocalPose(out Vector3 localPos, out Quaternion localRot)
        {
            localPos = Vector3.zero;
            localRot = Quaternion.identity;
            var root = EditorSampleRoot;
            if (root == null || _warpedTransform == null || _previewClip == null)
                return false;

            var warpTime = _warpNormalizedTime * _previewClip.length;
            var path = AnimationUtility.CalculateTransformPath(_warpedTransform, root);

            localPos = _warpedTransform.localPosition;
            localPos.x = EvalCurve(path, "m_LocalPosition.x", warpTime, localPos.x);
            localPos.y = EvalCurve(path, "m_LocalPosition.y", warpTime, localPos.y);
            localPos.z = EvalCurve(path, "m_LocalPosition.z", warpTime, localPos.z);

            var q = _warpedTransform.localRotation;
            var quatCurveX = AnimationUtility.GetEditorCurve(
                _previewClip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"));
            if (quatCurveX != null)
            {
                q.x = EvalCurve(path, "m_LocalRotation.x", warpTime, q.x);
                q.y = EvalCurve(path, "m_LocalRotation.y", warpTime, q.y);
                q.z = EvalCurve(path, "m_LocalRotation.z", warpTime, q.z);
                q.w = EvalCurve(path, "m_LocalRotation.w", warpTime, q.w);
                localRot = Quaternion.Normalize(q);
            }
            else
            {
                var euler = _warpedTransform.localEulerAngles;
                euler.x = EvalCurve(path, "localEulerAnglesRaw.x", warpTime, euler.x);
                euler.y = EvalCurve(path, "localEulerAnglesRaw.y", warpTime, euler.y);
                euler.z = EvalCurve(path, "localEulerAnglesRaw.z", warpTime, euler.z);
                localRot = Quaternion.Euler(euler);
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
                return;
            if (!TryEvaluateWarpFrameLocalPose(out var localPos, out var localRot))
                return;

            // 注意：若 _warpedTransform 與 root 之間還有被動畫的中間節點，這裡用的是中間節點當前的場景 pose
            var parent = _warpedTransform.parent;
            var landingPos = parent != null ? parent.TransformPoint(localPos) : localPos;
            var landingRot = (parent != null ? parent.rotation : Quaternion.identity) * localRot;

            // 黃色：clip 原樣（以自身 pivot 為錨）的 warp frame 落點
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(landingPos, 0.2f);
            Gizmos.DrawLine(landingPos, landingPos + landingRot * Vector3.forward * 0.5f);

            var root = EditorSampleRoot;
            if (root != null)
                Gizmos.DrawLine(root.position, landingPos);

            // 綠色：實際要貼齊的 target（有指定時），與落點的連線就是 warp 量
            if (_targetTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_targetTransform.position, 0.2f);
                Gizmos.DrawLine(landingPos, _targetTransform.position);
            }
        }
#endif
    }
}
