using UnityEngine;

//DElayNode?
//TODO: 什麼時候需要這個？
public class DelayActionModifier : MonoBehaviour
{
    public float delayTime = 1;

    [Component(typeof(AbstractStateAction), AddComponentAt.Children, "[Action]")]
    AbstractStateAction[] actions;
    // private void AddAction()
    // {
    // }
}