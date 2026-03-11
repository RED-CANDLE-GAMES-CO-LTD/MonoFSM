using MonoFSM.Core.LifeCycle;
using MonoFSM.Core.Runtime.Action;
using UnityEngine;

namespace MonoFSM_Physics.Runtime
{
    public class ArrowInitAction : AbstractStateAction
    {
        // public Rigidbody _rigidbody;
        //FIXME: 第一個 frame 的effectDetector的raycast 是錯的喔！
        protected override void OnActionExecuteImplement()
        {
            var schema = SelfEntity.GetSchema<ProjectileSchema>();
            var initVel = schema._initVel.Value;
            var rb = schema._rigidbody;
            var oriRbVel = rb.linearVelocity;
            rb.isKinematic = false;
            //速度
            rb.linearVelocity = initVel; //可以寫在Schema裡？然後就只call一行？

            //旋轉
            var targetRot = Quaternion.LookRotation(initVel, Vector3.up);
            rb.transform.rotation = targetRot; //用transform才會對

            Debug.Log(
                "ArrowInitAction set vel and rot: " + initVel + " oriRbVel:" + oriRbVel,
                this
            );
            Debug.DrawLine(rb.position, rb.position + initVel, Color.blue, 5f);
            // Debug.Break();
        }
    }
}
