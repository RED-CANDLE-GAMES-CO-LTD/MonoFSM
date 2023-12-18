using System.Collections;
using System.Collections.Generic;
using RCGMaker.Core;
using RCGMaker.Core.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

//先指到一個prefab裡的某個節點，之後再灌給 InstanceReference
public class InstanceConsumer : MonoBehaviour
{
   [InlineEditor]
   [Required]
   [FormerlySerializedAs("instanceReferenceData")] public InstanceReferenceData InstanceReferenceDataData;
   [PreviewInInspector]
   Object _cachedReference;
   public T CachedInstance<T>() where T : Component
   {
      if (_cachedReference == null)
      {
         _cachedReference = InstanceReferenceDataData.instance.GetComponent<T>();
      }

      return (T) _cachedReference;
   }
}
