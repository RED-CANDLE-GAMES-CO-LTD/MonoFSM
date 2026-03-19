using UnityEngine;

namespace MonoFSM.Editor.AnimatorParamReferenceSystem
{
    /// <summary>
    /// 描述一個 Animator Parameter 被設定的資訊
    /// </summary>
    public class AnimatorParamInfo
    {
        /// <summary>
        /// 參數名稱
        /// </summary>
        public string ParameterName;

        /// <summary>
        /// 設定此參數的 Action Component
        /// </summary>
        public Component ActionComponent;

        /// <summary>
        /// 目標 Animator
        /// </summary>
        public Animator TargetAnimator;

        /// <summary>
        /// Action 所屬的 State 名稱
        /// </summary>
        public string StateName;

        /// <summary>
        /// Action 的類型名稱
        /// </summary>
        public string ActionTypeName;

        /// <summary>
        /// Action 的 Description (如果有)
        /// </summary>
        public string ActionDescription;
    }
}
