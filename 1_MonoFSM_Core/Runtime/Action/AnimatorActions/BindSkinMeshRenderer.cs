using UnityEngine;

namespace MonoFSM.Runtime.Variable.Action.AnimatorActions
{
    public class ReplaceBones : MonoBehaviour
    {
        [Header("Source Bones")]
        public Transform rootBone; // Drag the root bone of the source prefab here
        Transform[] sourceBones; // Optionally, drag all source bones here

        [Header("Source Prefab")]
        public GameObject sourcePrefab; // Optional for auto-fetching bones
        public GameObject targetPrefab;

        void Start()
        {
            sourceBones = rootBone.GetComponentsInChildren<Transform>();
            if (rootBone == null || sourceBones.Length == 0)
            {
                Debug.LogError("Assign the rootBone and sourceBones in the Inspector.");
                return;
            }

            ReplaceBonesInTarget();
        }

        void ReplaceBonesInTarget()
        {
            SkinnedMeshRenderer[] targetSMRs = targetPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (SkinnedMeshRenderer targetSMR in targetSMRs)
            {
                // Replace root bone
                targetSMR.rootBone = FindMatchingBone(targetSMR.rootBone);

                // Replace bones
                Transform[] targetBones = new Transform[targetSMR.bones.Length];
                for (int i = 0; i < targetSMR.bones.Length; i++)
                {
                    targetBones[i] = FindMatchingBone(targetSMR.bones[i]);
                }

                targetSMR.bones = targetBones;
            }

            Debug.Log("Bone replacement completed!");
        }

        Transform FindMatchingBone(Transform targetBone)
        {
            foreach (Transform sourceBone in sourceBones)
            {
                if (sourceBone.name == targetBone.name)
                {
                    return sourceBone;
                }
            }

            Debug.LogWarning($"Bone '{targetBone.name}' not found in source bones.");
            return null;
        }
    }
}