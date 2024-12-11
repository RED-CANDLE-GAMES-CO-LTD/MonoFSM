using System;
using System.Linq;
using System.Reflection;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace RCGMaker.Runtime.FSM._2_Variable
{
    [Serializable]
   public class MySerializedType
    {
        [Button]
        void GetTypeFromString()
        {
            _type = Type.GetType(typeName);
        }
        
        [ShowInInspector]
        [OnValueChanged(nameof(TypeToString))]
        [TypeDrawerSettings(BaseType = typeof(AbstractVariable))]
        private Type _type;

        public Type RestrictType
        {
            get
            {
                if(_type == null)
                    GetTypeFromString();
                return _type;
            }
        }
        
        void TypeToString()
        {
            if(_type == null)
                return;
            typeName = _type.ToString();
        }
        
        [Required]
        [PreviewInInspector]
        [SerializeField]
        string typeName;
    }
    
    [CreateAssetMenu(menuName = "RCG/VariableTag")]
    public class VariableTag : ScriptableObject//, IFloatValue
    {
        //FIXME: 限定型別？
        //FIXME: 下拉式巢狀分類: 
        
        //可以DI標記variable類型，像是血量？要降低對方的血量之類的
        // [InlineProperty]
        public MySerializedType _variableType;

       //FIXME: Editor time 把雙向連結撈出來
#if UNITY_EDITOR
        [PreviewInInspector]
        AbstractVariable[] bindedVariables;
        
        [Button]
        void GetBindedVariables()
        {
            bindedVariables = FindObjectsOfType<AbstractVariable>(true).Where(v => v.varTag == this).ToArray();
        }
#endif
    }
}