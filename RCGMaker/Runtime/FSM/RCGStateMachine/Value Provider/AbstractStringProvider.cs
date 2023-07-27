using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
public abstract class AbstractStringProvider : MonoBehaviour, IStringProvider
{
    public abstract string StringValue
    {
        get;
    }
}

internal interface IStringProvider
{
    string StringValue { get; }
}