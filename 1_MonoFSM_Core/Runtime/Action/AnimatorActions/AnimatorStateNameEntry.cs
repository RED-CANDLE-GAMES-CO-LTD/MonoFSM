using System.Collections.Generic;
using MonoFSM.Foundation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

//掛在 AnimatorStateNameSource 底下，condition 成立時提供 state name 蓋掉 AnimatorPlayAction 的 stateName
public class AnimatorStateNameEntry : AbstractDescriptionBehaviour
{
    public override string Description => " -> " + _stateName;

    protected override string DescriptionTag => "StateName";

    [AutoParent] private AnimatorStateNameSource _source;

    //同節點或子節點上的條件
    [AutoNested] [SerializeField] protected ConditionGroup _conditionGroup;

#if UNITY_EDITOR
    [ValueDropdown(nameof(GetStateNames), NumberOfItemsBeforeEnablingSearch = 3)]
#endif
    [SerializeField]
    private string _stateName;

    public bool IsMatch => _conditionGroup.IsValid;

    public string StateName => _stateName;

    private int _stateHash;
    private bool _hasCachedStateHash;

    public int StateHash
    {
        get
        {
            if (!_hasCachedStateHash)
            {
                _stateHash = Animator.StringToHash(_stateName);
                _hasCachedStateHash = true;
            }

            return _stateHash;
        }
    }

#if UNITY_EDITOR
    private IEnumerable<string> GetStateNames()
    {
        if (_source == null)
            _source = GetComponentInParent<AnimatorStateNameSource>(true);
        return _source != null ? _source.GetStateNames() : null;
    }
#endif
}
