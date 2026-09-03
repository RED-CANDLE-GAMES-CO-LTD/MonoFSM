using System.Reflection;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Core.Editor
{
    /// <summary>
    ///     LayerMask 一律交給 Unity 原生 PropertyField 畫。
    ///     Odin 內建的 LayerMaskDrawer 不會畫出 prefab override 的粗體 label（它只靠自己那條可關閉的藍色 bar），
    ///     造成「改了 layer mask 但看不出是 override」。走 Unity 的 PropertyField 才會有粗體 + 右鍵 Revert/Apply。
    ///     拿不到 SerializedProperty（Odin 自行序列化、collection element 等）時 fallback 回原本的 drawer。
    /// </summary>
    [UsedImplicitly]
    [DrawerPriority(0, 1000, 0)]
    public class LayerMaskUnityDrawer : OdinValueDrawer<LayerMask>
    {
        private bool _resolved;
        private SerializedProperty _unityProperty;

        protected override void Initialize()
        {
            //每次 tree 重建才解析一次，避免每幀查字串 path
            _unityProperty = Property.Tree.GetUnityPropertyForPath(
                Property.UnityPropertyPath, out FieldInfo _);
            _resolved = _unityProperty != null;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (!_resolved)
            {
                CallNextDrawer(label);
                return;
            }

            EditorGUILayout.PropertyField(_unityProperty, label, true);
        }
    }
}
