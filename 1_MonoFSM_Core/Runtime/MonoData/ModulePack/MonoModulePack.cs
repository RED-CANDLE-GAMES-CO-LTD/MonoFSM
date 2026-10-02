using _1_MonoFSM_Core.Runtime.Attributes;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core;
using MonoFSM.CustomAttributes;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.LifeCycle.Update
{
    /// <summary>
    /// 掛進宿主 entity 的模組：放在宿主直屬 <c>Modules</c>（MonoModuleFolder）底下，宿主 EnterSceneAwake 時
    /// <c>MonoEntity.BindModulePackFolders</c> 把 pack 的 VariableFolder / StateFolder / EffectDetectable / SchemaFolder
    /// 依型別當 external source 併進宿主的同型 folder（var 用宿主 tag 查得到、state 跟宿主互斥、receiver 掛在宿主 detectable）。
    /// 是 <see cref="IEntityScopeBoundary" />：宿主自己的 folder 掃描不會鑽進 pack 把 pack 的 folder 當成宿主的。
    /// </summary>
    public class MonoModulePack : MonoBehaviour, IDropdownRoot, IEntityScopeBoundary,
        IBeforePrefabSaveCallbackReceiver
    {
        /// <summary>
        /// pack 的 receiver 要被哪些 collider 打到。
        /// </summary>
        public enum ReceiverScope
        {
            /// <summary>預設（舊行為）：pack 的 EffectDetectable 併進宿主的，打到宿主任何一顆 collider 都算。</summary>
            HostColliders,

            /// <summary>
            /// pack 的 EffectDetectable 不併進宿主，只有 pack 自己 [Geometry] 底下的判定 collider 打得到。
            /// 代價：用「問 entity 有沒有某 receiver」的路（PlayerInteractState 按 E、IsEffectDealerOrReceiverCondition、
            /// ForceTriggerEffectAction、up hit）看不到 pack 的 receiver。
            /// </summary>
            OwnGeometryOnly,
        }

        [Tooltip("HostColliders = 打到宿主任何 collider 都觸發 pack 的 receiver（預設、舊行為）；" +
                 "OwnGeometryOnly = 只有 pack 自己 [Geometry] 底下的 collider 打得到")]
        [SerializeField] private ReceiverScope _receiverScope = ReceiverScope.HostColliders;

        public ReceiverScope Scope => _receiverScope;

        #region Geometry 掛點

        [Tooltip("pack 底下要長在宿主身上的那包幾何（掛 ModulePackGeometry 的 [Geometry] 節點）。" +
                 "宿主 EnterSceneAwake 時 reparent 到宿主掛點（MonoEntity.ModuleGeometryAnchor，預設 ViewRoot.Root）。留空 = 不搬（舊行為）")]
        [SerializeField] private ModulePackGeometry _geometryRoot;

        public ModulePackGeometry GeometryRoot => _geometryRoot;

        //相對宿主掛點的 local pose，宿主存檔時記（pack 自己存檔時沒有宿主，不動）。
        //不能用 world pose：宿主的 Context 會被 SplineMover / 物理移動，SceneAwake 時 pack 跟掛點的相對位置已經不是 edit-time 那個
        [SerializeField] [ReadOnly] [ShowIf(nameof(HasGeometry))] private bool _hasGeometryPose;
        [SerializeField] [ReadOnly] [ShowIf(nameof(HasGeometry))] private Vector3 _geometryLocalPosition;
        [SerializeField] [ReadOnly] [ShowIf(nameof(HasGeometry))] private Quaternion _geometryLocalRotation = Quaternion.identity;
        [SerializeField] [ReadOnly] [ShowIf(nameof(HasGeometry))] private Vector3 _geometryLocalScale = Vector3.one;

        private bool HasGeometry => _geometryRoot != null;

        public enum GeometryAttachResult
        {
            NotAttempted,
            NoGeometry,
            Attached,
            /// <summary>搬過去了，但掛點不在宿主 entity 底下（例：nested entity 的 ViewRoot.Root 是外層的 Rigidbody）。宿主要設 _moduleGeometryAnchor</summary>
            Attached_AnchorOutsideHost,
            AttachedKeepWorldPose_NoRecordedPose,
            Fail_NoAnchor,
        }

        [ShowInInspector] [ReadOnly] [ShowIf(nameof(HasGeometry))]
        private GeometryAttachResult _lastAttachResult;

        [ShowInInspector] [ReadOnly] [ShowIf(nameof(HasGeometry))]
        private Transform _lastAttachAnchor;

        /// <summary>
        /// 宿主 EnterSceneAwake 呼叫：把 [Geometry] 搬到宿主掛點，套回存檔時記的 local pose。
        /// 重複呼叫（pool 重生）是冪等的。
        /// </summary>
        public void AttachGeometry(Transform anchor)
        {
            if (_geometryRoot == null)
            {
                _lastAttachResult = GeometryAttachResult.NoGeometry;
                return;
            }

            _lastAttachAnchor = anchor;
            if (anchor == null)
            {
                _lastAttachResult = GeometryAttachResult.Fail_NoAnchor;
                Debug.LogError($"[MonoModulePack] {name}: 宿主找不到 geometry 掛點（沒有 ViewRoot 也沒設 _moduleGeometryAnchor），[Geometry] 留在 pack 底下", this);
                return;
            }

            var geo = _geometryRoot.transform;
            if (!_hasGeometryPose)
            {
                if (geo.parent != anchor)
                    geo.SetParent(anchor, true);
                _lastAttachResult = GeometryAttachResult.AttachedKeepWorldPose_NoRecordedPose;
                Debug.LogWarning($"[MonoModulePack] {name}: 沒有記到相對掛點的 pose（宿主 prefab 存一次檔就會記），先保留 world pose", this);
                return;
            }

            if (geo.parent != anchor)
                geo.SetParent(anchor, false);
            geo.localPosition = _geometryLocalPosition;
            geo.localRotation = _geometryLocalRotation;
            geo.localScale = _geometryLocalScale;
            _lastAttachResult = GeometryAttachResult.Attached;
            var host = transform.parent != null ? transform.parent.GetComponentInParent<MonoFSM.Runtime.MonoEntity>(true) : null;
            if (host != null && !anchor.IsChildOf(host.transform))
            {
                _lastAttachResult = GeometryAttachResult.Attached_AnchorOutsideHost;
                Debug.LogWarning($"[MonoModulePack] {name}: geometry 掛點 {anchor.name} 不在宿主 {host.name} 底下（多半是 nested entity 的 ViewRoot.Root 指到外層剛體），宿主要設 _moduleGeometryAnchor", this);
            }
        }

        public void OnBeforePrefabSave()
        {
#if UNITY_EDITOR
            if (_geometryRoot == null)
                return;
            SyncGeometryDetectTargets();
            RecordGeometryPose();
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// [Geometry] 底下的 detect target 搬走後 AutoParent 找不到 pack 的 EffectDetectable，存檔時明確寫進 _detectableOverride
        /// （跟 DismantlePartsGroup.Sync 同一招；_detectable 是 AutoParent，手填會被蓋掉）。
        /// </summary>
        private void SyncGeometryDetectTargets()
        {
            var own = FindOwnDetectable();
            if (own == null)
            {
                Debug.LogWarning($"[MonoModulePack] {name}: [Geometry] 外面找不到 pack 自己的 EffectDetectable，detect target 沒有指回去", this);
                return;
            }

            foreach (var target in _geometryRoot.GetComponentsInChildren<BaseEffectDetectTarget>(true))
            {
                var so = new UnityEditor.SerializedObject(target);
                var prop = so.FindProperty("_detectableOverride");
                if (prop == null || prop.objectReferenceValue == own)
                    continue;
                prop.objectReferenceValue = own;
                so.ApplyModifiedPropertiesWithoutUndo();
                UnityEditor.EditorUtility.SetDirty(target);
            }
        }

        private EffectDetectable FindOwnDetectable()
        {
            foreach (var d in GetComponentsInChildren<EffectDetectable>(true))
                if (!d.transform.IsChildOf(_geometryRoot.transform))
                    return d;
            return null;
        }

        private void RecordGeometryPose()
        {
            var host = GetComponentInParent<MonoFSM.Runtime.MonoEntity>(true);
            if (host == null)
                return; //pack 自己的 prefab：沒有宿主，pose 等宿主存檔再記
            var anchor = host.ModuleGeometryAnchor;
            if (anchor == null)
            {
                Debug.LogWarning($"[MonoModulePack] {name}: 宿主 {host.name} 找不到 geometry 掛點，pose 沒記", this);
                return;
            }

            if (!anchor.IsChildOf(host.transform))
                Debug.LogWarning($"[MonoModulePack] {name}: geometry 掛點 {anchor.name} 不在宿主 {host.name} 底下，宿主要設 _moduleGeometryAnchor", this);

            var geo = _geometryRoot.transform;
            if (geo.parent == anchor)
                return; //已經被搬過（play mode 存檔之類），不重記

            var pos = anchor.InverseTransformPoint(geo.position);
            var rot = Quaternion.Inverse(anchor.rotation) * geo.rotation;
            var a = anchor.lossyScale;
            var g = geo.lossyScale;
            var scale = new Vector3(SafeDiv(g.x, a.x), SafeDiv(g.y, a.y), SafeDiv(g.z, a.z));

            if (_hasGeometryPose && pos == _geometryLocalPosition && rot == _geometryLocalRotation &&
                scale == _geometryLocalScale)
                return;
            _hasGeometryPose = true;
            _geometryLocalPosition = pos;
            _geometryLocalRotation = rot;
            _geometryLocalScale = scale;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private static float SafeDiv(float a, float b) => Mathf.Approximately(b, 0f) ? a : a / b;
#endif

        #endregion

        [CompRef] [AutoChildren] VariableFolder _variableFolder;
        [CompRef] [AutoChildren] StateFolder _stateFolder;
        [CompRef] [AutoChildren] EffectDetectable _detectable;
        [CompRef] [AutoChildren] SchemaFolder _folder;

        /// <summary>
        /// 返回此 ModulePack 下所有的 MonoDictFolder
        /// </summary>
        public IMonoDictFolder[] GetAllFolders()
        {
            return GetComponentsInChildren<IMonoDictFolder>(true);
        }

        [CompRef] [AutoChildren] IMonoDictFolder[] _moduleFolders;
    }
}
