using MonoFSM.Foundation;
using RCGExtension;
using UnityEngine;

namespace MonoFSM.Core
{
    /// <summary>
    /// <see cref="MonoShortCutInspector"/>
    /// </summary>
    public class MonoShortCut : AbstractDescriptionBehaviour, IEditorOnly, IHierarchyButton
    {
        [SerializeField] public GameObject targetGameObject;

        protected override string DescriptionTag => "=> ShortCut";
        public override string Description => targetGameObject.name;
        public bool IsDrawButton => true;
        public string IconName => "d_";

        public void OnClick()
        {
            //哇XD 還是沒辦法 還是乾脆把internal bridge也放進來...好痛苦
        }
    }
}