using Sirenix.OdinInspector;

namespace RCGMaker.Core
{
    //把好幾個condition包起來, 只撈一層
    public class ConditionFolder : AbstractConditionComp
    {
        [Component] [ShowInInspector] [AutoChildren(DepthOneOnly = true)]
        private AbstractConditionComp[] _conditions;

        protected override bool isValid
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