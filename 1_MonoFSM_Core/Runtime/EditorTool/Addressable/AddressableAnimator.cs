using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AddressableAssets;
#endif

using MonoFSM.Core.Attributes;

//TODO:
public class AddressableAnimator : MonoBehaviour
{
    [PreviewInInspector] [SerializeField] private Animator _animator;
    public AssetReference AnimatorControllerReference;
#if UNITY_EDITOR
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
#endif

    private void Load() //Camera Culling到的時候才Load
    {
        _animator.runtimeAnimatorController = AnimatorControllerReference.Asset as RuntimeAnimatorController;
    }
}