using System.Linq;
using UnityEngine;

namespace MonoFSM.Core
{
    public static class EffectHitExtension
    {
        public static T[] ConvertTo<T>(this IEffectDealer[] dealers) where T : MonoBehaviour
        {
            return dealers.Select(dealer => dealer as T).ToArray();
        }

        //這算是實作嗎....共用的實作，好像很爽會有問題嗎XDDD
        //可能沒什麼用？
        // public static T[] GetDealers<T>(this IEffectDealerProvider dealerProvider) where T : MonoBehaviour
        // {
        //     return dealerProvider.Dealers.ConvertTo<T>();
        // }
    }
}