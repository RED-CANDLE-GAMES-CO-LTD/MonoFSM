using System;
using System.Collections.Generic;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace _1_MonoFSM_Core.Editor.CustomDrawer
{
    //FIXME: 好像沒有比較好？用VarWrapperDrawer?
    // public class AbstractVarWrapperAttributeProcessor : OdinAttributeProcessor<AbstractVarWrapper>
    // {
    //     public override void ProcessSelfAttributes(
    //         InspectorProperty property,
    //         List<Attribute> attributes
    //     )
    //     {
    //         // var propertyType = property.ValueEntry.TypeOfValue;
    //         // if (attributes.Exists(x => x is InlinePropertyAttribute))
    //         //     return;
    //         // attributes.Add(new InlinePropertyAttribute());
    //         // attributes.Add(new HideLabelAttribute());
    //         // attributes.Add(new TitleAttribute(property.NiceName));
    //     }
    // }
}
