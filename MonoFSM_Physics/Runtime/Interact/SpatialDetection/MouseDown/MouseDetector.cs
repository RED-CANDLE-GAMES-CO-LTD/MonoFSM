using System;
using RCGMaker.Core.Detection;

namespace RCGMaker.Runtime.Interact.SpatialDetection
{
    public class IConditionProvider
    {
    }

    public class MouseDetector : AbstractDetector
    {
        static MouseDetector _instance;

        public static MouseDetector Instance => _instance;
        // //放在dealer層？
        // [AutoChildren] AbstractConditionComp[] conditions;
        // public bool IsValid => conditions.IsAllValid();

        protected override void SetLayerOverride()
        {
        }

        private void Awake()
        {
            _instance = this;
        }
    }
}