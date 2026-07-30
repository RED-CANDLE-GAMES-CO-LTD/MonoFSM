using System;
using MonoFSM.Runtime.Variable;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoValueProvider
{
    /// <summary>
    /// 共用的目標位置解析器，統一 VarVector3 / VarTransform / VarEntity / Transform 四種目標來源
    /// 優先順序：VarVector3 > VarTransform > VarEntity > Transform(editor 直接指定)
    /// 所有來源都透過 IsValueExist 確認 runtime 有值才使用
    /// </summary>
    [Serializable]
    public class TargetPositionResolver
    {
        [BoxGroup("PosResolver")]
        [Tooltip("故意留所有欄位，依照順序 resolve")] [DropDownRef]
        public VarVector3 _targetPosVar;

        [BoxGroup("PosResolver")]
        //note: 故意留著，依照順序 resolve
        // [HideIf(nameof(_targetPosVar))]
        [DropDownRef] public VarTransform _targetTransformVar;

        [BoxGroup("PosResolver")]
        // [HideIf(nameof(_targetPosVar))]
        [DropDownRef] public VarEntity _targetEntityVar;

        [BoxGroup("PosResolver")] [Tooltip("最低優先，editor 直接指定的 Transform 引用")]
        public Transform _targetTransform;

        private bool HasPosValue => _targetPosVar != null;

        private bool HasTransformValue =>
            _targetTransformVar != null && _targetTransformVar.Value != null;

        private bool HasEntityValue => _targetEntityVar != null && _targetEntityVar.Value != null;

        private bool HasDirectTransform => _targetTransform != null;

        [ShowInInspector, ReadOnly] //runtime用
        public bool HasTarget =>
            HasPosValue || HasTransformValue || HasEntityValue || HasDirectTransform;

        [ShowInInspector, ReadOnly]
        public string ActiveSource //editor看用的
        {
            get
            {
                if (_targetPosVar != null) return _targetPosVar.Description;
                if (_targetTransformVar != null) return _targetTransformVar.Description;
                if (_targetEntityVar != null) return _targetEntityVar.Description;
                if (_targetTransform != null) return _targetTransform.name;
                return "None";
            }
        }

        [ShowInInspector, ReadOnly]
        public string BindingSource //editor看用的
        {
            get
            {
                if (_targetPosVar != null) return _targetPosVar.Description;
                if (_targetTransformVar != null) return _targetTransformVar.Description;
                if (_targetEntityVar != null) return _targetEntityVar.Description;
                if (_targetTransform != null) return _targetTransform.name;
                return "None";
            }
        }

        [ShowInInspector, ReadOnly]
        public Transform ResolvedTransform
        {
            get
            {
                if (HasTransformValue) return _targetTransformVar.Value;
                if (HasEntityValue) return TransformOfEntity.GetEntityTransform(_targetEntityVar);
                if (HasDirectTransform) return _targetTransform;
                return null;
            }
        }

        /// <summary>
        /// 依優先順序解析目標位置：VarVector3 > VarTransform > VarEntity > Transform
        /// </summary>
        public Vector3 GetTargetPosition(Vector3 fallback) //fallback很鳥
        {
            // 1. VarVector3 — 被指派的靜態位置（最高優先，通常由 Action 動態設定）
            if (HasPosValue)
                return _targetPosVar.Value;

            // 2. VarTransform — 直接 Transform 引用
            if (HasTransformValue)
                return _targetTransformVar.Value.position;

            // 3. VarEntity — 透過 Entity 拿 Transform 再取 position
            if (HasEntityValue)
            {
                var t = TransformOfEntity.GetEntityTransform(_targetEntityVar);
                if (t != null) return t.position;
            }

            // 4. Transform — editor 直接指定的引用（最低優先）
            if (HasDirectTransform)
                return _targetTransform.position;

            return fallback;
        }

        /// <summary>
        /// 清除靜態位置目標（VarVector3），通常在到達後呼叫
        /// </summary>
        public void ClearPositionTarget()
        {
            _targetPosVar?.ClearValue();
        }
    }
}
