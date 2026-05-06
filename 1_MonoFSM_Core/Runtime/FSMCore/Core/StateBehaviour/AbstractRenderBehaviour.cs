using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour
{
    public abstract class AbstractRenderBehaviour : AbstractDescriptionBehaviour, IRenderBehaiour
    {
        [ShowInInspector] [Required] [AutoParent]
        RenderInvoker _renderInvoker;

        protected override string DescriptionTag => "Render";
        [ShowInDebugMode] private float _lastRenderTime;
        [ShowInDebugMode] private float _lastEnterRenderTime;
        public abstract void OnEnterRenderImplement();

        public virtual void OnRenderImplement()
        {
        }

        public void OnEnterRender()
        {
            if (isActiveAndEnabled == false)
                return;
            _lastEnterRenderTime = Time.time;
            OnEnterRenderImplement();
        }


        public void OnRender()
        {
            if (isActiveAndEnabled == false)
                return;
            _lastRenderTime = Time.time;
            OnRenderImplement();
        }
    }
}
