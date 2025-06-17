using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using Sirenix.Serialization;
//統計用，買Asset畫圖表？

namespace MonoFSM.Editor.DesignTool
{
    public class GamePlayDesignToolManager : SerializedMonoBehaviour
    {
    
        // public List<GamePlayerDesignPoint> points;
        public Dictionary<GamePlayTypeSO, int> tagCountDict;
    
        [Button("Calculate")]
        void Calculate()
        {
            tagCountDict = new Dictionary<GamePlayTypeSO, int>();
            var points = GetComponentsInChildren<GamePlayTag>();
            // var tagCount = 0;
            foreach (var point in points)
            {
                if (!tagCountDict.ContainsKey(point.tagType))
                {
                    tagCountDict.Add(point.tagType, 1);
                }
                else
                    tagCountDict[point.tagType]++;
            }
            // Debug.Log("Find Tag:" + targetTag.name + ", count:" + tagCount);
        }
    }
    
    
    public static class GamePlayDesignPref
    {
        private const string MenuName = "RCGs/設計規劃/顯示標籤";
        private const string SettingName = "GamePlayTagDisplay";
        private const int priority = 100;
    
    #if UNITY_EDITOR
        public static bool IsEnabled
        {
            //TODO: OnDrawGizmo
            get { return EditorPrefs.GetBool(SettingName, true); }
            set { EditorPrefs.SetBool(SettingName, value); }
        }
    
        [MenuItem(MenuName, false, priority)]
        private static void ToggleAction()
        {
            IsEnabled = !IsEnabled;
        }
    
        [MenuItem(MenuName, true, priority)]
        private static bool ToggleActionValidate()
        {
            Menu.SetChecked(MenuName, IsEnabled);
            return true;
        }
    #endif
    }

}
