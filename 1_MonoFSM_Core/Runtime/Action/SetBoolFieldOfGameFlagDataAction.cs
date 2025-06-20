using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core.Runtime.Action;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace MonoFSM.Variable
{
    //放棄這個ㄇ？要拿來做啥？
    // public class SetBoolFieldOfVariableAction : AbstractStateAction
    // {
    //     [FormerlySerializedAs("targetVariable")]
    //     public AbstractMonoVariable _targetMonoVariable;
    //
    //     public bool TargetValue = true;
    //     public SetBoolType targetType;
    //
    //     public enum SetBoolType
    //     {
    //         True,
    //         False,
    //         Toggle
    //     }
    //
    //     private FieldInfo targetField;
    //
    //     [ValueDropdown(nameof(GetAllFieldNames))]
    //     public string targetFieldName;
    //
    //     private IEnumerable<string> GetAllFieldNames()
    //     {
    //         if (_targetMonoVariable == null) yield break;
    //         
    //         foreach (var field in _targetMonoVariable.FinalDataType.GetFields())
    //         {
    //             if (field.FieldType != typeof(FlagFieldBool)) continue;
    //             yield return field.Name;
    //         }
    //     }
    //
    //     protected override void OnStateEnterImplement()
    //     {
    //         if (_targetMonoVariable == null)
    //         {
    //             Debug.LogError("SetBoolFieldOfGameFlagDataAction: targetVariable is null", this);
    //             return;
    //         }
    //
    //         if (_targetMonoVariable.FinalData == null)
    //         {
    //             Debug.LogWarning(
    //                 $"SetBoolFieldOfGameFlagDataAction: targetVariable.FinalData:{_targetMonoVariable.name} is null",
    //                 this);
    //             return;
    //         }
    //
    //         targetField = _targetMonoVariable.FinalDataType.GetField(targetFieldName,
    //             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //         if (targetField == null)
    //         {
    //             Debug.LogError($"SetBoolFieldOfGameFlagDataAction: {targetFieldName} not found");
    //             return;
    //         }
    //
    //         var flag = targetField.GetValue(_targetMonoVariable.FinalData) as FlagFieldBool;
    //         if (flag == null)
    //         {
    //             Debug.LogError($"SetBoolFieldOfGameFlagDataAction: {targetFieldName} is not FlagFieldBool");
    //             return;
    //         }
    //
    //         if (targetType == SetBoolType.Toggle)
    //             flag.CurrentValue = !flag.CurrentValue;
    //         else
    //             //FIXME: refactor: use switch
    //             flag.CurrentValue = TargetValue;
    //     }
    // }
}