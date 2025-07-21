using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MonoFSMCore.Runtime.LifeCycle;
using MonoFSM.Core.Attributes;

public interface IActiveOverrider
{
    void Show();
    void Hide();
}


//default開著，如果有人投票關掉，就關掉
public static class GameObjectActivateExtension
{
    public static bool SetActiveVote(this MonoBehaviour go, bool active)
    {
        //有人投票關掉，就關掉
        if (go.TryGetComponent<GameObjectActivateVoter>(out var voterComp)) return voterComp.SetActiveVote(active, go);

        //沒有投票的話，就直接用GameObject的開關
        go.gameObject.SetActive(active); //fallback 用Unity的開關法
        return active;
    }

    public static bool SetActiveVote(this GameObject go, MonoBehaviour owner, bool active)
    {
        //Debug.LogError("Vote1"+go+":"+owner+":"+active);
        if (go.TryGetComponent<GameObjectActivateVoter>(out var voterComp))
            return voterComp.SetActiveVote(active, owner);

        //Debug.LogError("Vote2"+go+":"+owner+":"+active);
        go.gameObject.SetActive(active); //fallback 用Unity的開關法
        return active;
        //  if (voterComp == null)
        //  {
        //      //Debug.LogError("Vote2"+go+":"+owner+":"+active);
        //      go.gameObject.SetActive(active); //fallback 用Unity的開關法
        //      return active;
        //  }
        // // Debug.LogError("Vote3"+go+":"+owner+":"+active);
        //  return voterComp.SetActiveVote(active, owner);
    }
}

public class GameObjectActivateVoter : MonoBehaviour, IResetStateRestore
{
    //Default是開自己節點，如果想要改成開其他節點，可以用這個override(Monster的話就是開MonsterCore的節點)
    [Auto(false)] private IActiveOverrider overrider;


    public enum ActivateScheme
    {
        AND,
        OR
    }

    public ActivateScheme activateScheme = ActivateScheme.AND;

    // public Action ShowOverride;
    // public Action HideOverride;
    public Dictionary<Component, bool> dict = new();

    [PreviewInInspector] public List<Component> voters => new(dict.Keys);

    public bool SetActiveVote(bool active, Component voter)
    {
        if (active)
            return VoteShow(voter);
        else
            return VoteHide(voter);
    }

    public bool VoteShow(Component voter)
    {
        return SetActive(true, voter);
    }

    public bool VoteHide(Component voter)
    {
        return SetActive(false, voter);
    }

    public bool SetActive(bool active, Component comp)
    {
        if (comp == null)
        {
            Debug.LogError("Null Activate key");
            return false;
        }

        if (!dict.TryAdd(comp, active)) dict[comp] = active;

        _voteResult = ActiveCheck();
        // Debug.Log("");
        return _voteResult;
    }

    private bool ActiveCheck()
    {
        if (activateScheme == ActivateScheme.AND)
        {
            foreach (var key in dict.Keys)
                if (dict[key] == false)
                {
                    this.Log("[active overrider] Hide", key, gameObject);
                    if (overrider != null)
                        overrider?.Hide();
                    else
                        gameObject.SetActive(false);
                    return false;
                }

            if (overrider != null)
            {
                this.Log("[active overrider] Show", gameObject);
                overrider?.Show();
            }
            else
            {
                gameObject.SetActive(true);
            }


            return true;
        }
        else if (activateScheme == ActivateScheme.OR)
        {
            foreach (var key in dict.Keys)
                if (dict[key] == true)
                {
                    if (overrider != null)
                    {
                        this.Log("[active overrider] Show", gameObject);
                        overrider?.Show();
                    }
                    else
                    {
                        gameObject.SetActive(true);
                    }

                    return true;
                }


            if (overrider != null)
                overrider?.Hide();
            else
                gameObject.SetActive(false);

            return false;
        }

        Debug.LogError("Wierd case!");
        return true;
    }

    private bool _voteResult = true;

    [PreviewInInspector] public bool VoteResult => _voteResult;

    public void ResetStateRestore()
    {
        // dict.Clear();
        // _voteResult = true;
        // ActiveCheck();
    }
}