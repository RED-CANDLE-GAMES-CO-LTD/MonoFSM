using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.Action.VariableAction;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.EditorExtension;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    public class SetVarBoolAction
        : AbstractStateAction,
            IArgEventReceiver<bool>,
            IHierarchyValueInfo
    {
        //FIXME: 用selection dropdown來篩選
        //這個還可以化簡嗎？整個description就代表含義了..但沒有Reference可能還是不夠用
        // protected override string renamePostfix =>
        public override string Description =>
            _target != null
                ? _target.name
                  + " = "
                  + (HasSourceVar ? _sourceVar.Description : _sourceValue.ToString())
                : "null target";

        private IList<ValueDropdownItem<VarBool>> GetVariables()
        {
            return this.GetVariableValueDropdownItems<VarBool>();
            // var items = new List<ValueDropdownItem<VarBool>>();
            // var contexts = GetComponentsInParent<VariableOwner>(true);
            // foreach (var context in contexts)
            // {
            //     var vars = context.GetComponentsInChildren<VarBool>(true);
            //     foreach (var var in vars)
            //     {
            //         var owner = var.GetComponentInParent<VariableOwner>();
            //         items.Add(new ValueDropdownItem<VarBool>(owner.name + "/" + var.name, var));
            //     }
            // }
            //
            // return items;
        }

        public bool IsVarExternal => _target?.BindEntity != BindEntity;

        [OnValueChanged(nameof(Rename))]
        [Required]
        //有辦法判斷不是
        public VarBool _target; //var?

        //ObjectReference還指不到耶？

        //FIXME: Multiple的話另外寫SetVariableComplexAction, 直接用VariableProviderList之類的好了？
        // [ShowIf("Multiple")] public List<VarBool> targetFlags;

        //常數來源值。語意上是 source，但沿用舊名：改名會讓既有 prefab override 的 propertyPath 失效
        [FormerlySerializedAs("TargetValue")] [HideIf(nameof(HasSourceVar))]
        public bool _sourceValue = true;

        //可選：值改從另一個 VarBool 讀，有指定就蓋過 TargetValue
        [DropDownRef(isOptional: true)] public VarBool _sourceVar;

        private bool HasSourceVar => _sourceVar != null;

        private bool SourceValue => HasSourceVar ? _sourceVar.Value : _sourceValue;

        // public bool Multiple = false;


        protected override void OnActionExecuteImplement()
        {
            SetValue();
        }

        // public override void EventReceived<T>(T arg)
        // {
        //
        //     // this.Log("EventReceived setVariableBoolAction");
        //     if (arg is bool b)
        //         SetValue(b);
        //     else
        //         SetValue();
        // }

        private void SetValue(bool v)
        {
            if (_target == null)
            {
                Debug.LogError("targetFlag==null", this);
                return;
            }

            this.Log($"SetVariableBool {_target} SetValue:{v}");
            _target.SetValue(v, this);
            // }
        }

        private void SetValue()
        {
            SetValue(SourceValue);
        }

        public void ArgEventReceived(bool arg)
        {
            SetValue(arg);
        }

        public string ValueInfo => "Cross Ref:" + _target.BindEntity.name; //highlight顏色？
        public bool IsDrawingValueInfo => _target != null && IsVarExternal;
    }

    // public class SetPropertyAction : AbstractAction
    // {
    //     [Filter(Properties = true, Fields = true)]
    //     public UnityMember property;

    //     //寫在哪裡？
    //     public float argFloat = 0;
    //     public int argInt = 0;

    //     protected override void OnStateEnterImplement()
    //     {
    //         var paramTypes = property.parameterTypes;

    //         if (paramTypes[0] == typeof(int))
    //         {
    //             property.Set(argInt);
    //         }
    //     }

    // }
}
