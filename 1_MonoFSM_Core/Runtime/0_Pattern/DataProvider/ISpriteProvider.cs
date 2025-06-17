using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MonoFSM.AddressableAssets;
using UIValueBinder;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.DataProvider
{
    public interface ISpriteProvider
    {
        UniTask<Sprite> GetSprite();
    }

    //不能還是蠻討厭的...
//     [Serializable]
//     public class SpriteProviderFromDescriptableData : AbstractDescriptablePropertyProvider, ISpriteProvider
//     {
//         
//         protected override List<Type> supportedTypes => new() { typeof(Sprite), typeof(RCGAssetReference) };
//         public async UniTask<Sprite> GetSprite()
//         {
//             var descriptableData = _dataProvider.GetDescriptableData();
//             if (descriptableData == null)
//             {
//                 // Debug.LogError("descriptableData == null");
//                 return null;
//             }
//             var data = descriptableData.GetProperty(_propertyName);
//             switch (data)
//             {
// #if UNITY_EDITOR
//                 case RCGAssetReference assetReference when Application.isPlaying == false:
//                     return assetReference.editorAsset as Sprite;
// #endif
//                 case RCGAssetReference { IsAssetLoaded: true } assetReference:
//                     var sprite = assetReference.GetAsset<Sprite>();
//                     return sprite;
//                 case RCGAssetReference assetReference:
//                     return await assetReference.GetAssetAsync<Sprite>();
//                 case Sprite sp:
//                     return sp;
//             }
//             return null;
//         }
//
//         
//     }
}


//         case null:
//         // Debug.Log("UIImageValueBinder fieldValue == null:" + this, this);
//         spr.sprite = null;
//         return;
//         // Debug.Log("UIImageValueBinder fieldValue AssetReferenceSprite:" + this, this);
//         //preview用
// #if UNITY_EDITOR
//         case RCGAssetReference assetReference when Application.isPlaying == false:
//         // Debug.Log("UIImageValueBinder fieldValue Editor Preview" + this, this);
//         spr.sprite = assetReference.editorAsset as Sprite;
//         return;
// #endif
//         // Debug.Log("UIImageValueBinder fieldValue AssetReferenceSprite" + this, this);
//         //FIXME: 怎麼不做null check? 這個好像還好
//         case RCGAssetReference assetReference when assetReference.IsAssetLoaded:
//         {
//             var sprite = assetReference.GetAsset<Sprite>();
//             // Debug.Log("UIImageValueBinder fieldValue AssetReferenceSprite is not null" + this, this);
//             // Debug.Log("UIImageValueBinder fieldValue AssetReferenceSprite is Done" + this, this);
//             spr.sprite = sprite;
//             break;
//         }
//         case RCGAssetReference assetReference:
//         {
//             var sprite = await assetReference.GetAssetAsync<Sprite>();
//             // spr.sprite = null;
//             // var sprite = await LoadSprite(data, assetReference.assetReference);
//             spr.sprite = sprite;
//             break;
//         }
        // public ValueDropdownList<string> GetSpritePropertyNames()
        // {
        //     return _descriptableDataProvider.GetDescriptableData().GetProperties<string>();
        // }
        // [ValueDropdown("GetSpritePropertyNames")]
        // public string _propertyName;
    