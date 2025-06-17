using System.Linq;
using MonoFSM.Core.Attributes;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

//FIXME: refactor
namespace MonoFSM.Core
{
    public interface IUpdatable
    {
        void MonoBindAwake();
        void UpdateEffect();
        void OnStop();
    }

    public interface IUpdateRunner //算時間，算回合，時間到了更新
    {
        void ResetCounter();
        void MonoBindAwake();
    }

    //接variable就好了？
    //FIXME: 這個是裝在哪裡？ BuffContainer? 如果 _updatables被清掉/關掉 應該不要執行？ 
    //動作遊戲用，照著時間decay
    public class RealtimeUpdateRunner : MonoBehaviour, IUpdateRunner
    {
        //一個buff會維持多久
        // public StatData LastForSeconds;

        //多久造成效果
        //ex: 1s，0.5f造成一次傷害
        [Header("多久發動一次效果")] public float UpdateInterval = 0.1f;
        public StatData UpdateIntervalStatData;

        private float UpdateIntervalValue => UpdateIntervalStatData ? UpdateIntervalStatData.Value : UpdateInterval;
        private float _intervalTimer;

        // private float LastForSecondsValue => LastForSeconds ? LastForSeconds.Value : lastForSecondsValue;
        private float LastForSecondsValue =>
            LastForSecondsStatData ? LastForSecondsStatData.Value : lastForSecondsValue;

        [Header("持續多久")] public float lastForSecondsValue = 0.5f;
        public StatData LastForSecondsStatData;
        [PreviewInInspector] private float _timer;

        [FormerlySerializedAs("_timerMonoVariableFloat")] [FormerlySerializedAs("_timerVariableFloat")]
        public VarFloat _timerMonoVarFloat;

        [PreviewInInspector] [AutoChildren()] private IUpdatable[] _updatables;

        [ShowInInspector] //可以用attribute讓interface變成可以看嗎？
        // private Component[] _updatableComponents =>
        //     _updatables.Select(a => a as Component).ToArray();

        //如果owner已經有同個BuffModule，就不要再加了
        //要登記...BuffContainer        
        public void ResetCounter()
        {
            _timer = LastForSecondsValue;
        }

        private void OnEnable()
        {
            _timer = LastForSecondsValue;
            _intervalTimer = UpdateIntervalValue;
        }

        public void MonoBindAwake()
        {
            foreach (var updatable in _updatables) updatable.MonoBindAwake();
        }

        //自己跑Update，還是要讓外部的人來call
        //動作遊戲可以自己maintain, 回合制應該讓外部的runner來做
        // Buff Runner Type...

        public UnityEvent OnStop;

        private void Update()
        {
            _intervalTimer -= Time.deltaTime;
            if (_intervalTimer <= 0)
            {
                _intervalTimer += UpdateIntervalValue;
                // foreach (var updatable in _updatables) updatable.UpdateEffect();
                foreach (var updatable in _updatables) updatable.UpdateEffect();
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                // _updatable.Stop();
                // gameObject.SetActive(false);
                OnStop.Invoke();
                // Debug.Log("RealtimeUpdate Runner Stop");
                foreach (var updatable in _updatables) updatable.OnStop();
                //至少先disable就不會有效果了
                //[]: Pool return..? 應該讓buff module自己return就好
                return;
            }
        }
    }
}