using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM_Physics.Runtime.Interact.SpatialDetection.Gameplay
{
    //放在被打的
    public class CollisionDamageHitAction : AbstractStateAction, IArgEventReceiver<Collision>
    {
        public VarFloat _durability;

//hitEntity接過來？好怪
        protected override void OnActionExecuteImplement()
        {
            // throw new System.NotImplementedException();
        }

        public void ArgEventReceived(Collision arg)
        {
            //FIXME: 公式再想想看
            var v = arg.rigidbody.mass * arg.relativeVelocity.magnitude / 2;
            //min damage filter? 結果還要傳下去喔...
            _durability.AddBy(-v, this);
            Debug.Log(
                $"[CollisionDamageHitAction] ArgEventReceived: mass={arg.rigidbody.mass},relvel:{arg.relativeVelocity.magnitude} damage={v}, collision with {arg.gameObject.name}",
                arg.rigidbody);
        }
    }
}
