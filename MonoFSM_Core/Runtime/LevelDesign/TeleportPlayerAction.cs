using MonoFSM_Core.Runtime.Action;
using MonoFSM.Variable;
using RCGMaker.Runtime.Item_BuildSystem.MonoDescriptables;
using UnityEngine;

namespace MonoFSM_Core.Runtime.LevelDesign
{
    public class TeleportPlayerAction:AbstractStateAction,IArgEventReceiver<Vector3>
    {
        // public Vector3 teleportPosition;
        // public Transform playerTransform;
        // [DropDownRef]
        public VarTransform playerVar;
        protected override void OnStateEnterImplement()
        {
            
        }

        public void ArgEventReceived(Vector3 arg)
        {
            if(playerVar.Value == null)
            {
                Debug.LogError("playerVar is null",playerVar);
                return;
            }
            //FIXME: photon?
            playerVar.Value.position = arg;
            //KCC根本沒辦法這樣移動...?
            Debug.Log("Teleport to " + arg,playerVar.Value);
            //network? singleton...
        }
    }
}