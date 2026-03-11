using System;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSM.Runtime.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.EffectHit.Action
{
    /// <summary>
    ///     想要把打到的目標設定到一個VarEntity變數裡面
    ///     //FIXME: EffectHit類的好像可以抽成一組Handler? 這樣EffectEnter和EffectExit都可以做事？
    /// //FIXME: 應該讓EffectEnterNode做？
    /// </summary>
    [Obsolete("應該讓EffectEnterNode做? 還是有成對的問題？不對是local應該可刪掉")]
    public class EffectHitSetVarEntityAction : AbstractArgEventHandler<GeneralEffectHitData>
    {
        [Required]
        [DropDownRef]
        public VarEntity _targetVar; // 目標變數

        public enum EffectHitState
        {
            Enter,
            Exit,
        }

        public enum EffectHitTarget
        {
            Dealer,
            Receiver,
        }

        //FIXME: 還是應該自動判定？如果上面有EffectReceiver, 就一定選Dealer
        public EffectHitTarget _effectHitTarget = EffectHitTarget.Receiver;
        public EffectHitState _effectHitState = EffectHitState.Enter;

        //Toggle SetTo Null?

        protected override void OnActionExecuteImplement()
        {
            // 從 OnActionExecuteImplement 執行時無法取得碰撞資料
            // 實際的邏輯在 ArgEventReceived 中處理
        }

        // public void ArgEventReceived(GeneralEffectHitData arg)
        // {
        //
        // }

        protected override void OnArgEventReceived(GeneralEffectHitData arg)
        {
            //FIXME: 不一定是Receiver啊...
            var monoEntity =
                _effectHitTarget == EffectHitTarget.Receiver
                    ? arg.GeneralReceiver.SelfEntity
                    : arg.GeneralDealer.SelfEntity;
            if (_targetVar == null)
            {
                Debug.LogError("EffectHitSetVarEntityAction: Target variable is null", this);
                return;
            }

            //FIXME: 這個可能非對稱耶，好像還是list比較對...?
            switch (_effectHitState)
            {
                // 設定目標實體到變數
                case EffectHitState.Enter:
                    // if (_targetVar.Value == monoEntity)
                    //     _targetVar.SetValue(null, this);
                    // else
                    _targetVar.SetValue(monoEntity, this);
                    Debug.Log(
                        $"EffectHitSetVarEntityAction (Enter): Set {monoEntity?.name} to {_targetVar.name}",
                        this
                    );
                    break;
                // 清空變數（設為 null）
                case EffectHitState.Exit:
                    _targetVar.SetValue(null, this);
                    Debug.Log($"EffectHitSetVarEntityAction (Exit): Clear {_targetVar.name}", this);
                    break;
            }
        }
    }
}
