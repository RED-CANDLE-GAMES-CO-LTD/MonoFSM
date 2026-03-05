using System;
using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.Attributes;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.Variable;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Variable;
using MonoFSM.Runtime.Vote;
using MonoFSMCore.Runtime.LifeCycle;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Variable
{
    [Serializable]
    public class VarFloatFoldOut : VarFoldOut<VarFloat, float>
    {
    }

    [Serializable]
    public class VarMonoObjFoldOut : VarFoldOut<VarMonoObj, MonoObj>
    {
        [HideIf(nameof(HasVar))] [PrefabFilter] [SerializeField]
        private MonoObj _constObjValue;

        protected override bool HasOverrideHideValue => true;

        public override MonoObj Value => HasVar ? base.Value : _constObjValue;
    }

    //這啥？定位一樣？
    [Serializable]
    public class VarFoldOut<TVarType, TValueType>
        where TVarType : AbstractMonoVariable //用attribute processor幫她加inlinefield
    {
        [SerializeField] bool _isVarNeeded;

        [ShowIf(nameof(_isVarNeeded))] [SerializeField]
        private TVarType _var;

        [HideIf(nameof(HideConstValue))] [SerializeField]
        private TValueType _constValue;

        public virtual TValueType Value => HasVar ? _var.GetValue<TValueType>() : _constValue;

        public bool HasVar => _var != null;

        bool HideConstValue => HasVar || HasOverrideHideValue;

        protected virtual bool HasOverrideHideValue => false;
    }

    //varTransformWapper
    [Serializable]
    public class VarTransformWrapper : VarWrapper<VarTransform, Transform>
    {
        public bool HasValue => Value != null;
    }

    // [InlineField]
    [Serializable]
    public class VarCompField<T>
        where T : class //用attribute processor幫她加inlinefield
    {
        public bool HasVar => _varComp != null;

        [SerializeField] VarComp _varComp;
        public T Value => _varComp.Value as T;
    }

    [Serializable]
    public class VarVector3Wrapper : VarWrapper<VarVector3, Vector3>
    {
        public VarVector3Wrapper() { }

        public VarVector3Wrapper(Vector3 defaultValue)
            : base(defaultValue) { }
    }

    [Serializable]
    public class VarVoteWrapper : VarWrapper<VarVote, bool>
    {
        public VarVoteWrapper() { }

        public VarVoteWrapper(bool defaultValue)
            : base(defaultValue) { }
    }

    [Serializable]
    public class VarBoolWrapper : VarWrapper<VarBool, bool>
    {
        public VarBoolWrapper() { }

        public VarBoolWrapper(bool defaultValue)
            : base(defaultValue) { }
    }

    [Serializable]
    public class VarIntWrapper : VarWrapper<VarInt, int>
    {
        public VarIntWrapper()
        {
        }

        public VarIntWrapper(int defaultValue)
            : base(defaultValue)
        {
        }
    }
    [Serializable]
    public class VarGameDataWrapper : VarWrapper<VarGameData, GameData>
    {
        public VarGameDataWrapper() { }

        public VarGameDataWrapper(GameData defaultValue)
            : base(defaultValue) { }
    }

    [Serializable]
    public class VarFloatWrapper : VarWrapper<VarFloat, float>
    {

        public VarFloatWrapper() { }

        public VarFloatWrapper(float defaultValue)
            : base(defaultValue) { }
    }

    [Serializable]
    public class VarEntityWrapper : VarWrapper<VarEntity, MonoEntity>
    {
        public VarEntityWrapper() { }

        public VarEntityWrapper(MonoEntity defaultValue)
            : base(defaultValue) { }
    }

    [Serializable]
    public class VarListEntityWrapper : VarWrapper<VarListEntity, List<MonoEntity>>
    {
        public VarListEntityWrapper() { }

        public VarListEntityWrapper(List<MonoEntity> defaultValue)
            : base(defaultValue) { }
    }



    public abstract class AbstractVarWrapper { }

    //FIXME: 真的有需要wrapper嗎？
    [Serializable]
    public class VarWrapper<TVar, TValue> : AbstractVarWrapper
        where TVar : AbstractMonoVariable
    {
        public string Description => _var != null ? _var.Description : _tempValue.ToString();
        [HideIf("_var", null, false)]
        [ShowInInspector]
        [SerializeField]
        private TValue _tempValue;

        [ShowInDebugMode]
        [SOConfig("VariableType")]
        public VariableTag _bindTag;

        [Component]
        public TVar _var;

        public VarWrapper() { }

        public VarWrapper(TValue defaultValue)
        {
            _tempValue = defaultValue;
        }

        [PreviewInInspector]
        public TValue Value
        {
            get
            {
                if (_var == null)
                    return _tempValue;
                return _var.Get<TValue>();
            }
        }



        public void SetValue(TValue value, Object byWho, string reason = "")
        {
            // Debug.Log($"VarWrapper SetValue: Setting value to {value}", byWho);
            if (_var == null)
            {
                // Debug.Log($"VarWrapper SetValue: Var is null, setting temp value to {value}",
                //     byWho);
                _tempValue = value;
                return;
            }

            _var.SetRaw(value, byWho);
        }

        public void ClearValue()
        {
            if (_var != null)
            {
                _var.ClearValue();
            }

            _tempValue = default;
        }

        public override string ToString()
        {
            return _var != null ? _var.name : _tempValue?.ToString() ?? "null";
        }
    }
}
