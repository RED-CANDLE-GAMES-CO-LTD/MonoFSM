using MonoFSM.Core.Runtime.Action;
using MonoFSM.Core.Attributes;
using UnityEngine;

//DElayNode?
//FIXME: 很危險，可能因為切state delay還沒結束結果沒有觸發
public class DelayActionModifier : MonoBehaviour
{
    public float delayTime = 1;

    [Component(AddComponentAt.Children, "[Action]")] [PreviewInInspector] [AutoChildren]
    private AbstractStateAction[] actions;
    // private void AddAction()
    // {
    // }
}