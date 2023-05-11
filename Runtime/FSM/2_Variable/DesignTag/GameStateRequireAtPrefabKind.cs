using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    public class GameStateRequireAtPrefabKind : MonoBehaviour
    {
        [DisallowModificationsIn(PrefabKind.InstanceInScene)]
        public PrefabKind prefabKind = PrefabKind.InstanceInScene; //default以scene危單位在存

#if UNITY_EDITOR
        public bool IsPrefabKindMatch()
        {
            return (this.CurrentPrefabKind() & prefabKind) != 0;
        }
#endif
        private AbstractVariable variable; //好像也不用反向指
        //讓AbstractVariable可以來反查
        //MonoVariable
    }
}

//TODO: 想要在哪裡gen game state
//情境：
//1. 拿過了：InScene
//2. 主角的某個數值 InPrefab (也可以不用裝了？
//3. Config/Stat InPrefabVariant