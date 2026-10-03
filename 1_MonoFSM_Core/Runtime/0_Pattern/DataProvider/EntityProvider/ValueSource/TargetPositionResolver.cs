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
        [BoxGroup("PosResolver")] [Tooltip("故意留所有欄位，依照順序 resolve")] [DropDownRef]
        public VarVector3 _targetPosVar;

        [BoxGroup("PosResolver")]
        //note: 故意留著，依照順序 resolve
        [DropDownRef]
        public VarTransform _targetTransformVar;

        [BoxGroup("PosResolver")]
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
            get => ResolveTransformOnce();
        }

        /// <summary>
        ///     依優先序（VarTransform > VarEntity > 直接指定的 Transform）解出目標 Transform，
        ///     **每顆 Var 的 Value 只讀一次**。VarEntity 常是 Getter（例如 d_Chasing Target 底下掛
        ///     VarEntityRef → proxy var），每讀一次 Value 就重跑一遍 value source 挑選 + proxy GetVar 鏈；
        ///     舊寫法 HasEntityValue + GetEntityTransform 一次解析要讀 3 次，每 tick 跑的 Move Action /
        ///     距離 Condition 會被放大好幾倍。不處理 VarVector3（它不是 Transform 來源）。
        /// </summary>
        private Transform ResolveTransformOnce()
        {
            if (_targetTransformVar != null)
            {
                var t = _targetTransformVar.Value;
                if (t != null) return t;
            }

            if (_targetEntityVar != null)
            {
                var entity = _targetEntityVar.Value;
                if (entity != null) return TransformOfEntity.GetEntityTransform(entity);
            }

            return _targetTransform != null ? _targetTransform : null;
        }

        /// <summary>
        ///     目前生效的來源 runtime 上「真的有值」嗎。
        ///     純查詢，**不改動 GetTargetPosition 的既有優先序行為**（那條路上很多呼叫端靠
        ///     「沒值就回 fallback」運作，改成早退會炸一票）。
        ///     VarVector3 走 IsValueExist：proxy 型（掛在 VarEntity 底下）在 entity 還沒解到時會回 false，
        ///     這是「持有者還沒 spawn」與「值剛好是 (0,0,0)」唯一分得開的地方 —— 不要拿 Vector3.zero 比對。
        /// </summary>
        public bool IsTargetValueReady
        {
            get
            {
                if (_targetPosVar != null)
                    return _targetPosVar.IsValueExist;
                return ResolveTransformOnce() != null;
            }
        }

        /// <summary>
        /// 依優先順序解析目標位置：VarVector3 > VarTransform > VarEntity > Transform
        /// </summary>
        public Vector3 GetTargetPosition(Vector3 fallback) //fallback很鳥
        {
            return TryGetTargetPosition(out var pos) ? pos : fallback;
        }

        /// <summary>
        ///     等同 <c>HasTarget ? GetTargetPosition(x) : 失敗</c>，但每顆來源 Var 的 Value 只讀一次。
        ///     每 tick 都要「先判有沒有目標再取位置」的呼叫端（Move Action、距離 Condition）用這顆，
        ///     不要再 HasTarget + GetTargetPosition 分兩次問（VarEntity Getter 會被重算 4 次）。
        ///     優先序：VarVector3（有指派就用，不看 IsValueExist，跟舊行為一致）> VarTransform > VarEntity > Transform。
        /// </summary>
        public bool TryGetTargetPosition(out Vector3 position)
        {
            // 1. VarVector3 — 被指派的靜態位置（最高優先，通常由 Action 動態設定）
            if (_targetPosVar != null)
            {
                position = _targetPosVar.Value;
                return true;
            }

            // 2~4. VarTransform > VarEntity > editor 直接指定的 Transform
            var t = ResolveTransformOnce();
            if (t != null)
            {
                position = t.position;
                return true;
            }

            position = default;
            return false;
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
