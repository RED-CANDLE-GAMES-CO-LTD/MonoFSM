using System.Collections.Generic;
using System.Linq;
using MonoFSM.Core.Variable;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Core.Formula
{
    public class VarListSourceProvider : MonoBehaviour, IMonoDescriptableListProvider
    {
        [DropDownRef] [SerializeField] [Required]
        private VarListEntity _sourceList;

        public IEnumerable<MonoEntity> GetDescriptables()
        {
            if (_sourceList == null)
            {
                return Enumerable.Empty<MonoEntity>();
            }
            // VarList<MonoDescriptable> stores items as MonoDescriptable.
            return _sourceList.GetItems();
        }
    }
}
