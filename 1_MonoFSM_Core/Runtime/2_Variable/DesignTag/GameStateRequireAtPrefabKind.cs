using UnityEngine;

using MonoFSM.Variable;
using Sirenix.OdinInspector;


namespace MonoFSM.Core
{
    public class GameStateRequireAtPrefabKind : MonoBehaviour, IEditorOnly
    {
        [DisallowModificationsIn(PrefabKind.InstanceInScene)]
        public PrefabKind prefabKind = PrefabKind.InstanceInScene; //default以scene危單位在存

#if UNITY_EDITOR
        public bool IsPrefabKindMatch => this.IsPrefabKindMatchedWith(prefabKind); //TODO: 這段最好拿去drawer就好
#endif

        private AbstractMonoVariable _monoVariable; //好像也不用反向指
        //讓AbstractVariable可以來反查
        //MonoVariable
    }
}

//TODO: 想要在哪裡gen game state
//情境：
//1. 拿過了：InScene
//2. 主角的某個數值 InPrefab (也可以不用裝了？
//3. Config/Stat InPrefabVariant