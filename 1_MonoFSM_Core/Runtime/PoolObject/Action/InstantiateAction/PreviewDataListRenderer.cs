using _1_MonoFSM_Core.Runtime._3_FlagData.DataFunction;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using _1_MonoFSM_Core.Runtime.MonoData;
using MonoFSM.Core.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.LifeCycle
{
    /// <summary>
    /// 把 VarListData（VarList&lt;GameData&gt;）每一項的 PreviewDataFunction.PreviewPrefab
    /// 依 index 顯示在事先擺好的 _slots anchor 下（純 View，走 SpawnVisual pool）。
    /// list 增減或內容改變時自動 diff：prefab 沒變的 slot 不動，變的才換。
    /// </summary>
    public class PreviewDataListRenderer : AbstractRenderBehaviour
    {
        public override string Description =>
            "Preview List: " + (_varListData != null ? _varListData.name : "?");

        [Required]
        [DropDownRef]
        public VarListData _varListData;

        [SerializeField]
        private Transform[] _slots; //事先擺好的 anchor，index 對應 list index

        [PreviewInInspector]
        private MonoObj[] _currentPrefabs;

        [PreviewInInspector]
        private MonoObj[] _currentInstances;

        public override void OnEnterRenderImplement()
        {
            if (_varListData == null || _slots == null)
                return;

            if (_currentPrefabs == null || _currentPrefabs.Length != _slots.Length)
            {
                _currentPrefabs = new MonoObj[_slots.Length];
                _currentInstances = new MonoObj[_slots.Length];
            }

            var list = _varListData.Value;
            if (list != null && list.Count > _slots.Length)
                Debug.LogWarning(
                    $"PreviewDataListRenderer: list count {list.Count} 超過 slot 數 {_slots.Length}，超出部分不顯示",
                    this);

            for (var i = 0; i < _slots.Length; i++)
            {
                var data = list != null && i < list.Count ? list[i] : null;
                var prefab = data?.GetDataFunction<PreviewDataFunction>()?.PreviewPrefab;
                if (prefab == _currentPrefabs[i])
                    continue; //沒變就不動

                ClearSlot(i);
                if (prefab == null || _slots[i] == null)
                    continue;

                var sim = _parentObj != null ? _parentObj.WorldUpdateSimulator : null;
                if (sim == null)
                {
                    Debug.LogError("PreviewDataListRenderer: No WorldUpdateSimulator found", this);
                    return;
                }

                var newObj = sim.SpawnVisual(prefab, _slots[i].position, _slots[i].rotation);
                if (newObj == null)
                    continue;

                newObj.transform.SetParent(_slots[i], true);
                newObj.gameObject.SetActive(true);
                _currentPrefabs[i] = prefab;
                _currentInstances[i] = newObj;
            }
        }

        public override void OnRenderImplement()
        {
        }

        private void ClearSlot(int i)
        {
            if (_currentInstances[i] != null && _parentObj != null)
                _parentObj.WorldUpdateSimulator?.DespawnVisual(_currentInstances[i]);
            _currentInstances[i] = null;
            _currentPrefabs[i] = null;
        }

        public void ClearAll()
        {
            if (_currentInstances == null)
                return;
            for (var i = 0; i < _currentInstances.Length; i++)
                ClearSlot(i);
        }
    }
}
