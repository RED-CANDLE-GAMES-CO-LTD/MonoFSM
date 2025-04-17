using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "GamePlayTypeSO", menuName = "ScriptableObjects/GamePlayDesign/GamePlayTypeSO", order = -1)]
public class GamePlayTypeSO : ScriptableObject
{
    [ColorPalette("Tag")]
    public Color color;
    public List<GamePlayAttributeTypeSO> requiredAttributes;

    // [Button("Create Attribute")]
    // private void RemoveAttribute()
    // {
    //     // AssetDatabase.RemoveObjectFromAsset()
    // }
    // [Button("Create Attribute")]
    // private void AttachAttribute()
    // {

    //     var attributeSchema = ScriptableObject.CreateInstance<GamePlayAttributeTypeSO>();
    //     attributeSchema.name = "attribute";

    //     AssetDatabase.AddObjectToAsset(attributeSchema, this);
    //     AssetDatabase.SaveAssets();

    //     EditorUtility.SetDirty(this);
    //     EditorUtility.SetDirty(attributeSchema);
    // }

}



[System.Serializable]
public class AttributeEntry
{
    [HideInInspector, HideLabel]
    public GamePlayAttributeTypeSO type;
    //可以filter?
    // [AssetList(CustomFilterMethod = "IsTypeMatch")] //NOTE: 用value dropdown沒辦法？太多了？ 但如果是
    // [InlineEditor(InlineEditorModes.LargePreview)]
    [LabelText("@type.name")]
    // [InfoBox("Empty or Not Match", InfoMessageType.Error, "IsTagNotMatch")]
    [ValueDropdown("GetOptions")]
    public GamePlayAttributeSO attribute;

    // [ValueDropdown("GetOptions")]
    // public GamePlayAttributeSO attribute2;

    private IEnumerable<GamePlayAttributeSO> GetOptions()
    {
        return type.options;
    }

    // private bool IsTypeMatch(GamePlayAttributeSO obj)
    // {
    //     return obj.attributeType == type;
    // }


    // bool IsTagNotMatch()
    // {

    //     return attribute == null || attribute.attributeType != type;
    // }
}

