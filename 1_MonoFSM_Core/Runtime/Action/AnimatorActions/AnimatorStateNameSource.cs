using System.Collections.Generic;
using MonoFSM.Animation;
using MonoFSM.Variable.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

//依 child entry 順序判斷 condition，第一個成立的 entry 提供 state name，都不成立就用 default
public class AnimatorStateNameSource : AbstractStringProvider, IStateHashProvider
{
    [Auto] private AnimatorPlayAction _animatorPlayAction; //同節點

    [SerializeField]
    [CompRef]
    [AutoChildren(DepthOneOnly = true)]
    private AnimatorStateNameEntry[] _entries;

#if UNITY_EDITOR
    [ValueDropdown(nameof(GetStateNames), NumberOfItemsBeforeEnablingSearch = 3)]
#endif
    [SerializeField]
    private string _defaultStateName;

    public override string StringValue
    {
        get
        {
            var entry = GetActiveEntry();
            return entry != null ? entry.StateName : _defaultStateName;
        }
    }

    private int _defaultStateHash;
    private bool _hasCachedDefaultHash;

    public int StateHashValue
    {
        get
        {
            var entry = GetActiveEntry();
            if (entry != null)
                return entry.StateHash;

            if (!_hasCachedDefaultHash)
            {
                _defaultStateHash = Animator.StringToHash(_defaultStateName);
                _hasCachedDefaultHash = true;
            }

            return _defaultStateHash;
        }
    }

    private AnimatorStateNameEntry GetActiveEntry()
    {
        if (_entries == null)
            return null;

        for (var i = 0; i < _entries.Length; i++)
        {
            var entry = _entries[i];
            if (entry != null && entry.IsMatch)
                return entry;
        }

        return null;
    }

#if UNITY_EDITOR
    public IEnumerable<string> GetStateNames()
    {
        if (_animatorPlayAction == null)
            _animatorPlayAction = GetComponent<AnimatorPlayAction>();
        return _animatorPlayAction != null
            ? _animatorPlayAction.GetAnimatorStateNamesWithNone()
            : null;
    }
#endif
}
