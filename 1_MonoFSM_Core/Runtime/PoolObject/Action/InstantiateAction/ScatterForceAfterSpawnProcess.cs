using JetBrains.Annotations;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// spawn 後對物件的 Rigidbody 施加「從中心向外炸 + 隨機擴散」的 impulse。
    /// 向外方向由 (物件位置 - center) 回推，等價於 scatter pattern 的 rotation * offset，
    /// 因此不需要 offset 進 IAfterSpawnProcess 簽章也能保留精準向外的效果。
    /// 與 ShootWithDirectionAfterProcess（單一方向、設 linearVelocity）用途不同。
    /// </summary>
    public class ScatterForceAfterSpawnProcess : MonoBehaviour, IAfterSpawnProcess
    {
        [Tooltip("向外炸的中心點，通常填 SpawnAction / SpawnTableAction 的 spawnPosition")]
        [SerializeField]
        private Transform _centerTransform;

        [SerializeField]
        private float _forceStrength = 5f;

        [Tooltip("主要飛出方向（會根據 rotation 旋轉）")]
        [SerializeField]
        private Vector3 _forceDirection = Vector3.up;

        [Range(0f, 1f)]
        [Tooltip("方向擴散程度 (0=集中, 1=全隨機)")]
        [SerializeField]
        private float _forceSpread = 0.3f;

        public void AfterSpawn(
            MonoObj obj,
            Vector3 position,
            Quaternion rotation,
            [CanBeNull] GeneralEffectHitData hitData
        )
        {
            var rb = obj.GetCompCache<Rigidbody>();
            if (rb == null) return;

            var center = _centerTransform != null ? _centerTransform.position : position;
            var outward = position - center; // == 原本 scatter 的 rotation * offset

            // 基礎方向：有向外分量就用「向外 + 設定方向」，否則只用設定方向
            Vector3 baseDirection;
            if (outward.sqrMagnitude > 0.001f)
                baseDirection = outward.normalized + rotation * _forceDirection.normalized;
            else
                baseDirection = rotation * _forceDirection.normalized;

            baseDirection = baseDirection.normalized;

            var finalDirection =
                Vector3.Lerp(baseDirection, Random.insideUnitSphere, _forceSpread).normalized;

            rb.AddForce(finalDirection * _forceStrength, ForceMode.Impulse);
            Debug.Log($"ScatterForceAfterSpawnProcess: force {finalDirection * _forceStrength}", rb);
        }
    }
}
