using JetBrains.Annotations;
using RCGMaker.Runtime.Attributes;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace RCGMaker.Core
{
    //自動把parent的MonoBehaviour值設定到這個field
    [UsedImplicitly]
    public class SerializeReferenceParentValidateDrawer:OdinAttributeDrawer<SerializeReferenceParentValidateAttribute>
    {
        protected override void Initialize()
        {
            // base.Initialize();
            var mono = Property.SerializationRoot.ParentValues[0] as MonoBehaviour;
            if (mono == null)
            {
                throw new System.ArgumentNullException(nameof(Property.ParentValues));
            }
            Property.ValueEntry.WeakSmartValue = mono;
        }
        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);
        }
    }
}