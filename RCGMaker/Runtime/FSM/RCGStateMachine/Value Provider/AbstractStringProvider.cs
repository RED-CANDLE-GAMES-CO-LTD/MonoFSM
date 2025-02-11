using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using I2.Loc;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using RCGMaker.Core.DataProvider;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

//
public abstract class AbstractStringProvider : MonoBehaviour//, IStringProvider
{
    public abstract string StringValue
    {
        get;
    }

    public string GetString()
    {
        return StringValue;
    }
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

    [HideIf(nameof(IsTypeOriValue))] public List<LocalizedString> switchValues;

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

    // private void OnEnable()
    // {
    //     UpdateText();
    //
    //     if (_updateViewEventProvider != null)
    //     {
    //         _updateViewEventProvider.RegisterUpdate(UpdateText, this);
    //     }
    // }
    //
    // private void OnDisable()
    // {
    //     if (_updateViewEventProvider != null)
    //     {
    //         _updateViewEventProvider.UnRegisterUpdate(UpdateText, this);
    //     }
    // }

    //FIXME: 註冊？
    // public void UpdateNativeData(INativeData data)
    // {
    //     UpdateText();
    // }

    public override string StringValue => PreviewValue;
}