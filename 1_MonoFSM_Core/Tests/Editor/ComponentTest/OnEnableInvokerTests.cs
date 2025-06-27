using System.Collections;
using NUnit.Framework;
using MonoFSM.Core.Module;
using UnityEngine;
using UnityEngine.TestTools;

// NOTE: Replace the following namespace with the actual one used by your project if different.
// If the classes are in the global namespace, you can delete this using statement.
// using RCGMaker.Core.Module;

/// <summary>
/// PlayMode tests for <see cref="OnEnableInvoker"/> component.
/// </summary>
/// <remarks>
/// This test validates that the Auto‑wiring performed by the <c>AutoChildren</c> attribute
/// successfully assigns references to child <see cref="OnEnableNode"/> and <see cref="OnDisableNode"/>
/// components at runtime.
///
/// Because <c>Awake</c>/<c>OnEnable</c> are executed during the first frame, the test
/// initialises the hierarchy and then yields one frame to allow Unity&#x27;s internal life‑cycle
/// to run before performing assertions.
/// </remarks>
public class OnEnableInvokerTests
{
    private const string scenePath = "Packages/com.monofsm.core/1_MonoFSM_Core/Tests/Editor/ComponentTest/OnEnableInvokerTests.unity";
    private GameObject _root;
    private OnEnableInvoker _invoker;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // 開啟指定場景
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        yield return null;
        // 於場景中尋找 OnEnableInvoker
        _invoker = Object.FindFirstObjectByType<OnEnableInvoker>();
        Assert.IsNotNull(_invoker, "OnEnableInvoker component should be present in scene.");
        _root = _invoker.gameObject;
        // 若有自動綁定需求可於此呼叫
        AutoAttributeManager.AutoReferenceAllChildren(_root);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // 關閉場景或清理（可視需求保留）
        yield return null;
    }

    [UnityTest]
    public IEnumerator AutoChildrenAssignments_ShouldNotBeNull_AfterAwake()
    {
        Debug.Log(_root.name);
        Assert.IsNotNull(_invoker, "OnEnableInvoker component should be present.");

        // Using reflection to access the private readonly fields populated by AutoChildren.
        var onEnableNode = GetPrivateField<OnEnableNode>(_invoker, "_onEnableNode");
        var onDisableNode = GetPrivateField<OnDisableNode>(_invoker, "_onDisableNode");

        Assert.IsNotNull(onEnableNode,
            "_onEnableNode reference was not assigned – make sure a child OnEnableNode exists.");
        Assert.IsNotNull(onDisableNode,
            "_onDisableNode reference was not assigned – make sure a child OnDisableNode exists.");

        //關著
        Assert.IsFalse(_root.gameObject.activeInHierarchy, "Root node should be disabled.");
        Assert.IsTrue(onEnableNode.enabled && onDisableNode.enabled, "Child nodes should be enabled.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator LogActions_ShouldLogExpectedMessages_WhenActivated()
    {
        LogAssert.Expect(LogType.Log, "OnEnable");

        _root.SetActive(true);
        yield return null;

        LogAssert.Expect(LogType.Log, "OnDisable");
        _root.SetActive(false);
        yield return null;

        LogAssert.NoUnexpectedReceived();
    }

    // Helper method for accessing non‑public fields via reflection.
    private static T GetPrivateField<T>(object obj, string fieldName) where T : class
    {
        var fieldInfo = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return fieldInfo?.GetValue(obj) as T;
    }
}