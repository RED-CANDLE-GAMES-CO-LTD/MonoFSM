using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace MonoFSM.Core.Tests.Runtime.ComponentTest
{
    public class TestComp : MonoBehaviour
    {
        public GameObject _prefab;

        public IEnumerator LogActions_ShouldLogExpectedMessages_WhenActivated()
        {
            LogAssert.Expect(LogType.Log, "OnEnable");

            // _root.SetActive(true);
            yield return null;

            LogAssert.Expect(LogType.Log, "OnDisable");
            // _root.SetActive(false);
            yield return null;

            LogAssert.NoUnexpectedReceived();
        }
    }
}