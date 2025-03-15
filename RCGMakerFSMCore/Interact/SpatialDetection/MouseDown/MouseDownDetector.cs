using System;
using RCGMaker.Core.Detection;

namespace RCGMaker.Runtime.Interact.SpatialDetection
{
    public class IConditionProvider
    {
    }

    public class MouseDownDetector : AbstractDetector
    {
        public static MouseDownDetector Instance;

        // //放在dealer層？
        // [AutoChildren] AbstractConditionComp[] conditions;
        // public bool IsValid => conditions.IsAllValid();

        protected override void SetLayerOverride()
        {
        }

        private void Awake()
        {
            MouseDownDetector.Instance = this;
        }
    }
}