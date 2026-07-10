using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Variable;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// Visual-only Spawn Action，跑在 Render Tier (OnEnterRender)。
    /// 不參與 Simulate / 網路同步，純粹用於特效、預覽物件等視覺呈現。
    /// 實作 IArgRenderBehaviour&lt;GeneralEffectHitData&gt;，收到 hit 資料時直接在 hitPoint spawn。
    /// </summary>
    public class SpawnVisualAction : AbstractRenderBehaviour, IPoolObjectPlayer,
        IArgRenderBehaviour<GeneralEffectHitData>
    {
        public override string Description =>
            "SpawnVisual " + (_poolObjFoldOut?.Value != null ? _poolObjFoldOut.Value.name : "?");

        [InlineField] [SerializeField] private VarMonoObjFoldOut _poolObjFoldOut;

        [BoxGroup("Scale")] [HideIf(nameof(_scaleRatio))] [SerializeField]
        private bool _isUsingSpawnTransformScale;

        [BoxGroup("Scale")] [SerializeField] private VarFloat _scaleRatio;

        [SerializeField] private Transform _spawnPosition;

        [SerializeField] private VarVector3 _spawnPositionV3;

        [SerializeField] private bool _isRotationIdentity;

        private Vector3 SpawnPos =>
            _spawnPosition != null
                ? _spawnPosition.position
                : (_spawnPositionV3 != null ? _spawnPositionV3.Value : transform.position);

        private Quaternion SpawnRot =>
            _isRotationIdentity ? Quaternion.identity
            : _spawnPosition != null ? _spawnPosition.rotation
            : transform.rotation;

        private MonoObj Prefab => _poolObjFoldOut?.Value;


        //local idendity?
        [GUIColor(0.4f, 1f, 0.4f)] [PreviewInInspector]
        private MonoObj _lastSpawnedObj;

        public override void OnEnterRenderImplement()
        {
            SpawnAt(SpawnPos, SpawnRot);
        }

        //收到 hit 資料版本：有 hitPoint 就在 hitPoint spawn，並用 hitNormal 對齊朝向
        public void OnArgEnterRender(GeneralEffectHitData arg)
        {
            if (arg?.hitPoint is { } hitPoint)
            {
                var rot = arg.hitNormal is { } normal
                    ? Quaternion.LookRotation(normal)
                    : SpawnRot;
                SpawnAt(hitPoint, rot);
            }
            else
            {
                SpawnAt(SpawnPos, SpawnRot);
            }
        }

        public void OnArgRender(GeneralEffectHitData arg)
        {
            //應該用不到對ㄅ
        }

        private void SpawnAt(Vector3 spawnPos, Quaternion spawnRot)
        {
            var prefab = Prefab;
            if (prefab == null)
            {
                Debug.LogError("SpawnVisualAction: Prefab is null", this);
                return;
            }

            if (_parentObj == null)
            {
                Debug.LogError("SpawnVisualAction: _parentObj is null", this);
                return;
            }

            var sim = _parentObj.WorldUpdateSimulator;
            if (sim == null)
            {
                Debug.LogError(
                    $"SpawnVisualAction:{name}, No WorldUpdateSimulator found in _parentObj",
                    _parentObj);
                return;
            }

            var newObj = sim.SpawnVisual(prefab, spawnPos, spawnRot);
            if (newObj == null)
                return;

            if (_scaleRatio != null)
                newObj.transform.localScale = Vector3.one * _scaleRatio.CurrentValue;
            else if (_isUsingSpawnTransformScale)
                newObj.transform.localScale = transform.lossyScale;

            newObj.gameObject.SetActive(true);
            _lastSpawnedObj = newObj;
            newObj.GetComponent<PoolObject>().lastPlayer = this;
        }

        public override void OnRenderImplement()
        {
            //應該用不到對ㄅ
        }
    }
}
