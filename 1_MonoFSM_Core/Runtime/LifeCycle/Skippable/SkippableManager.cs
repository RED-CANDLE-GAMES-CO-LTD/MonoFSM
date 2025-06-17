using System;
using System.Collections;
using System.Collections.Generic;
using Auto.Utils;
using MonoFSM.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

public class SkippableManager : SingletonBehaviour<SkippableManager>, IClearReference
{
    [PreviewInInspector] private List<ISkippable> allSkippables = new();
    
    public void RegisterSkippable(ISkippable skippable)
    {
#if RCG_DEV
        if (allSkippables.Contains(skippable) == false)
        {
            allSkippables.Add(skippable);
        }
#endif
    }
    
    public bool UnRegisterSkippable(ISkippable skippable)
    {
#if RCG_DEV
        if (allSkippables.Contains(skippable) == true)
        {
            allSkippables.Remove(skippable);
            return true;
        }
#endif
        return false;
    }
    List<ISkippable> validSkippable = new List<ISkippable>();
    public void TrySkip()
    {
        validSkippable.Clear();
        validSkippable.AddRange(allSkippables);
        validSkippable.RemoveAll((s) => s == null);
        validSkippable.RemoveAll((s)=>s.CanSkip==false);
        
        foreach (var skip in validSkippable)
        {
            try
            {
                if (skip.CanSkip)
                {
                    // Debug.Log("Skip:"+skip.GameObject(),skip.GameObject());
                    skip.TrySkip();
                }
            }
            catch (Exception e)
            {
                continue;
            }
        
        }
        validSkippable.Clear(); // prevent from leak.
    }

    public void ClearReference()
    {
        allSkippables.Clear();
    }
}


public interface ISkippable
{
    public void TrySkip();
    public bool CanSkip { get; }
}