using System;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static MonoFSMEditor.RefectionUtility;

namespace MonoFSM.Editor.AnimationWindow
{
    /// <summary>
    /// test
    /// </summary>
    public static class AnimationWindowSearchBar
    {
        private static IAnimatorPlayAction _lastEditState;

        [MenuItem("MonoFSM/Edit Animation of State %_E")]
        static void OpenAnimationWindow()
        {
            EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
            if (Application.isPlaying)
                return;
            if (Selection.activeGameObject == null)
                return;

            var iAnimatorPlayAction =
                Selection.activeGameObject.GetComponentInChildren<IAnimatorPlayAction>();
            if (iAnimatorPlayAction != null)
            {
                // Debug.Log("[ShortCut] Edit anim of state" + iAnimatorPlayAction);
                _lastEditState = iAnimatorPlayAction;
                iAnimatorPlayAction.EditClip();
                // Debug.Log(" Edit anim of stateDone");
            }
            else
            {
                //去找animator play action來播？好像也沒什麼必要
                var selection = Selection.activeGameObject;
                if (selection != null)
                {
                    var animator = selection.GetComponentInChildren<Animator>();
                    if (animator != null)
                        Selection.activeGameObject = animator.gameObject;
                }
            }
        }

        private static Dictionary<EditorWindow, AnimationWindowNavbar> navbars_byWindow = new();
        private static Type t_AnimationWindow;
        private static Type t_HostView;
        private static Type t_EditorWindowDelegate;
        private static MethodInfo mi_WrappedGUI;

        static AnimationWindowSearchBar()
        {
            t_AnimationWindow = typeof(UnityEditor.Editor).Assembly.GetType(
                "UnityEditor.AnimationWindow"
            );
            t_HostView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.HostView");
            t_EditorWindowDelegate = t_HostView.GetNestedType(
                "EditorWindowDelegate",
                maxBindingFlags
            );
            mi_WrappedGUI = typeof(AnimationWindowSearchBar).GetMethod(
                nameof(WrappedGUI),
                maxBindingFlags
            );
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.update -= CheckForAnimationWindows;
            EditorApplication.update += CheckForAnimationWindows;
        }

        private static void CheckForAnimationWindows()
        {
            var animationWindows = UnityEditor.AnimationWindow.GetAllAnimationWindows();
            // var animationWindows = Resources
            //     .FindObjectsOfTypeAll(t_AnimationWindow)
            //     .Cast<EditorWindow>();

            foreach (var window in animationWindows)
            {
                if (window != null && window.hasFocus)
                {
                    UpdateGUIWrapping(window);
                }
            }
        }

        // private static void DelayCallLoop()
        // {
        //     var animationWindows = UnityEditor.AnimationWindow.GetAllAnimationWindows();
        //
        //     foreach (var window in animationWindows)
        //     {
        //         if (window != null)
        //         {
        //             UpdateGUIWrapping(window);
        //         }
        //     }
        //
        //     EditorApplication.delayCall -= DelayCallLoop;
        //     EditorApplication.delayCall += DelayCallLoop;
        // }

        private static void WrappedGUI(EditorWindow window)
        {
            var navbarHeight = 26;

            void navbarGui()
            {
                if (!navbars_byWindow.ContainsKey(window))
                    navbars_byWindow[window] = new AnimationWindowNavbar(window);

                var navbarRect = window.position.SetPos(0, 0).SetHeight(navbarHeight);
                navbars_byWindow[window].OnGUI(navbarRect);
            }

            void defaultGuiWithOffset()
            {
                // 在調用原始OnGUI之前，先處理navbar的dropdown事件
                if (navbars_byWindow.ContainsKey(window))
                {
                    var navbar = navbars_byWindow[window];
                    navbar.HandleDropdownEventsFirst();
                }

                var m_Pos_original = window.GetFieldValue<Rect>("m_Pos");

                // 用 GUI.matrix 平移內容往下，不會產生 clipping（footer 不會被擠掉）
                var originalMatrix = GUI.matrix;
                GUI.matrix = Matrix4x4.TRS(
                    new Vector3(0, navbarHeight, 0),
                    Quaternion.identity,
                    Vector3.one
                ) * originalMatrix;

                // 告訴 Animation Window 可用高度少了 navbarHeight，讓它 layout 在縮小的空間內
                window.SetFieldValue("m_Pos", m_Pos_original.AddHeightFromBottom(-navbarHeight));

                try
                {
                    window.InvokeMethod("OnGUI");
                }
                catch (Exception exception)
                {
                    if (exception.InnerException is ExitGUIException)
                        throw exception.InnerException;
                    else
                        throw exception;
                }

                window.SetFieldValue("m_Pos", m_Pos_original);
                GUI.matrix = originalMatrix;
            }

            // 設定 Dockarea 的 bottom style，讓 IMGUI 區域縮短以容納 navbar（避免 footer 被擠掉）
            var dockarea =
                window.rootVisualElement; //?.parent?.Q(className: "unity-imgui-container");
            if (dockarea != null)
                dockarea.style.bottom = navbarHeight;

            // 先渲染原始 GUI（往下平移），再把 navbar 畫在最上面
            defaultGuiWithOffset();
            navbarGui();
        }

        private static void UpdateGUIWrapping(EditorWindow window)
        {
            if (!window || !window.hasFocus)
                return;
            if (window.GetType() != t_AnimationWindow)
                return;
            // 清除原有的OnGUI方法，避免重複調用
            var curOnGUIMethod = window
                .GetMemberValue("m_Parent")
                ?.GetMemberValue<Delegate>("m_OnGUI")
                ?.Method;
            if (curOnGUIMethod == null)
                return;

            var isWrapped = curOnGUIMethod == mi_WrappedGUI;
            var shouldBeWrapped = true; // 總是啟用search bar

            void wrap()
            {
                var hostView = window.GetMemberValue("m_Parent");
                if (hostView == null)
                    return;

                var newDelegate = mi_WrappedGUI.CreateDelegate(t_EditorWindowDelegate, window);
                hostView.SetMemberValue("m_OnGUI", newDelegate);
                window.Repaint();
            }

            void unwrap()
            {
                var hostView = window.GetMemberValue("m_Parent");
                if (hostView == null)
                    return;

                var originalDelegate = hostView.InvokeMethod("CreateDelegate", "OnGUI");
                hostView.SetMemberValue("m_OnGUI", originalDelegate);

                // 還原 Dockarea 的 bottom style
                var dockarea =
                    window.rootVisualElement; //?.parent?.Q(className: "unity-imgui-container");
                if (dockarea != null)
                    dockarea.style.bottom = 3;

                window.Repaint();
            }

            if (shouldBeWrapped && !isWrapped)
                wrap();

            if (!shouldBeWrapped && isWrapped)
                unwrap();
        }
    }
}
