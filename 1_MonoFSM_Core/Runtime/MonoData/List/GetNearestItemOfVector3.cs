using MonoFSM.Foundation;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Mono;
using MonoFSM.Variable;
using UnityEngine;

namespace MonoFSM.Core.Variable.Providers
{
    public class GetNearestItemOfVector3 : AbstractEntitySource
    {
        [Tooltip("量距離的來源位置，優先於 _sourceTransform")]
        public VarVector3 _sourcePosition;

        [Tooltip("量距離的來源 transform（會移動的發射點用這個），_sourcePosition 沒設時才看；兩個都沒設就用自己的 transform")]
        public Transform _sourceTransform;

        public VarListEntity _varList;

        private Vector3 SourcePosition
        {
            get
            {
                if (_sourcePosition != null)
                    return _sourcePosition.Value;
                if (_sourceTransform != null)
                    return _sourceTransform.position;
                return transform.position;
            }
        }

        MonoEntity FindNearestEntity()
        {
            if (_varList == null)
                return null;
            var list = _varList.GetList();
            if (list == null || list.Count == 0)
                return null;
            MonoEntity nearest = null;
            var nearestSqrDist = float.MaxValue;
            var sourcePos = SourcePosition;
            foreach (var item in list)
            {
                if (item == null)
                    continue;
                //只比大小，開根號沒意義
                var sqrDist = (sourcePos - item.transform.position).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = item;
                }
            }

            return nearest;
        }

        public override MonoEntity Value => FindNearestEntity();
        public override MonoEntityTag entityTag { get; }
    }
}
