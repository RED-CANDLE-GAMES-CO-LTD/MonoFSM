using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGInputAction
{
    //把 DeviceIconMapConfig 注入 InputPromptUIData 的靜態 finder，場景裡放一顆即可
    public class HintSpriteFinderInstaller : MonoBehaviour, ISceneAwake
    {
        [Required]
        [SerializeField]
        private DeviceIconMapConfig _iconMap;

        public void EnterSceneAwake()
        {
            InputPromptUIData.SetSpriteFinder(_iconMap);
        }
    }
}
