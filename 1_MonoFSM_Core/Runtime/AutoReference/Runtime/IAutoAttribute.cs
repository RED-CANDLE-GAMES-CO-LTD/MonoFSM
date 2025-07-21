using System;
using System.Reflection;
using UnityEngine;

public interface IAutoAttribute
{
    bool Execute(MonoBehaviour mb, FieldInfo field);
//Action<MonoBehaviour, object> SetVariableType
}

public abstract class AbstractAutoAttribute : Attribute, IAutoAttribute
{
    private bool logErrorIfMissing = true;

    public AbstractAutoAttribute(bool getMadIfMissing = true)
    {
        this.logErrorIfMissing = getMadIfMissing;
    }

    public abstract bool Execute(MonoBehaviour mb, FieldInfo field);
}

public interface IAutoAttributeClass
{
}