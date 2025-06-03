using System.Collections.Generic;

namespace RCGMaker.Stat
{
    public class StatModifierData : DescriptableData
    {
        public List<StatModifierEntry> effectModifiers;
        public DescriptableData countProvider;
    }
}