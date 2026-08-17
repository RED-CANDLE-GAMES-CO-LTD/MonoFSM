using System.Collections.Generic;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM.Core
{
    /// <summary>
    ///     Cheat 用：Alt + 1~9 瞬移玩家到指定位置，Alt + T 依序循環切換。
    ///     掛在有 IArgEventReceiver&lt;Vector3&gt; 子節點（例如 FusionCharacterTeleportAction）的 GameObject 上。
    /// </summary>
    //不繼承 AbstractDescriptionBehaviour：它會用 Description 去改 GameObject 名字，
    //會蓋掉共用節點（例如 [SpawnPoint] PlayerStartSpawnPoint）原本的名稱
    public class CheatTeleportPoints : MonoBehaviour, IUpdateSimulate
    {
        //場上所有 CheatTeleportPoint 自動蒐集，順序即 Alt+1、Alt+2…
        [ShowInInspector]
        [ReadOnly]
        private List<CheatTeleportPoint> Points => CheatTeleportPoint.GetSortedPoints();

        [CompRef] [AutoChildren] [ShowInInspector]
        private IArgEventReceiver<Vector3> _playerTeleporter;

        [ShowInInspector] [ReadOnly] private int _currentIndex = -1;

        private static readonly Key[] _digitKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };

        public void Simulate(float deltaTime)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (!keyboard.leftAltKey.isPressed && !keyboard.rightAltKey.isPressed)
                return;

            //Alt+Shift+數字 是火車傳送（CheatTeleportSplineTrainPoints），不要一起觸發玩家瞬移
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                return;

            for (var i = 0; i < _digitKeys.Length; i++)
                if (keyboard[_digitKeys[i]].wasPressedThisFrame)
                {
                    TeleportToIndex(i);
                    return;
                }

            if (keyboard.tKey.wasPressedThisFrame)
                TeleportToNext();
        }

        [Button]
        public void TeleportToNext()
        {
            var points = Points;
            if (points.Count == 0)
            {
                Debug.LogWarning("[CheatTeleport] 場上沒有任何 CheatTeleportPoint", this);
                return;
            }

            TeleportToIndex((_currentIndex + 1) % points.Count);
        }

        [Button]
        public void TeleportToIndex(int index)
        {
            var points = Points;
            if (index < 0 || index >= points.Count)
            {
                Debug.LogWarning($"[CheatTeleport] Alt+{index + 1} 沒有對應的傳送點（場上共 {points.Count} 個）", this);
                return;
            }

            var point = points[index].transform;
            if (_playerTeleporter == null)
            {
                Debug.LogWarning("[CheatTeleport] 找不到子節點上的 IArgEventReceiver<Vector3>，瞬移沒有作用", this);
                return;
            }

            _currentIndex = index;
            Debug.Log($"[CheatTeleport] Alt+{index + 1} 瞬移到 {point.name} {point.position}", point);
            _playerTeleporter.ArgEventReceived(point.position);
        }
    }
}
