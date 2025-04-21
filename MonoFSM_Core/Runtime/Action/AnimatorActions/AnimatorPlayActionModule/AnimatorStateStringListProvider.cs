using System.Collections.Generic;

using UnityEngine;

using Sirenix.OdinInspector;

//提供給動畫用的string list, 但是hash效率比較好，盡量用StateHashValue
public class AnimatorStateStringListProvider : AbstractStringProvider
{
    public Animator animator;

    [ValueDropdown("GetAnimatorStateNames")] //, IsUniqueList = true???
    public List<string> list;

    private List<int> hashList;
    [Required] public VarInt currentIndex;

    public override string StringValue =>
        currentIndex.CurrentValue < 0 || 
        currentIndex.CurrentValue >= list.Count 
            ? string.Empty 
            : list[currentIndex.CurrentValue];

    public int StateHashValue 
        => currentIndex.CurrentValue < 0 || 
           currentIndex.CurrentValue >= hashList.Count
            ? 0
            : hashList[currentIndex.CurrentValue];

    private void Awake()
    {
        hashList = new List<int>();
        foreach (var name in list)
        {
            hashList.Add(Animator.StringToHash(name));
        }
    }

    int stateLayer => 0;

    public bool HasCurrentAnimation 
        => 0 <= currentIndex.CurrentValue && 
           currentIndex.CurrentValue < list.Count;

    //FIXME: 底下內容duplicate code
#if UNITY_EDITOR
    bool IsStateNameNotInAnimator(string name)
    {
        var names = GetAnimatorStateNames();
        if (names == null)
            return true;
        foreach (var _name in names)
        {
            if (_name == name)
                return false;
        }

        return true;
    }

    //拿動畫上的所有state name
    private IEnumerable<string> GetAnimatorStateNames() 
        => AnimatorHelpler.GetAnimatorStateNames(animator, stateLayer);
#endif
}