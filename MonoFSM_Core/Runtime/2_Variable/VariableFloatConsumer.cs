using UnityEngine;

using MonoFSM.Variable;

//使用 VariableFloat 的人要用這個
public class VariableFloatConsumer : AbstractVariableConsumer
{
    public VarFloat MonoVarSource => variableSource as VarFloat;
}

public abstract class AbstractVariableConsumer : MonoBehaviour
{
    public AbstractMonoVariable variableSource;
}