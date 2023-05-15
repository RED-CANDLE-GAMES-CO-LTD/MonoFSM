using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RCGMaker.Core
{
    public interface IUpdatable
    {
        void UpdateEffect();
    }

    public interface IUpdateRunner //算時間，算回合，時間到了更新
    {
        void Reset();
    }

    //動作遊戲用，照著時間decay
    public class RealtimeUpdateRunner : MonoBehaviour, IUpdateRunner
    {
        //一個buff會維持多久
        public StatData LastForSeconds;

        //多久造成效果
        //ex: 1s，0.5f造成一次傷害
        public float interval = 0.1f;
        private float _intervalTimer;
        private float LastForSecondsValue => LastForSeconds ? LastForSeconds.Value : lastForSecondsValue;
        public float lastForSecondsValue;
        private float _timer;

        private IUpdatable[] _updatables;

        [ShowInInspector] //可以用attribute讓interface變成可以看嗎？
        private Component[] _updatableComponents =>
            _updatables.Select(a => a as Component).ToArray();

        //如果owner已經有同個BuffModule，就不要再加了
        //要登記...BuffContainer        


        public void Reset()
        {
            _timer = LastForSecondsValue;
        }

        private void OnEnable()
        {
            _timer = LastForSecondsValue;
        }


        //自己跑Update，還是要讓外部的人來call
        //動作遊戲可以自己maintain, 回合制應該讓外部的runner來做
        // Buff Runner Type...

        private void Update()
        {
            _intervalTimer -= Time.deltaTime;
            if (_intervalTimer <= 0)
            {
                _intervalTimer = interval + _intervalTimer;
                foreach (var updatable in _updatables) updatable.UpdateEffect();
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                gameObject.SetActive(false);
                //至少先disable就不會有效果了
                //[]: Pool return..? 應該讓buff module自己return就好
                return;
            }
        }
    }
}