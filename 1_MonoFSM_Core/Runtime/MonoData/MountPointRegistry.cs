using Sirenix.OdinInspector;
using UnityEngine;

namespace _1_MonoFSM_Core.Runtime.MonoData
{
    /// <summary>
    /// Entity 上 mount point 的穩定編號表。
    /// 連線同步時 Transform 無法上網路，改同步這裡的 index，各端用 index 查回 Transform。
    /// 清單順序就是網路協定的一部分：只能往後加，不要重排或刪除中間項。
    /// </summary>
    public class MountPointRegistry : MonoBehaviour
    {
        public const int InvalidIndex = -1;

        [InfoBox("清單順序會被連線同步當作 index 使用：只能往後加，不要重排或刪除中間項")]
        [SerializeField] private Transform[] _mountPoints;

        public int IndexOf(Transform mountPoint)
        {
            if (mountPoint == null || _mountPoints == null) return InvalidIndex;
            for (var i = 0; i < _mountPoints.Length; i++)
                if (_mountPoints[i] == mountPoint)
                    return i;
            return InvalidIndex;
        }

        public Transform Get(int index)
        {
            if (_mountPoints == null || index < 0 || index >= _mountPoints.Length) return null;
            return _mountPoints[index];
        }
    }
}
