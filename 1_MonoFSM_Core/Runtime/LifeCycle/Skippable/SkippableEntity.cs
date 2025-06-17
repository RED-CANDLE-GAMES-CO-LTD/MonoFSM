using System.Collections;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

public class SkippableEntity : MonoBehaviour,IResetter
{
    [PreviewInInspector]
    [Auto()]
    private ISkippable _skippable;

    //對方不在GameLevel裡面 如 DialoguePlayer.cs 的 SkippableEntity
    public void Awake()
    {
#if RCG_DEV
        SkippableManager.Instance.RegisterSkippable(_skippable);
#endif
    }

    public void EnterLevelReset()
    {
#if RCG_DEV
        SkippableManager.Instance.RegisterSkippable(_skippable);
#endif
    }

    public void ExitLevelAndDestroy()
    {
#if RCG_DEV
        SkippableManager.Instance.UnRegisterSkippable(_skippable);
#endif
    }
    
    private void OnDestroy() {
        //FIXME: 這裡只是緊急修復用，實際上根本原因是 ExitLevelAndDestroy 沒有被正確呼叫到
        //現在這個 patch 只是加保險，不過反而會造成重複呼叫的 cost
        //參閱: https://app.clickup.com/9018329649/v/dc/8crhjhh-378/8crhjhh-3558
#if RCG_DEV
        if (SkippableManager.IsAvailable()) {
            SkippableManager.Instance.UnRegisterSkippable(_skippable);

        }
#endif
    }
}
