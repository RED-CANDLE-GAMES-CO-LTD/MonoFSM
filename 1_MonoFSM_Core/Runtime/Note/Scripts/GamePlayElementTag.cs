using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using TMPro;
using Sirenix.OdinInspector;

//標記物件：
namespace MonoFSM.Editor.DesignTool
{
    public class GamePlayElementTag : AbstractMapTag
    {
        //要分類？
        [PropertyOrder(-1)] [BoxGroup("GamePlayElement")]
        public ObjectTagSO objectType;

        public bool IsCustomTitle = true;
        public bool syncGObjName = false;

#if UNITY_EDITOR
        [Button("重整")]
        protected override void OnValidate()
        {
            // gameObject.name = title;
            // if (objectType && title == "")
            //     title = objectType.name;
            var fullTitle = title;
            if (objectType)
            {
                fullTitle = objectType.prefix + title;
                if (iconSpr)
                {
                    iconSpr.sprite = objectType.icon;
                    iconSpr.color = objectType.tintColor;
                }
            }


            if (syncGObjName)
                gameObject.name = fullTitle;

//FIXME: TMP??
            // if (text) text.text = fullTitle;
        }
#endif
        // public TextMeshPro text;
        public SpriteRenderer iconSpr;
        public float iconScale;
    }
}
