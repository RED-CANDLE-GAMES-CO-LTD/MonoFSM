using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider.Condition
{
    public class ParentValueExistCondition : AbstractConditionBehaviour
    {
        //includeSelf: false，否則會抓到自己（AbstractConditionBehaviour 也是 IValueProvider）造成無限遞迴
        [Required] [PreviewInInspector] [AutoParent(includeSelf: false)]
        private IValueProvider _parentValueProvider;

        protected override bool IsValid
        {
            get
            {
                //防禦：抓到自己會造成 IsValid -> IsValueExist -> FinalResult -> IsValid 無限遞迴 (stack overflow)
                if (ReferenceEquals(_parentValueProvider, this))
                {
                    Debug.LogError(
                        "[ParentValueExistCondition] _parentValueProvider 解析到自己，請確認上層有 IValueProvider",
                        this);
                    return false;
                }

                return _parentValueProvider is { IsValueExist: true };
            }
        }
    }
}