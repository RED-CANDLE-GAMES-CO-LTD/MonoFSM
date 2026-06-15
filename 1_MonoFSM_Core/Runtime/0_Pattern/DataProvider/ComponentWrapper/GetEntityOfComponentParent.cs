using System;
using MonoDebugSetting;
using MonoFSM.Core;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Variable;
using MonoFSM.Variable.Attributes;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    public class GetEntityOfComponentParent : AbstractValueSource<MonoEntity>
    {
        public VarComp _compProvider;

        public override string Description => "Get ParentEntity Of " + _compProvider?.Description;

        public override MonoEntity Value
        {
            get
            {
                if (_compProvider == null || _compProvider.Value == null)
                    return null;

                if (_compProvider.Value is IParentEntityProvider entityProvider)
                {
                    return entityProvider.ParentEntity;
                }

//warning?
                if (RuntimeDebugSetting.IsDebugMode)
                    Debug.LogWarning(
                        $"[GetEntityOfComponentParent] Component {_compProvider.Value.name} does not implement IParentEntityProvider. Attempting to get MonoEntity from parent hierarchy.",
                        this);
                return _compProvider.Value.GetComponentInParent<MonoEntity>();
            }
        }
    }
}
