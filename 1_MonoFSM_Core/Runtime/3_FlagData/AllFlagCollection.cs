using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

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

        [Button]
        public void FindAllFlagsInProject()
        {
            Flags.Clear();
            // gameFlagDataList.Clear();
            // Debug.Log("Find GameFlag:" + typeof(T).FullName);
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
    }
}