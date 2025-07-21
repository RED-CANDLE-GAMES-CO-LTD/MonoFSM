using System;
using System.Collections.Generic;
using System.Reflection;
using MonoFSM.Core.Runtime.Action;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Runtime.Variable.Action
{
    [Obsolete("Use AssignValueAction instead")]
    public class SetBoolFieldOfGameFlagAction : AbstractStateAction
    {
        [InlineEditor] public GameFlagBase targetVariable;
        public bool TargetValue = true;
        public SetBoolType targetType;

        public enum SetBoolType
        {
            True,
            False,
            Toggle
        }

        private FieldInfo targetField;

        [ValueDropdown(nameof(GetAllFieldNames))]
        public string targetFieldName;

        private IEnumerable<string> GetAllFieldNames()
        {
            if (targetVariable == null) yield break;
            foreach (var field in targetVariable.GetType().GetFields())
            {
                if (field.FieldType != typeof(FlagFieldBool)) continue;
                yield return field.Name;
            }
        }

        protected override void OnActionExecuteImplement()
        {
            if (targetVariable == null)
            {
                Debug.LogWarning(
                    $"SetBoolFieldOfGameFlagDataAction: targetVariable.FinalData:{targetVariable.name} is null", this);
                return;
            }

            targetField = targetVariable.GetType().GetField(targetFieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (targetField == null)
            {
                Debug.LogError($"SetBoolFieldOfGameFlagDataAction: {targetFieldName} not found");
                return;
            }

            var flag = targetField.GetValue(targetVariable) as FlagFieldBool;
            if (flag == null)
            {
                Debug.LogError($"SetBoolFieldOfGameFlagDataAction: {targetFieldName} is not FlagFieldBool");
                return;
            }

            if (targetType == SetBoolType.Toggle)
                flag.CurrentValue = !flag.CurrentValue;
            else
                //FIXME: refactor: use switch
                flag.CurrentValue = TargetValue;
        }
    }
}