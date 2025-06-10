using System.Linq;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Runtime.Interact.EffectHit
{
    [CreateAssetMenu(fileName = "GeneralEffectType", menuName = "RCGMaker/GeneralEffectType", order = 0)]
    public class GeneralEffectType : ScriptableObject, IEffectType
    {
#if UNITY_EDITOR
        [TextArea] [SerializeField] private string _note; 
        [PreviewInInspector] GeneralEffectDealer[] _bindedDealers;
        [PreviewInInspector] GeneralEffectReceiver[] _bindedReceivers;

        [Button]
        private void GetBindingVariables()
        {
            _bindedDealers =
                FindObjectsByType<GeneralEffectDealer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(v => v.EffectType == this).ToArray();
            _bindedReceivers =
                FindObjectsByType<GeneralEffectReceiver>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(v => v.EffectType == this).ToArray();
        }
#endif
    }
}