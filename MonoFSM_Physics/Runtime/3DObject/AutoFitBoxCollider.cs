using MonoFSM.Core;
using Sirenix.OdinInspector;

namespace MonoFSM.Core.Runtime.LevelDesign._3DObject
{
    using UnityEngine;

    //#if Editor?
    [RequireComponent(typeof(BoxCollider))] // 確保物件上有 BoxCollider
    public class AutoFitBoxCollider : MonoBehaviour,IBeforePrefabSaveCallbackReceiver
    {
        private BoxCollider boxCollider;
        [Button]
        // 你也可以呼叫這個方法來手動觸發調整
        [ContextMenu("Adjust Box Collider to Children Bounds")]
        public void AdjustColliderToBounds()
        {
            // 獲取或新增 BoxCollider 元件
            boxCollider = GetComponent<BoxCollider>();
            
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                Debug.LogWarning("No Renderers found on " + gameObject.name +
                                 " or its children. Cannot adjust BoxCollider.");
                // 如果沒有 Renderer，可以考慮禁用 Collider 或給一個預設大小
                boxCollider.size = Vector3.one;
                boxCollider.center = Vector3.zero;
                return;
            }

            // 初始化總體邊界
            Bounds combinedBounds = new Bounds(renderers[0].bounds.center, renderers[0].bounds.size);

            // 遍歷所有 Renderer (包括父物件自身和子物件)
            for (int i = 1; i < renderers.Length; i++)
            {
                // 確保 Renderer 是啟用的，否則其 bounds 可能無效
                if (renderers[i].enabled)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
            }

            // 將世界空間的總體邊界轉換到父物件的本地空間
            // BoxCollider 的 center 和 size 是在本地空間中定義的
            Vector3 localCenter = transform.InverseTransformPoint(combinedBounds.center);
            Vector3 localSize = combinedBounds.size; // 注意：這在父物件沒有旋轉和非均勻縮放時是準確的

            // 如果父物件有非均勻縮放或旋轉，直接使用 combinedBounds.size 可能不準確
            // 一個更精確的方法是將世界空間的 8 個角落轉換到本地空間並計算新的本地邊界
            // 但對於大多數常見情況，直接使用 combinedBounds.size 是足夠的

            // 設定 BoxCollider 的中心和大小
            boxCollider.center = localCenter;
            boxCollider.size = localSize;

            Debug.Log("BoxCollider on " + gameObject.name + " adjusted to cover children bounds.");
        }

        public void OnBeforePrefabSave()
        {
            // 在儲存 Prefab 前自動調整 BoxCollider
            AdjustColliderToBounds();
        }
    }
}