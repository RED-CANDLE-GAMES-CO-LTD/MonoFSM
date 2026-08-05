using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _1_MonoFSM_Core.Runtime._3_FlagData
{
    [CreateAssetMenu(fileName = "AllFlagCollection", menuName = "MonoFSM/Flag/AllFlagCollection")]
    public class AllFlagCollection : GameFlagCollection //繼承所有 GameFlagCollection 功能
    {
        private static AllFlagCollection _instance;

        /// <summary>
        /// Singleton 實例，使用 lazy initialization
        /// </summary>
        public static AllFlagCollection Instance =>
            _instance ??= LazyInstanceHelper.GetOrCreateInstance<AllFlagCollection>();

        /// <summary>
        /// 手動指派實例（用於特殊情況）
        /// </summary>
        public void ManuallyAssign()
        {
            _instance = this;
        }


#if UNITY_EDITOR
        public override void OnHeavySceneSaving()
        {
            FindAllFlagsInProject();
        }

        [Button]
        public void FindAllFlagsInProject() //FIXME: 用CustomSceneSave?
        {
            Flags.Clear();
            // gameFlagDataList.Clear();
            Debug.Log("FindAllFlagsInProject");
            var myPath = AssetDatabase.GetAssetPath(this);
            // Debug.Log("Mypath" + name + ":" + myPath);
            // var dirPath = System.IO.Path.GetDirectoryName(myPath);
            var allProjectFlags = AssetDatabase.FindAssets("t:GameFlagBase");
            //All 10_Flags
            // string[] allProjectFlags = AssetDatabase.FindAssets("t:GameFlagBase", new[] { "Assets/10_Flags" });
            for (var i = 0; i < allProjectFlags.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(allProjectFlags[i]);
                var flag = AssetDatabase.LoadAssetAtPath<GameFlagBase>(path);
                Flags.Add(flag);
            }

            EditorUtility.SetDirty(this);
        }

        public void AddFlag(GameFlagBase flag)
        {
            if (flag == null || Flags.Contains(flag)) return;
            Flags.Add(flag);
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
