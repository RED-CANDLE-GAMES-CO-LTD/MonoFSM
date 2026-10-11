using System;
using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Runtime.Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 將 VarFloat 的值（秒）轉換成 mm:ss 格式字串
    /// </summary>
    public class VarFloatTimeFormatString : AbstractValueSource<string>, IStringProvider
    {
        [InlineProperty] [HideLabel] [SerializeField]
        private VarFloatWrapper _seconds = new();

        //TextValueBinder 每幀都會 Get 來比對，秒數沒變就回同一個字串，不然每幀都產新字串（GC）
        [NonSerialized] private int _cachedTotalSecs = -1;
        [NonSerialized] private string _cached;

        public override string Value
        {
            get
            {
                var totalSecs = Mathf.FloorToInt(Mathf.Max(0f, _seconds.Value));
                if (totalSecs == _cachedTotalSecs && _cached != null)
                    return _cached;
                _cachedTotalSecs = totalSecs;
                _cached = $"{totalSecs / 60:00}:{totalSecs % 60:00}";
                return _cached;
            }
        }

        public override string Description => "Time: " + _seconds.Description;
        protected override string DescriptionTag => "Time";

        public string GetString() => Value;
    }
}
