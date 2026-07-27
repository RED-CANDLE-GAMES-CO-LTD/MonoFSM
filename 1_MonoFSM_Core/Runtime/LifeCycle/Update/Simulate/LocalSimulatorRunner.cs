using System.Collections.Generic;
using MonoFSM.Core.Attributes;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonoFSM.Core.Simulate
{
    public static class WorldSimulatorHelper
    {
        public static T[] GetComponents<T>(
            this Scene scene,
            bool includeInactive,
            out GameObject[] rootObjects
        )
            where T : Component
        {
            rootObjects = scene.GetRootGameObjects();

            var partialResult = new List<T>();
            var result = new List<T>();

            foreach (var go in rootObjects)
            {
                // depth-first, according to docs and verified by our tests
                go.GetComponentsInChildren(includeInactive: includeInactive, partialResult);
                // AddRange accepts IEnumerable, so there would be an alloc
                foreach (var comp in partialResult)
                {
                    result.Add(comp);
                }
            }

            return result.ToArray();
        }
    }

    [DefaultExecutionOrder(10000)] //確保在所有Update之後執行
    [RequireComponent(typeof(WorldUpdateSimulator))]
    public class LocalSimulatorRunner : MonoBehaviour, ISimulateRunner, ISceneSavingCallbackReceiver
    {
        //撈場上所有的MonoPoolObj？
        [PreviewInInspector] // [AutoChildren]
        [SerializeField]
        private MonoObj[] _allSceneMonoPoolObjs;

        [Auto]
        private WorldUpdateSimulator _world;

        private void Awake()
        {
            //FIXME: 可以cache
#if UNITY_EDITOR
            _allSceneMonoPoolObjs = gameObject.scene.GetComponents<MonoObj>(true, out _);
#endif
            //scene上的
            foreach (var sceneMonoPoolObj in _allSceneMonoPoolObjs)
            {
                _world.RegisterMonoObject(sceneMonoPoolObj);
                //單機沒有 ISimulateAuthorityProvider，ShouldSimulte 會落到 _shouldSimulateFlag
                //（預設 false）——不在這裡 push true 的話，scene 上的物件永遠不會被 Simulate。
                //spawn 出來的走 LocalSpawnManager 有 push，scene 物件原本漏了。
                sceneMonoPoolObj.AssignShouldSimulateForAllChildrenObj(true);
            }
        }

        private void Start() //timing hmm
        {
            //FIXME: 還是要player生出來才呼叫？
            _world.WorldInit();
            _world.WorldReset();
        }

        private void FixedUpdate()
        {
            _world.BeforeSimulate(Time.fixedTime, Time.fixedDeltaTime, Time.frameCount);
            _world.Simulate(WorldUpdateSimulator.DeltaTime);
        }

        //會需要Update嗎？
        private void Update()
        {
            // _world.Simulate(Time.deltaTime);
        }

        private void LateUpdate()
        {
            //FIXME: 沒測過
            float timeSinceFixedUpdate = Time.time - Time.fixedTime;
            float alpha = Mathf.Clamp01(timeSinceFixedUpdate / Time.fixedDeltaTime);
            _world.Render(timeSinceFixedUpdate, alpha); //放這？
            _world.AfterRender(); //放這？
            _world.AfterUpdate();
        }

        public void OnBeforeSceneSave()
        {
            _allSceneMonoPoolObjs = gameObject.scene.GetComponents<MonoObj>(true, out _);
        }
    }
}
