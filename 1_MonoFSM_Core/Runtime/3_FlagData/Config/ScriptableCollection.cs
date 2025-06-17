using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Core
{
    public abstract class ScriptableCollection<T> : ScriptableObject where T : ScriptableObject
    {
        protected virtual bool FlagBelongThisCollection(T t) //用一些條件去篩掉特定flag, 例如要做百科分類
        {
            return true;
        }
#if UNITY_EDITOR
        [Button("Clear")]
        public void Clear()
        {
            collection.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#if UNITY_EDITOR
        [Button("Find Under Folder With OverrideTypeName")]
        public void FindUnderFolder()
        {
            collection = ScriptableHelper.FindAllSO<T>(this);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        [TextArea] public string note;
        [Button("FindAllFlags")]
        public void FindAllFlags()
        {
            collection.Clear();
            var myPath = AssetDatabase.GetAssetPath(this);
            var dirPath = Path.GetDirectoryName(myPath);
            var scriptables = AssetDatabase.FindAssets("t:" + typeof(T).FullName, new[] { dirPath });

            foreach (var t in scriptables)
            {
                var path = AssetDatabase.GUIDToAssetPath(t);
                var flag = AssetDatabase.LoadAssetAtPath<T>(path);

                //  自動生成pathName
                // var pathName = path.Substring(16, path.Length - 16);
                // if (flag.flagpath != pathName)
                // {
                //     flag.flagpath = pathName;
                //     EditorUtility.SetDirty(flag);
                // }\
                if (FlagBelongThisCollection(flag))
                    collection.Add(flag);
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
        [FormerlySerializedAs("data")] [FormerlySerializedAs("gameFlagDataList")] [InlineEditor()] [SerializeField]
        public List<T> collection = new();
    }
}