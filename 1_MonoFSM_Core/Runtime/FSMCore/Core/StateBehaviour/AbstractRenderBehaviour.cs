using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    /// <summary>
    /// 每個 render frame 被祖先的 IRenderInvoker（[Event] RenderLoop）叫一次的視覺更新單元：
    /// 只讀狀態、只改視覺，不改邏輯資料。進入該 render 區間的第一幀走 OnEnterRenderImplement，
    /// 之後每幀走 OnRenderImplement（多數子類兩者實作成同一件事）。
    /// 掛著的物件 inactive、或 _conditionGroup 不成立時整顆跳過（比照 AbstractStateAction.IsValid）。
    /// </summary>
    public abstract class AbstractRenderBehaviour : AbstractDescriptionBehaviour, IRenderBehaiour,
        ISceneStart
    {

        [ShowInInspector] [Required] [AutoParent]
        IRenderInvoker _iRenderInvoker;

        [AutoNested]
        [InlineField]
        [PropertyOrder(1)]
        public ConditionGroup _conditionGroup; //condition 成立，才能觸發 Render

        //比照 AbstractStateAction.IsValid，condition group 不成立就不觸發
        protected bool IsConditionValid => _conditionGroup.IsValid;

        protected override bool HasError()
        {
            _iRenderInvoker = GetComponentInParent<IRenderInvoker>();
            return _iRenderInvoker == null || base.HasError();
        }

        protected override string DescriptionTag => "Render";
        [ShowInDebugMode] private float _lastRenderTime;
        [ShowInDebugMode] private float _lastEnterRenderTime;

        [Button]
        void ForceTestOnEnter()
        {
            OnEnterRenderImplement();
        }

        [Button]
        void ForceTestRenderUpdate()
        {
            OnRenderImplement();
        }

        //fixme: 改 protected
        public abstract void OnEnterRenderImplement();

        public abstract void OnRenderImplement();
        public void OnEnterRender()
        {
            if (isActiveAndEnabled == false) //FIXME: 要有個bypass的方式？ checkWhileDisable?
                return;
            if (IsConditionValid == false)
                return;
            _lastEnterRenderTime = Time.time;
            OnEnterRenderImplement();
        }


        public void OnRender()
        {
            if (isActiveAndEnabled == false)
                return;
            if (IsConditionValid == false)
                return;
            _lastRenderTime = Time.time;
            OnRenderImplement();
        }

        public void EnterSceneStart()
        {
            //初始化要先跑一下？
            // OnEnterRender();
        }
    }
}
