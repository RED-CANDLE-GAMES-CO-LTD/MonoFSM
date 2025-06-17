using System;
using MonoFSM.Core;
using UnityEditor;
using UnityEngine;


namespace MonoFSM.Editor
{
    [InitializeOnLoad]
    public class ExtendedHotkeys : ScriptableObject
    {
        // static EditStateAnimation() //監聽進出Play/Edit
        // {
        //     //        Debug.Log("ExtendHotKeys init");
        //    
        // }
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= LogPlayModeState;
        }

        private static IAnimatorPlayAction _lastEditState;
        [MenuItem("RCGs/ShortCut/Find Animator #_A")]
        public static void FindAnimator()
        {
            if (Application.isPlaying) return;
            if (Selection.activeGameObject == null) return;
            var owner = Selection.activeGameObject.GetComponentInParent<StateMachineOwner>();
            if(owner == null) return;
            var anim = owner.GetComponentInChildren<Animator>();
            Selection.activeGameObject = anim.gameObject;
        }

        [MenuItem("RCGs/ShortCut/Edit Animation of State  %_E")]
        // [Shortcut("NOT_Lonely/Edit Animation of Monster", typeof(SceneView), KeyCode.E, ShortcutModifiers.Shift)]
        private static void DoEditAnimation()
        {
            // Debug.Log("[ShortCut] Edit Animation of State");
            if (Application.isPlaying) return;
            if (Selection.activeGameObject == null) return;

            var iAnimatorPlayAction = Selection.activeGameObject.GetComponentInChildren<IAnimatorPlayAction>();
            if (iAnimatorPlayAction != null)
            {
                Debug.Log("[ShortCut] Edit anim of state" + iAnimatorPlayAction);
                _lastEditState = iAnimatorPlayAction;
                AnimatorHelper.EditClip(_lastEditState.BindAnimator, _lastEditState.Clip);
            }
            else
            {
                var animator = Selection.activeGameObject.GetComponent<Animator>();
                if (animator != null)
                {
                    Debug.Log("[ShortCut] Edit anim of animator" + animator);
                    //open animator window
                    AssetDatabase.OpenAsset(animator.runtimeAnimatorController);
                    AnimationClipSearchWindow.ShowCustomWindow();
                    
                }
            }
        }
        
        private static void LogPlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                var lastEditStateID = EditorPrefs.GetInt("lastEditState");

                var lastEditStateGO = UnityEditor.EditorUtility.InstanceIDToObject(lastEditStateID) as GameObject;
                if (lastEditStateGO == null)
                {
                    Debug.Log("No lastEditStateGO");
                    return;
                }

                _lastEditState = lastEditStateGO.GetComponent<IAnimatorPlayAction>();
                if (_lastEditState != null)
                    _lastEditState.EditClip();
                else
                    Debug.Log("No last Edit State");
            }
        }
    }
}