using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GamePlayAttributeTypeSO", menuName = "ScriptableObjects/GamePlayDesign/GamePlayAttributeTypeSO", order = 0)]
public class GamePlayAttributeTypeSO : ScriptableObject
{
    public List<GamePlayAttributeSO> options;
}