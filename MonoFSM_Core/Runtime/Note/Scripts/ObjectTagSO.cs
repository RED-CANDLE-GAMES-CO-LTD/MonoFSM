using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ObjectTagSO", menuName = "ScriptableObjects/GamePlayDesign/GamePlayElement", order = 0)]
public class ObjectTagSO : ScriptableObject
{
    //TODO: localized string?
    [PreviewField]
    public Sprite icon;
    public Color tintColor = Color.white;
    public string prefix;
}

//GamePlayElement?