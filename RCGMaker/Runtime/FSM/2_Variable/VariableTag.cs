using System;
using System.Reflection;
using Sirenix.OdinInspector;
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
            typeName = _type.ToString();
        }
        
        [SerializeField]
        [HideInInspector]
        string typeName;
    }
    
    [CreateAssetMenu(menuName = "RCG/VariableTag")]
    public class VariableTag : ScriptableObject//, IFloatValue
    {
        //FIXME: 限定型別？
        //FIXME: 下拉式巢狀分類: 
        
        //可以DI標記variable類型，像是血量？要降低對方的血量之類的
        [InlineProperty]
        public MySerializedType _variableType;

       
    }
}