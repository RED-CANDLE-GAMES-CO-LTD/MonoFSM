using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _3_Script._0_RedCandleGamesUtilities.UICanvas.ActivateChecker
{
    public class DepthOneConditionActivateTarget : AbstractConditionActivateTarget
    {
        [Component] //沒用...
        [AutoChildren(DepthOneOnly = true)]
        [ShowInInspector]
        private AbstractConditionComp[] _depthOneConditions = Array.Empty<AbstractConditionComp>();

        protected override bool result => _depthOneConditions.IsAllValid();
        //應該用addlistener?
    }
}