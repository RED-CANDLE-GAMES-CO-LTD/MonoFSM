using MonoFSM.Core.Attributes;
using MonoFSM.Core.Simulate;
using MonoFSM.Foundation;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;

namespace MonoFSM.Core.Condition
{
    //這個要整個Panel OnEnable的時候才會檢查一遍，不會隨時檢查
    //ActivateChecker
    public abstract class //IReturnToPool? IDespawn?
        AbstractConditionActivateRunner : AbstractDescriptionBehaviour, IUpdateSimulate,
        IResetStart //, ISelectedInstanceUpdater //ISubmitHandler
    {
        protected override string DescriptionTag => "Condition Activate";

        /// <summary>
        /// 要不要做成disable還會檢查的simulate?
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Simulate(float deltaTime)
        {
            //proxy不會跑唷
            ActivateCheck();
        }


        [InlineField]
        [AutoNested]
        public ConditionGroup _conditionGroup;

        // [PreviewInInspector] protected virtual bool result => _conditionGroup.IsValid;

        public void ActivateCheck()
        {
            Profiler.BeginSample("ConditionActivateCheck");
            var result = _conditionGroup.IsValid;
            Profiler.EndSample();

            ActivateCheckImplement(result);
        }

        protected abstract void ActivateCheckImplement(bool isValid); //last result?

        public void ResetStart() //開始時先檢查
        {
            ActivateCheck();
        }
    }
}
