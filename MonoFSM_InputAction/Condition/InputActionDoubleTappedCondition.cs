using MonoFSM_InputAction;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Fusion.Addons.KCC.ECM2.Examples.Networking.Fusion_v2.Characters.Scripts.Input
{
    public class InputActionDoubleTappedCondition : AbstractConditionBehaviour
    {
        protected override bool IsValid
        {
            get
            {
                if (_inputAction == null)
                    return false;

                // 使用 WorldUpdateSimulator.SimulationTime，與 MonoInputAction.LastPressedTime 時間源一致，
                // 在 Fusion 下 Runner.SimulationTime 會被 tick 驅動，支援 resimulation。
                var currentTime = WorldUpdateSimulator.SimulationTime;

                if (_inputAction.WasPressed)
                {
                    if (_lastPressTime > 0 && currentTime - _lastPressTime <= _doubleTapTimeWindow)
                    {
                        this.Log("InputActionDoubleTappedCondition: Double tap detected!");
                        // _lastPressTime = -1; // 重置以避免三連擊被判定為兩次雙擊
                        _lastPressTime = currentTime;
                        return true;
                    }

                    _lastPressTime = currentTime;
                }

                // 如果超過時間窗口，重置計時器
                if (_lastPressTime > 0 && currentTime - _lastPressTime > _doubleTapTimeWindow)
                    _lastPressTime = -1;

                return false;
            }
        }

        [DropDownRef] public MonoInputAction _inputAction;

        [SerializeField] [Range(0.1f, 1.0f)] [Tooltip("連續兩次點擊之間的最大時間間隔（秒）")]
        private float _doubleTapTimeWindow = 0.3f;

        [SerializeField] [ReadOnly] [ShowInPlayMode]
        private float _lastPressTime = -1;

        public override string Description =>
            _inputAction != null ? $"{_inputAction.name} Double Tapped" : "No Input Action";
    }
}
