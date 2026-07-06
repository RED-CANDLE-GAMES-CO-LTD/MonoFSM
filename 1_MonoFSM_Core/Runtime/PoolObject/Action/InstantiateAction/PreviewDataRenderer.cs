using _1_MonoFSM_Core.Runtime._3_FlagData.DataFunction;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// 讀 VarGameData 目前指到的 GameData，
    /// 取其 PreviewDataFunction.PreviewPrefab，用 SpawnVisual 顯示在 _anchor 下（純 View）。
    /// prefab 沒變時不重生成；GameData 為 null 或沒有 PreviewDataFunction 時自動回收。
    /// 取代「事先擺一串 GameObject + SetGameObjectActiveByIndexAction」的做法。
    /// </summary>
    public class PreviewDataRenderer : AbstractRenderBehaviour
    {
        public override string Description =>
            "Preview: " + (_gameData._var != null ? _gameData._var.name : _gameData.Value?.name);

        [SerializeField] private VarGameDataWrapper _gameData;

        [SerializeField] private Transform _anchor; //preview 生成後掛在這個 transform 下，沒填就用自己

        [PreviewInInspector] private MonoObj _currentPrefab;

        [PreviewInInspector] private MonoObj _currentInstance;

        public override void OnEnterRenderImplement()
        {
            var data = _gameData?.Value;
            var prefab = data?.GetDataFunction<PreviewDataFunction>()?.PreviewPrefab;
            if (prefab == _currentPrefab)
                return; //沒變就不動

            ClearPreview();
            if (prefab == null)
                return;

            var sim = _parentObj != null ? _parentObj.WorldUpdateSimulator : null;
            if (sim == null)
            {
                Debug.LogError("PreviewDataRenderer: No WorldUpdateSimulator found", this);
                return;
            }

            var anchor = _anchor != null ? _anchor : transform;
            var newObj = sim.SpawnVisual(prefab, anchor.position, anchor.rotation);
            if (newObj == null)
                return;

            newObj.transform.SetParent(anchor, true);
            newObj.gameObject.SetActive(true);
            _currentPrefab = prefab;
            _currentInstance = newObj;
            // Debug.Log($"PreviewDataRenderer: Spawn preview {prefab.name}", this);
        }

        public override void OnRenderImplement()
        {
        }

        public void ClearPreview()
        {
            if (_currentInstance != null && _parentObj != null)
                _parentObj.WorldUpdateSimulator?.DespawnVisual(_currentInstance);
            _currentInstance = null;
            _currentPrefab = null;
        }
    }
}
