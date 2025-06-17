using System.Collections.Generic;

namespace MonoFSM.Stat
{
    public class StatModifierData : DescriptableData
    {
        public List<StatModifierEntry> effectModifiers;
        public DescriptableData countProvider;
    }
}