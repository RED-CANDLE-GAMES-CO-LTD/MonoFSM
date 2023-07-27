using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//使用 VariableFloat 的人要用這個
public class VariableFloatConsumer : AbstractVariableConsumer
{
    public VariableFloat VariableSource => variableSource as VariableFloat;
    // public float Value
    // {
    //     get
    //     {
    //         return VariableSource.Value;
    //     }
    //     set
    //     {
    //         VariableSource.Value = value;
    //     }
    // }
}
public abstract class AbstractVariableConsumer : MonoBehaviour
{
    public AbstractVariable variableSource;
}