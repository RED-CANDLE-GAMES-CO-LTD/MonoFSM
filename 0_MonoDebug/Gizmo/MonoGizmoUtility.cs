using UnityEngine;

namespace MonoDebug.Gizmo
{
    /// <summary>
    /// Spline 系統專用的 Gizmo 繪製工具
    /// 整合所有 Spline 相關的 Gizmo 繪製功能
    /// </summary>
    public static class MonoGizmoUtility
    {
        #region Arrow Drawing

        /// <summary>
        /// 繪製箭頭（方法 1: 用於 SplitSwitch，使用 LookRotation）
        /// </summary>
        /// <param name="pos">箭頭位置</param>
        /// <param name="direction">箭頭方向（未歸一化）</param>
        /// <param name="arrowHeadLength">箭頭長度</param>
        /// <param name="arrowHeadAngle">箭頭角度</param>
        public static void DrawArrowHead(Vector3 pos, Vector3 direction,
            float arrowHeadLength = 0.5f, float arrowHeadAngle = 20.0f)
        {
            Vector3 right = Quaternion.LookRotation(direction) *
                            Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) *
                           Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * Vector3.forward;
            Gizmos.DrawRay(pos, right * arrowHeadLength);
            Gizmos.DrawRay(pos, left * arrowHeadLength);
        }

        /// <summary>
        /// 繪製箭頭（方法 2: 用於 SplineNode，使用 Cross Product）
        /// </summary>
        /// <param name="position">箭頭位置</param>
        /// <param name="direction">箭頭方向（歸一化）</param>
        /// <param name="size">箭頭大小</param>
        public static void DrawArrowCross(Vector3 position, Vector3 direction, float size)
        {
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(direction, Vector3.forward).normalized;

            Vector3 arrowTip = position + direction * size;
            Vector3 arrowLeft = position + (-direction + right) * size * 0.5f;
            Vector3 arrowRight = position + (-direction - right) * size * 0.5f;

            Gizmos.DrawLine(arrowTip, arrowLeft);
            Gizmos.DrawLine(arrowTip, arrowRight);
        }

        /// <summary>
        /// 繪製完整箭頭（線條 + 箭頭）
        /// </summary>
        /// <param name="from">起點</param>
        /// <param name="to">終點</param>
        /// <param name="color">顏色</param>
        /// <param name="arrowHeadLength">箭頭長度</param>
        public static void DrawArrowLine(Vector3 from, Vector3 to, Color color,
            float arrowHeadLength = 0.5f)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(from, to);

            Vector3 direction = (to - from).normalized;
            Vector3 midPoint = (from + to) * 0.5f;
            DrawArrowHead(midPoint, direction, arrowHeadLength);
        }

        /// <summary>
        /// 繪製完整箭頭（線條 + 箭頭），使用當前 Gizmos.color
        /// </summary>
        public static void DrawArrowLine(Vector3 from, Vector3 to, float arrowHeadLength = 0.5f)
        {
            Gizmos.DrawLine(from, to);
            Vector3 direction = (to - from).normalized;
            Vector3 midPoint = (from + to) * 0.5f;
            DrawArrowHead(midPoint, direction, arrowHeadLength);
        }

        #endregion

        #region Node Connection Drawing

        /// <summary>
        /// 繪製節點連接線（用於 SplineNode）
        /// </summary>
        /// <param name="startPos">起點位置</param>
        /// <param name="endPos">終點位置</param>
        /// <param name="isDefault">是否為預設路徑</param>
        /// <param name="drawArrow">是否繪製箭頭</param>
        public static void DrawNodeConnection(Vector3 startPos, Vector3 endPos, bool isDefault,
            bool drawArrow = true)
        {
            Gizmos.color = isDefault ? Color.green : Color.yellow;
            Gizmos.DrawLine(startPos, endPos);

            if (drawArrow)
            {
                Vector3 direction = (endPos - startPos).normalized;
                Vector3 midPoint = Vector3.Lerp(startPos, endPos, 0.5f);
                DrawArrowCross(midPoint, direction, 0.5f);
            }
        }

        #endregion

        #region Switch Drawing

        /// <summary>
        /// 繪製轉轍器當前選擇的分支（用於 SplitSwitch）
        /// </summary>
        /// <param name="fromPos">起點</param>
        /// <param name="toPos">終點</param>
        /// <param name="isSwitching">是否正在切換</param>
        public static void DrawSwitchBranch(Vector3 fromPos, Vector3 toPos, bool isSwitching)
        {
            Gizmos.color = isSwitching ? Color.yellow : Color.cyan;
            Gizmos.DrawLine(fromPos, toPos);

            Vector3 direction = (toPos - fromPos).normalized;
            Vector3 midPoint = (fromPos + toPos) * 0.5f;
            DrawArrowHead(midPoint, direction, 0.5f);
        }

        #endregion

        #region Intersection Drawing

        /// <summary>
        /// 繪製 Intersection 連接（用於 SplineIntersection）
        /// </summary>
        /// <param name="start">起點</param>
        /// <param name="end">終點</param>
        public static void DrawIntersection(Vector3 start, Vector3 end)
        {
            // 繪製連接線
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, end);

            // 繪製端點標記
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(start, 0.2f);
            Gizmos.DrawWireSphere(end, 0.2f);
        }

        #endregion

        #region Node Markers

        /// <summary>
        /// 繪製節點起點標記
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="isStartNode">是否為起始節點</param>
        /// <param name="radius">半徑</param>
        public static void DrawNodeStart(Vector3 position, bool isStartNode, float radius = 0.1f)
        {
            // Gizmos.color = isStartNode ? Color.green : Color.yellow;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(position, radius);
        }

        /// <summary>
        /// 繪製節點終點標記
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="isEndNode">是否為終點節點</param>
        /// <param name="radius">半徑</param>
        public static void DrawNodeEnd(Vector3 position, bool isEndNode, float radius = 0.1f)
        {
            Gizmos.color = Color.greenYellow;
            Gizmos.DrawWireSphere(position, radius);
        }

        /// <summary>
        /// 繪製節點起點和終點標記
        /// </summary>
        public static void DrawNodeMarkers(Vector3 start, Vector3 end, bool isStartNode,
            bool isEndNode)
        {
            DrawNodeStart(start, isStartNode); //蛤這很重要嗎？
            DrawNodeEnd(end, isEndNode);
        }

        #endregion

        #region Path Preview

        // DrawPath 方法已移除，因跨命名空間參考 SplineNode 會造成編譯錯誤
        // 如需路徑預覽，請在 SplineExt 命名空間的類別中直接實作

        #endregion

        #region Helper Methods

        /// <summary>
        /// 繪製虛線
        /// </summary>
        public static void DrawDashedLine(Vector3 from, Vector3 to, float dashLength = 0.5f)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            direction.Normalize();

            float currentDistance = 0f;
            bool draw = true;

            while (currentDistance < distance)
            {
                float segmentLength = Mathf.Min(dashLength, distance - currentDistance);
                Vector3 start = from + direction * currentDistance;
                Vector3 end = start + direction * segmentLength;

                if (draw)
                {
                    Gizmos.DrawLine(start, end);
                }

                currentDistance += segmentLength;
                draw = !draw;
            }
        }

        /// <summary>
        /// 繪製帶標籤的點
        /// </summary>
        public static void DrawLabeledPoint(Vector3 position, string label, Color color,
            float radius = 0.2f)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(position, radius);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.up * 0.5f, label);
#endif
        }

        #endregion
    }
}
