using System.Collections;
using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;

namespace MonoFSM.Editor.DesignTool
{

    interface IGizmoColorProvider
    {
        Color GizmoColor { get; }
    }
    
    [RequireComponent(typeof(SpriteRenderer))]
    public class AreaMarker : MonoBehaviour, IEditorOnly
    {

        public int fontSize = 16;

        private SpriteRenderer _spr;

        //TODO: align Title at? 還是其實不用有color coding, 還可以加上icon輔助？ 或是 想要顯示的特別抓出來
        private const float size = 256 / 8 / 2;
#if UNITY_EDITOR
        SpriteRenderer Spr
        {
            get
            {
                if (_spr == null)
                    _spr = GetComponent<SpriteRenderer>();
                return _spr;
            }
        }

        void AssignPureWhite() //直接拿purewhite
        {

            if (Spr.sprite != null)
                return;

            Sprite t = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/2_Art/TestBed/HoHoRef/pure_white.png",
                typeof(Sprite));
            Spr.sprite = t;
            var color = Color.yellow;
            color.a = 0.5f;
            Spr.color = color;
        }

        void OnValidate()
        {
            // base.OnValidate();
            AssignPureWhite();
        }

        public void OnDrawGizmos()
        {

            if (GamePlayDesignPref.IsEnabled == false)
            {
                Spr.enabled = false;
                return;
            }

            Spr.enabled = true;

            var colorProvider = GetComponent<IGizmoColorProvider>();
            if (colorProvider != null)
            {
                var color = colorProvider.GizmoColor;
                color.a = 0.25f;
                Spr.color = color;
            }
            // Spr.color = ColorFromGameType;
            // base.OnDrawGizmos();

            DrawLabel(name.ToString(), Vector3.down * (fontSize + 4));

        }

        public void DrawLabel(string text, Vector3 offset = default(Vector3))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = GizmoColor;
            style.fontSize = fontSize;
            var x = (transform.localScale.x - 1) * size;
            var y = (transform.localScale.y - 1) * size;
            UnityEditor.Handles.Label(transform.position + offset + new Vector3(-x, y, 0), text, style);
        }
#endif


        protected virtual Color GizmoColor
        {
            get
            {
                // if (IsCustomColor)
                //     return customColor;
                return Color.white;
            }
        }

    }
}