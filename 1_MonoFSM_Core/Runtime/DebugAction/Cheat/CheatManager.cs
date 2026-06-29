using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MonoFSM.Core
{
    public class CheatManager : AbstractDescriptionBehaviour, IUpdateSimulate
    {
        public void CheatKeyCheck()
        {

            if (Keyboard.current[Key.LeftMeta].isPressed ||
                Keyboard.current[Key.LeftCtrl].isPressed)
            {
                //重置關卡
                if (
                    Keyboard.current[Key.R].wasPressedThisFrame)
                {
                    if (
                        Keyboard.current[Key.LeftShift].isPressed)
                        WorldUpdateSimulator.ManualResetLevel(true);
                    else
                    {
                        WorldUpdateSimulator.ManualResetLevel();
                    }
                }
                // 在這裡執行作弊行為，例如增加分數、解鎖功能等
            }

            if (Keyboard.current.digit0Key.IsPressed() || Mouse.current.middleButton.isPressed)
            {
                WorldUpdateSimulator.TimeScale = 5f;
                Debug.Log(" WorldUpdateSimulator.TimeScale = 5f;");
            }

            else
                WorldUpdateSimulator.TimeScale = 1f;
        }

        public void Simulate(float deltaTime)
        {
            CheatKeyCheck();
        }
    }
}
