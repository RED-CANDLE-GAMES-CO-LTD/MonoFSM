using Sirenix.OdinInspector;

namespace MonoFSM.Core
{
    //把好幾個condition包起來, 只撈一層
    public class ConditionFolder : AbstractConditionBehaviour
    {
        [Component] [ShowInInspector] [AutoChildren(DepthOneOnly = true)]
        private AbstractConditionBehaviour[] _conditions;

        protected override bool IsValid
        {
            get
            {
                if (_conditions == null || _conditions.Length == 0)
                    return true;
                foreach (var condition in _conditions)
                {
                    if (condition == this)
                        continue;
                    if (condition == null)
                        continue;
                    if (condition.gameObject.activeSelf == false) //只看自己，可能是parent有人關
                        continue;
                    if (condition.FinalResult == false)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}