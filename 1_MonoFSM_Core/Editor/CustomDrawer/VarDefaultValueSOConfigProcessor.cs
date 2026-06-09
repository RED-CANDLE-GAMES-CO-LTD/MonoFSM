using System;
using System.Collections.Generic;
using System.Reflection;
using _1_MonoFSM_Core.Runtime.Attributes;
using JetBrains.Annotations;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace _1_MonoFSM_Core.Editor.CustomDrawer
{
    /// <summary>
    ///     GenericObjectVariable 的 _defaultValue 欄位：當 TValueType 是 ScriptableObject
    ///     （如 VarGameData 的 GameData）時，移除 base 硬掛的 DropDownRef，改套 SOConfig，
    ///     讓它擁有完整的建立體驗（路徑下拉、子資料夾、SOTypeDropdown 選具體子類、一鍵 Create）。
    ///     子資料夾用 TValueType 型別名；Component 型則維持原本的 DropDownRef。
    /// </summary>
    [UsedImplicitly]
    public class VarDefaultValueSOConfigProcessor : OdinAttributeProcessor<AbstractMonoVariable>
    {
        private const string DefaultValueFieldName = "_defaultValue";

        public override void ProcessChildMemberAttributes(
            InspectorProperty parentProperty,
            MemberInfo member,
            List<Attribute> attributes
        )
        {
            if (member is not FieldInfo fieldInfo)
                return;
            if (fieldInfo.Name != DefaultValueFieldName)
                return;
            //只接管 ScriptableObject 型，Component 型維持 DropDownRef
            if (!typeof(ScriptableObject).IsAssignableFrom(fieldInfo.FieldType))
                return;

            //DropDownRef 的 DrawerPriority 較高會蓋掉 SOConfig 的建立 UI，必須移除
            attributes.RemoveAll(a => a is DropDownRefAttribute);

            if (attributes.Exists(a => a is SOConfigAttribute))
                return;

            //子資料夾用型別名（如 GameData → 10_Scriptables/GameData/）；
            //useVarTagRestrictType 讓建立時用 VarTag 限定的具體子類
            attributes.Add(new SOConfigAttribute(fieldInfo.FieldType.Name, useVarTagRestrictType: true));
            //IncludeMyAttributes 不會自動帶上 SOTypeDropdown，需手動加（同 AbstractSoConfigAttributeProcessor）
            attributes.Add(new SOTypeDropdownAttribute());
        }
    }
}
