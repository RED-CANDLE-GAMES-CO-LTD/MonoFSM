using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.LevelDesign._3DObject
{
    /// <summary>
    /// 自動調整 Collider 以包覆指定的 Renderer
    /// 支援 AABB、OBB（旋轉優化的 BoxCollider）和 Convex MeshCollider
    /// </summary>
    public class AutoFitBoxCollider
        : MonoBehaviour,
            IBeforePrefabSaveCallbackReceiver,
            ICustomHeavySceneSavingCallbackReceiver
    {
        public enum ColliderFitMode
        {
            [Tooltip("軸對齊包圍盒，最快但空間浪費最多")] AABB,

            [Tooltip("方向包圍盒，會創建子物件來放置旋轉後的 BoxCollider")]
            OBB,

            [Tooltip("凸包 MeshCollider，最緊密但運算成本較高")]
            ConvexMesh,
        }

        public enum RendererSource
        {
            [Tooltip("只使用指定的單一 Renderer")] SingleRenderer,

            [Tooltip("只抓取子物件的 Renderer")] ChildrenOnly,

            [Tooltip("包含自己和子物件的 Renderer")] SelfAndChildren,

            [Tooltip("包含父物件、自己和子物件的 Renderer")] All,
        }

        [Title("基本設定")] public bool fitOnSceneSave = true;

        [Tooltip("Collider 中心的本地 Y 軸偏移")] public float _offsetY;

        [Title("Renderer 來源")]
        public RendererSource rendererSource = RendererSource.SelfAndChildren;

        [ShowIf("rendererSource", RendererSource.SingleRenderer)] [Tooltip("指定要包覆的單一 Renderer")]
        public Renderer targetRenderer;

        [Title("Collider 模式")] public ColliderFitMode fitMode = ColliderFitMode.AABB;

        [ShowIf("fitMode", ColliderFitMode.OBB)]
        [Tooltip("粗掃階段每軸（0~90°）的取樣數，三軸都掃。越高越不易漏掉最佳方向，成本 O(n³)")]
        [Range(4, 24)]
        public int obbCoarseSteps = 12;

        [ShowIf("fitMode", ColliderFitMode.OBB)]
        [Tooltip("細化次數：在目前最佳角度附近反覆縮小窗口，逼近真正最小體積")]
        [Range(0, 6)]
        public int obbRefineIterations = 4;

        [ShowIf("fitMode", ColliderFitMode.OBB)]
        [Tooltip("每次細化時每軸的取樣數")]
        [Range(2, 8)]
        public int obbRefineSteps = 4;

        [ShowIf("fitMode", ColliderFitMode.OBB)] [Tooltip("OBB 子物件名稱")]
        public string obbChildName = "_OBBCollider";

        [Title("除錯")] [ReadOnly] public float lastVolume;

        [Title("Gizmo 設定")] public bool showGizmos = true;
        public Color gizmoVertexColor = Color.cyan;
        public Color gizmoBestBoxColor = Color.green;
        public Color gizmoCenterColor = Color.red;

        [Range(0.01f, 0.2f)] public float gizmoVertexSize = 0.05f;

        // 儲存 Gizmo 需要的資料
        private List<Vector3> debugVertices = new List<Vector3>();
        private Vector3 debugBestCenter;
        private Vector3 debugBestSize;
        private Quaternion debugBestRotation;

        #region Main Methods

        [Button("調整 Collider", ButtonSizes.Large)]
        [ContextMenu("Adjust Collider To Bounds")]
        public void AdjustColliderToBounds()
        {
            Renderer[] renderers = GetTargetRenderers();

            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning(
                    $"[{gameObject.name}] 找不到任何 Renderer，無法調整 Collider",
                    this
                );
                return;
            }

            switch (fitMode)
            {
                case ColliderFitMode.AABB:
                    FitAABB(renderers);
                    break;
                case ColliderFitMode.OBB:
                    FitOBB(renderers);
                    break;
                case ColliderFitMode.ConvexMesh:
                    FitConvexMesh(renderers);
                    break;
            }
        }

        [Button("清除產生的 Collider")]
        public void ClearGeneratedColliders()
        {
            // 清除 OBB 子物件
            Transform obbChild = transform.Find(obbChildName);
            if (obbChild != null)
            {
                DestroyImmediate(obbChild.gameObject);
            }

            // 清除 MeshCollider
            MeshCollider meshCol = GetComponent<MeshCollider>();
            if (meshCol != null)
            {
                DestroyImmediate(meshCol);
            }

            // 重置 BoxCollider
            BoxCollider boxCol = GetComponent<BoxCollider>();
            if (boxCol != null)
            {
                boxCol.center = Vector3.zero;
                boxCol.size = Vector3.one;
            }
        }

        #endregion

        #region Renderer Collection

        private Renderer[] GetTargetRenderers()
        {
            switch (rendererSource)
            {
                case RendererSource.SingleRenderer:
                    return targetRenderer != null ? new[] { targetRenderer } : null;

                case RendererSource.ChildrenOnly:
                    List<Renderer> childRenderers = new List<Renderer>();
                    foreach (Transform child in transform)
                    {
                        childRenderers.AddRange(child.GetComponentsInChildren<Renderer>());
                    }

                    return childRenderers.ToArray();

                case RendererSource.SelfAndChildren:
                    return GetComponentsInChildren<Renderer>();

                case RendererSource.All:
                    HashSet<Renderer> allRenderers = new HashSet<Renderer>();
                    allRenderers.UnionWith(GetComponentsInChildren<Renderer>());
                    allRenderers.UnionWith(GetComponentsInParent<Renderer>());
                    Renderer[] result = new Renderer[allRenderers.Count];
                    allRenderers.CopyTo(result);
                    return result;

                default:
                    return null;
            }
        }

        private List<Vector3> CollectVertices(Renderer[] renderers)
        {
            List<Vector3> vertices = new List<Vector3>();

            foreach (Renderer rend in renderers)
            {
                if (!rend.enabled)
                    continue;

                MeshFilter meshFilter = rend.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    Transform meshTransform = rend.transform;

                    foreach (Vector3 vertex in mesh.vertices)
                    {
                        vertices.Add(meshTransform.TransformPoint(vertex));
                    }
                }
            }

            return vertices;
        }

        #endregion

        #region AABB (Axis-Aligned Bounding Box)

        private void FitAABB(Renderer[] renderers)
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
            }

            // 計算合併的世界空間邊界
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
            }

            // 轉換到本地空間
            boxCollider.center =
                transform.InverseTransformPoint(combinedBounds.center) + Vector3.up * _offsetY;

            // 處理縮放
            Vector3 lossyScale = transform.lossyScale;
            boxCollider.size = new Vector3(
                combinedBounds.size.x / lossyScale.x,
                combinedBounds.size.y / lossyScale.y,
                combinedBounds.size.z / lossyScale.z
            );

            lastVolume = combinedBounds.size.x * combinedBounds.size.y * combinedBounds.size.z;
            Debug.Log($"[{gameObject.name}] AABB 調整完成，體積: {lastVolume:F2}");
        }

        #endregion

        #region OBB (Oriented Bounding Box)

        private void FitOBB(Renderer[] renderers)
        {
            List<Vector3> vertices = CollectVertices(renderers);
            if (vertices.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] 找不到 Mesh 頂點，改用 AABB");
                FitAABB(renderers);
                return;
            }

            // 儲存頂點用於 Gizmo 顯示
            debugVertices = new List<Vector3>(vertices);

            // 三軸暴力搜尋 + 階層細化，找最小體積方向
            float minVolume = float.MaxValue;
            Vector3 bestAngles = Vector3.zero;
            Quaternion bestRotation = Quaternion.identity;
            Vector3 bestCenter = Vector3.zero;
            Vector3 bestSize = Vector3.one;

            // 粗掃：BoxCollider 對三主軸各有 90° 旋轉對稱性，每軸只需掃 [0,90)
            float coarseStep = 90f / obbCoarseSteps;
            for (int ix = 0; ix < obbCoarseSteps; ix++)
            for (int iy = 0; iy < obbCoarseSteps; iy++)
            for (int iz = 0; iz < obbCoarseSteps; iz++)
            {
                EvaluateOBB(
                    vertices,
                    new Vector3(ix * coarseStep, iy * coarseStep, iz * coarseStep),
                    ref minVolume,
                    ref bestAngles,
                    ref bestRotation,
                    ref bestCenter,
                    ref bestSize
                );
            }

            // 細化：在目前最佳角度附近反覆縮小窗口逼近
            float window = coarseStep;
            for (int iter = 0; iter < obbRefineIterations; iter++)
            {
                Vector3 pivot = bestAngles;
                float step = 2f * window / obbRefineSteps;
                for (int ix = 0; ix <= obbRefineSteps; ix++)
                for (int iy = 0; iy <= obbRefineSteps; iy++)
                for (int iz = 0; iz <= obbRefineSteps; iz++)
                {
                    EvaluateOBB(
                        vertices,
                        new Vector3(
                            pivot.x - window + ix * step,
                            pivot.y - window + iy * step,
                            pivot.z - window + iz * step
                        ),
                        ref minVolume,
                        ref bestAngles,
                        ref bestRotation,
                        ref bestCenter,
                        ref bestSize
                    );
                }

                window = step; // 下一層聚焦更細
            }

            // 儲存最佳結果用於 Gizmo 顯示
            debugBestCenter = bestCenter;
            debugBestSize = bestSize;
            debugBestRotation = bestRotation;

            // 創建或獲取 OBB 子物件
            Transform obbChild = transform.Find(obbChildName);
            if (obbChild == null)
            {
                GameObject obbGO = new GameObject(obbChildName);
                obbChild = obbGO.transform;
            }

            // bestCenter 是在旋轉後的本地空間中的中心點
            // 需要先用 bestRotation 轉回世界空間偏移，再加上 transform.position
            Vector3 worldCenter =
                transform.position + bestRotation * bestCenter + transform.up * _offsetY;

            // 先設置世界座標的旋轉和位置，再設定父物件
            obbChild.position = worldCenter;
            obbChild.rotation = bestRotation;

            // 設定父物件但保持世界座標不變
            if (obbChild.parent != transform)
            {
                obbChild.SetParent(transform, true);
            }

            // 添加或獲取 BoxCollider
            BoxCollider obbCollider = obbChild.GetComponent<BoxCollider>();
            if (obbCollider == null)
            {
                obbCollider = obbChild.gameObject.AddComponent<BoxCollider>();
                obbCollider.isTrigger = true;
            }

            obbCollider.center = Vector3.zero;
            obbCollider.size = bestSize;

            // 禁用自身的 BoxCollider（如果有的話）
            BoxCollider selfCollider = GetComponent<BoxCollider>();
            if (selfCollider != null)
            {
                selfCollider.enabled = false;
            }

            lastVolume = bestSize.x * bestSize.y * bestSize.z;
            Debug.Log(
                $"[{gameObject.name}] OBB 調整完成，體積: {lastVolume:F2}，減少了 {(1 - lastVolume / GetAABBVolume(renderers)) * 100:F1}%",
                this
            );
        }

        /// <summary>
        /// 以指定 Euler 角度計算 OBB，若體積比目前最小值更小則更新最佳結果
        /// </summary>
        private void EvaluateOBB(
            List<Vector3> vertices,
            Vector3 anglesDeg,
            ref float minVolume,
            ref Vector3 bestAngles,
            ref Quaternion bestRotation,
            ref Vector3 bestCenter,
            ref Vector3 bestSize
        )
        {
            Quaternion rotation = Quaternion.Euler(anglesDeg);
            CalculateOBB(vertices, rotation, out Vector3 center, out Vector3 size);
            float volume = size.x * size.y * size.z;

            if (volume < minVolume)
            {
                minVolume = volume;
                bestAngles = anglesDeg;
                bestRotation = rotation;
                bestCenter = center;
                bestSize = size;
            }
        }

        private void CalculateOBB(
            List<Vector3> worldVertices,
            Quaternion rotation,
            out Vector3 center,
            out Vector3 size
        )
        {
            Quaternion invRotation = Quaternion.Inverse(rotation);
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (Vector3 worldVertex in worldVertices)
            {
                Vector3 localVertex = invRotation * (worldVertex - transform.position);
                min = Vector3.Min(min, localVertex);
                max = Vector3.Max(max, localVertex);
            }

            center = (min + max) / 2f;
            size = max - min;
        }

        private float GetAABBVolume(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.size.x * bounds.size.y * bounds.size.z;
        }

        #endregion

        #region Convex Mesh Collider

        private void FitConvexMesh(Renderer[] renderers)
        {
            List<Vector3> worldVertices = CollectVertices(renderers);
            if (worldVertices.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] 找不到 Mesh 頂點，改用 AABB");
                FitAABB(renderers);
                return;
            }

            // 將世界座標轉換為本地座標
            Vector3[] localVertices = new Vector3[worldVertices.Count];
            for (int i = 0; i < worldVertices.Count; i++)
            {
                localVertices[i] = transform.InverseTransformPoint(worldVertices[i]);
            }

            // 創建一個簡單的 Mesh 給 MeshCollider 使用
            // Unity 會自動計算 Convex Hull
            Mesh convexMesh = new Mesh();
            convexMesh.name = "ConvexColliderMesh";
            convexMesh.vertices = localVertices;

            // 創建簡單的三角形索引（Unity 會在 Convex 模式下忽略這個）
            int[] triangles = new int[Mathf.Min(localVertices.Length, 255) * 3];
            for (int i = 0; i < triangles.Length / 3; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = Mathf.Min(i + 1, localVertices.Length - 1);
                triangles[i * 3 + 2] = Mathf.Min(i + 2, localVertices.Length - 1);
            }

            convexMesh.triangles = triangles;

            // 獲取或創建 MeshCollider
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = convexMesh;
            meshCollider.convex = true;

            // 禁用 BoxCollider（如果有的話）
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }

            // 計算體積（使用 bounds 作為估計）
            lastVolume =
                meshCollider.bounds.size.x
                * meshCollider.bounds.size.y
                * meshCollider.bounds.size.z;
            Debug.Log(
                $"[{gameObject.name}] Convex MeshCollider 調整完成，估計體積: {lastVolume:F2}"
            );
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos)
                return;

            // 繪製所有採樣的頂點
            if (debugVertices != null && debugVertices.Count > 0)
            {
                Gizmos.color = gizmoVertexColor;
                foreach (Vector3 vertex in debugVertices)
                {
                    Gizmos.DrawSphere(vertex, gizmoVertexSize);
                }
            }

            // 繪製最佳 OBB 包圍盒
            if (debugBestSize != Vector3.zero)
            {
                // 計算世界空間的中心點
                Vector3 worldCenter = transform.position + debugBestRotation * debugBestCenter;

                // 繪製中心點
                Gizmos.color = gizmoCenterColor;
                Gizmos.DrawSphere(worldCenter, gizmoVertexSize * 2f);

                // 繪製旋轉後的 Box
                Gizmos.color = gizmoBestBoxColor;
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(worldCenter, debugBestRotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, debugBestSize);
                Gizmos.matrix = oldMatrix;

                // 繪製軸向指示
                Gizmos.color = Color.red;
                Gizmos.DrawRay(
                    worldCenter,
                    debugBestRotation * Vector3.right * debugBestSize.x * 0.5f
                );
                Gizmos.color = Color.green;
                Gizmos.DrawRay(
                    worldCenter,
                    debugBestRotation * Vector3.up * debugBestSize.y * 0.5f
                );
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(
                    worldCenter,
                    debugBestRotation * Vector3.forward * debugBestSize.z * 0.5f
                );
            }
        }

        #endregion

        #region Callbacks

        public void OnBeforePrefabSave()
        {
            // 可選：在儲存 Prefab 前自動調整
            // AdjustColliderToBounds();
        }

        public void OnHeavySceneSaving()
        {
            if (fitOnSceneSave)
            {
                AdjustColliderToBounds();
            }
        }

        #endregion
    }
}
