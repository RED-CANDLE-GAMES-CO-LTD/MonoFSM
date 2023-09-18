using System;
using RCGMaker.Core.Attributes;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

public class AddressableAnimator : MonoBehaviour
{
    [PreviewInInspector] [SerializeField] private Animator _animator;
    public AssetReference AnimatorControllerReference;

    private void OnValidate()
    {
        //Create Addressable for AnimatorController
        //要有個setting group

        var animatorController = GetComponent<Animator>().runtimeAnimatorController as AnimatorController;
        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(animatorController));
        AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid,
            AddressableAssetSettingsDefaultObject.Settings.DefaultGroup);
        AnimatorControllerReference = new AssetReference(guid); //這樣就可以嗎？
    }

    private void Load() //Camera Culling到的時候才Load
    {
        _animator.runtimeAnimatorController = AnimatorControllerReference.Asset as AnimatorController;
    }
}