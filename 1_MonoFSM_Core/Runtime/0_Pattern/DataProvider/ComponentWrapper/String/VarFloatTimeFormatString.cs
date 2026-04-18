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

        public override string Value
        {
            get
            {
                var total = Mathf.Max(0f, _seconds.Value);
                var minutes = Mathf.FloorToInt(total / 60f);
                var secs = Mathf.FloorToInt(total % 60f);
                return $"{minutes:00}:{secs:00}";
            }
        }

        public override string Description => "Time: " + _seconds.Description;
        protected override string DescriptionTag => "Time";

        public string GetString() => Value;
    }
}
