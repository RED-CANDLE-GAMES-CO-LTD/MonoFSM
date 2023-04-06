using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum TestMode
{
    Undefined = -1,
    PlayerMode,
    //DeveloperStaticTest,
    DeveloperDynamicTest,

    BetaTest,
}

//TODO: 改名 RCGBuildMode
[CreateAssetMenu(fileName = "TestModeGameFlag", menuName = "GameFlag/TestModeGameFlag", order = 1)]
public class TestModeGameFlag : GameFlagBase
{

    //最單純所有的flag都直接改成on
    public TestMode mode = TestMode.DeveloperDynamicTest;
    public bool isDemo = false;
    //TODO: 把ability flag放到gameflagmanager的一個list?
    // public bool AllAbilityOn;

    //TODO: 還有甚麼可能的有關flag想綁在一起嗎?

}
