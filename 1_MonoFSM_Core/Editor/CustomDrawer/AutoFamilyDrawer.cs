using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Auto.Utils;
using MonoFSM.Variable;
using Sirenix.OdinInspector.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Core
{
     public class AutoUtils
    {
        public static bool IsSerialized(InspectorProperty property,object belongObj,MonoBehaviour mb,out FieldInfo field)
        {
            var isSerialized = property.Info.GetAttribute<SerializeField>() != null;
            var propName = property.Name;
            
            //要public/nonpublic都找, 這個API會誤會？
            field = ReflectionHelperMethods.FindNonPublicFieldInBaseClasses(belongObj.GetType(),propName);
            // field = belongObj.GetType().GetField(propName, BindingFlags.Public |BindingFlags.NonPublic | BindingFlags.Instance);
            
            // var publicField = belongObj.GetType().GetField(propName, BindingFlags.Public | BindingFlags.Instance);
            // privateField = belongObj.GetType().GetField(propName, BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field == null)
            {
                Debug.LogError("No Field Found: fieldName:"+propName+" mb:"+mb,mb);
                
                return false;
            }
                
            bool isPublicOrSerialized = field.IsPublic || isSerialized;
            return isPublicOrSerialized;
        }

        public static void SetPrivate(FieldInfo field, object belongObj, InspectorProperty property,
            AutoFamilyAttribute autoAttribute, MonoBehaviour mb, Type componentType)
        {
            field.SetValue(belongObj,
                property.ValueEntry.TypeOfValue.IsArray
                    ? autoAttribute.GetComponentsToReference(mb, mb.gameObject, componentType)
                    : autoAttribute.GetTheSingleComponent(mb, componentType));
        }

        public static void SetSerialized(IPropertyValueEntry valueEntry, AutoFamilyAttribute autoAttribute,
            MonoBehaviour mb, Type componentType)
        {
            valueEntry.WeakSmartValue = valueEntry.TypeOfValue.IsArray
                ? autoAttribute.GetComponentsToReference(mb, mb.gameObject, componentType)
                : autoAttribute.GetTheSingleComponent(mb, componentType);
        }
    }
    [DrawerPriority(0, 100, 0)]
    public abstract class AutoFamilyDrawer<TAutoFamily> : OdinAttributeDrawer<TAutoFamily>
        where TAutoFamily : AutoFamilyAttribute
    {
        Type componentType => Property.ValueEntry.TypeOfValue;
        object belongObj => Property.ParentValues[0];

        private MonoBehaviour GetMB
        {
            get
            {
                var parent = Property.FindParent((parent) => parent.ParentValues[0] is MonoBehaviour,true);
                if(parent == null)
                    Debug.LogError("No MonoBehaviour Parent Value found?"+Property.Name);
                var belongMb = parent.ParentValues[0] as MonoBehaviour;
                return belongMb;
            }
        }
        
        protected override void Initialize()
        {
            //play的時候不要抓
            if (Application.isPlaying)
                return;
            var mb = GetMB;
            if (mb == null)
            {
                Debug.LogError("No Parent GetMB Value");
                return;
            }
            if(componentType == null)
            {
                Debug.LogError("No Component Type Found");
                return;
            }
            
            //if property is array element
            if (Property.Name.Contains('$'))
            {
                return;
            }

            var isSerialized = AutoUtils.IsSerialized(Property,belongObj,mb,out var field);
            if (field == null)
            {
                //FIXME: 參考ReflectionHelperMethod GetNonPublicFieldsInBaseClasses
                Debug.LogError("No Field Found, field might be private, change to protected: "+Property.Name+" "+mb,mb);
                return;
            }
            if (isSerialized)
                AutoUtils.SetSerialized(Property.ValueEntry,Attribute,mb,componentType);
            else
                AutoUtils.SetPrivate(field,belongObj,Property,Attribute,mb,componentType);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);
        }
    }
}