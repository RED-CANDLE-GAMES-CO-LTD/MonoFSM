using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

using Sirenix.OdinInspector;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;

public abstract class AbstractStringProvider : MonoBehaviour
{
    public abstract string StringValue { get; }

    public string GetString() 
        => StringValue;
}

public class StringFromDataProvider<TField> : AbstractStringProvider, IStringProvider
{
    //int value switch of strings
    //int of scriptable? 
    //int of pure class?
    //直接弄4個scriptable?
    [Required] [PreviewInInspector] [AutoParent]
    private INativeDataProvider dataInParent;

    [SerializeField] private GetType getType = GetType.Field;

    public enum GetType
    {
        Property,
        Field
    }

    private IEnumerable<string> GetPropertyNames()
    {
        if (dataInParent == null)
            return new List<string>();
        var type = dataInParent.GetNativeDataType();
        var names = new List<string>();

        if (getType == GetType.Property)
        {
            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Debug.Log("type: " + type + " properties: " + properties.Length);
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(TField))
                    names.Add(property.Name);
            }
        }
        else
        {
            var properties = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Debug.Log("type: " + type + " properties: " + properties.Length);

            foreach (var property in properties)
            {
                if (property.FieldType == typeof(TField))
                    names.Add(property.Name);
            }
        }

        return names;
    }

    [ValueDropdown("GetPropertyNames")] public string propertyName;

    //FIXME: 讓Text自己去替換才是對的嗎？用標記
    [HideIf(nameof(IsTypeOriValue))] public List<string> switchValues;
    // [HideIf(nameof(IsTypeOriValue))] public List<LocalizedString> switchValues;

    private bool IsTypeOriValue => valueType == ValueType.OriValue;

    private int GetValueInParent()
    {
        if (dataInParent == null)
            dataInParent = GetComponentInParent<INativeDataProvider>(true);
        var data = dataInParent.GetNativeData();

        if (getType == GetType.Property)
            return (int)dataInParent.GetNativeDataType().GetProperty(propertyName).GetValue(data);
        else
            return (int)dataInParent.GetNativeDataType().GetField(propertyName).GetValue(data);
    }

    public enum ValueType
    {
        OriValue,
        SwitchValueIndex
    }

    public ValueType valueType;

    [ShowInPlayMode]
    private string PreviewValue
    {
        get
        {
            var valueInt = GetValueInParent(); //TODO: get value from dataInParent
            if (valueType == ValueType.OriValue)
                return valueInt.ToString();
            if (valueType == ValueType.SwitchValueIndex)
            {
                if (valueInt < switchValues.Count && valueInt >= 0)
                    return switchValues[valueInt].ToString();
                return null;
            }

            return "";
        }
    }

    public override string StringValue => PreviewValue;
}