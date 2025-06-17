using System.Linq;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace MonoFSM.Core
{
    public static class PrefabKindExt
    {
#if UNITY_EDITOR
        public static readonly PrefabKind InAnyPrefabAsset = PrefabKind.PrefabAsset | PrefabKind.InstanceInPrefab;
#endif
    }
    public static class PrefabExtension
    {
#if UNITY_EDITOR

        //給validate用的，想要不同情況下做不同validate
        public static bool IsInPrefab(this GameObject gObj)
        {
            return OdinPrefabUtility.GetPrefabKind(gObj) is PrefabKind.Variant or PrefabKind.Regular
                or PrefabKind.InstanceInPrefab;
        }

        public static bool IsPrefabAssetFile(this GameObject gameObject)
        {
            return gameObject.IsInPrefab() && PrefabStageUtility.GetCurrentPrefabStage() == null;
        }

        public static bool IsInPrefabVariant(this GameObject gObj)
        {
            return OdinPrefabUtility.GetPrefabKind(gObj) == PrefabKind.Variant;
        }

        public static bool IsInScene(this GameObject gObj) //想要在場景上才validate
        {
            return OdinPrefabUtility.GetPrefabKind(gObj) is PrefabKind.InstanceInScene or PrefabKind.NonPrefabInstance;
        }

        public static PrefabKind CurrentPrefabKind(this Object obj)
        {
            return OdinPrefabUtility.GetPrefabKind(obj);
        }

        public static bool IsPrefabKindMatchedWith(this Object obj, PrefabKind prefabKind)
        {
            return (obj.CurrentPrefabKind() & prefabKind) != 0;
        }
#endif


//TODO: 還沒測試
        public static T GenerateScriptableObjectInPrefabFolder<T>(this GameObject gObj) where T : ScriptableObject
        {
            //動畫對應是clip？
#if UNITY_EDITOR
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                // prefabStage.IsPartOfPrefabContents()
                var prefabPath = prefabStage.assetPath;
                var folderPath = prefabPath[..prefabPath.LastIndexOf('/')];
                var asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, folderPath + "/0_" + gObj.name + ".anim");
                Debug.Log("生成 SO" + asset, asset);
                AssetDatabase.SaveAssets();
                return asset;
            }
            // var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m);
#endif
            return null;
        }


        //TODO: 還沒測試，monsterState 類似code, 但不是用 stateName做
        public static AnimationClip GenerateAnimationClipInPrefabFolder(this GameObject gObj, string stateName)
        {
#if UNITY_EDITOR
            var animator = gObj.GetComponentInParent<IAnimatorProvider>();
            var anim = animator.ChildAnimator;
            var overrideController = anim.runtimeAnimatorController as AnimatorOverrideController;
            if (overrideController == null) return null;

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                var prefabPath = prefabStage.assetPath;
                var folderPath = prefabPath[..prefabPath.LastIndexOf('/')];
                var overrideClip = new AnimationClip();

                var baseController = overrideController.runtimeAnimatorController as AnimatorController;
                if (baseController == null)
                    return null;

                var baseClip = baseController.layers[0].stateMachine.states
                    .FirstOrDefault((state) => state.state.name == stateName).state.motion as AnimationClip;

                overrideController[baseClip] = overrideClip;
                AssetDatabase.CreateAsset(overrideClip, folderPath + "/0_" + gObj.name + ".anim");
                Debug.Log("生成 clip" + overrideClip, overrideClip);
                AssetDatabase.SaveAssets();
                return overrideClip;
            }
#endif
            return null;
        }
    }
}