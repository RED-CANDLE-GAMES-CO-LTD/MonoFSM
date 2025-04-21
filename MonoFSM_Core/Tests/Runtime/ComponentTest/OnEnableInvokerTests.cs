using System.Collections;
using NUnit.Framework;
using RCGMaker.Core.Module;
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
    private GameObject _root;
    private OnEnableInvoker _invoker;

//     OnEnableInvoker:3960855537088388994(C:OnEnableInvoker #2269622561476295443-0)
//     ..GO:"OnEnableNode" #7235220239025890921-0(C:OnEnableNode #2653871638592893292-0)
//     ....GO:"[Action] LogAction" #8718455501359711145-0(C:LogAction #5392556174335500925-0{_logMessage:OnEnable})
//     ..GO:"OnDisableNode" #6373827583573887965-0(C:OnDisableNode #812966048265802247-0)
//     ....GO:"[Action] LogAction" #5465047916926780095-0(C:LogAction #6779099019067522787-0{_logMessage:OnDisable})
//沒有介面拗痛苦...
    private const string prefabPath = "Packages/com.rcg.fsm/0_MonoFSM_Example_Module/OnEnableInvoker.prefab";
    // public GameObject _prefab;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.IsNotNull(prefab, $"Test prefab not found");
        // Create a root object and add the component under test.
        _root = Object.Instantiate(prefab);
        _invoker = _root.GetComponent<OnEnableInvoker>();
        AutoAttributeManager.AutoReferenceAllChildren(_root);
        // Allow one frame for Awake/OnEnable to execute.
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(_root);
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
    private static T? GetPrivateField<T>(object obj, string fieldName) where T : class?
    {
        var fieldInfo = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return fieldInfo?.GetValue(obj) as T;
    }
}