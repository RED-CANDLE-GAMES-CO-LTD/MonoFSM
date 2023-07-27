using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum TestMode
{
    Undefined = -1,
    Production,
    //DeveloperStaticTest,
    EditorDevelopment
    //BetaTest,
    
}

//[]: 之後刪掉，用DebugSetting看就好
[CreateAssetMenu(fileName = "TestModeGameFlag", menuName = "GameFlag/TestModeGameFlag", order = 1)]
public class TestModeGameFlag : ScriptableObjectSingleton<TestModeGameFlag>
{

    //最單純所有的flag都直接改成on
    public TestMode mode = TestMode.EditorDevelopment;
    public bool isDemo = false;
    //TODO: 把ability flag放到gameflagmanager的一個list?
    // public bool AllAbilityOn;

    //TODO: 還有甚麼可能的有關flag想綁在一起嗎?

}
