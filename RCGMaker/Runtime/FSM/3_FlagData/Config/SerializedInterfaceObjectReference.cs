using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class SerializedInterfaceObjectReference<TInterface, TObject> : ISerializationCallbackReceiver
	where TInterface : class
	where TObject : Object
{
	[HideReferenceObjectPicker]
	[ShowInInspector] protected TInterface m_value;

	[SerializeField] protected TObject m_object;

	public TObject Object => m_object;

	public TInterface Value => m_value;
	
	public SerializedInterfaceObjectReference() { }

	public SerializedInterfaceObjectReference(TInterface tInterface) => SetValue(tInterface);

	public void SetValue(TInterface newValue)
	{
		if (newValue is not TObject tObject)
		{
#if UNITY_EDITOR
			Debug.LogWarning($"Object {newValue} is not of type {typeof(TObject)}");
#endif
			return;
		}
		
		m_object = tObject;
		m_value = newValue;
	}

	public static implicit operator SerializedInterfaceObjectReference<TInterface, TObject>(TInterface tInterface) =>
		new(tInterface);

	public void OnBeforeSerialize() { }

	public void OnAfterDeserialize() => m_value = m_object as TInterface;
}

[Serializable]
public class SerializedInterfaceObjectReference<TInterface> : SerializedInterfaceObjectReference<TInterface, Object>
	where TInterface : class
{
	public SerializedInterfaceObjectReference(TInterface tInterface) : base(tInterface) { }
	
	public static implicit operator SerializedInterfaceObjectReference<TInterface>(TInterface tInterface) => new(tInterface);
}

[Serializable]
public class SerializedInterfaceMonoBehaviourReference<TInterface> : SerializedInterfaceObjectReference<TInterface, MonoBehaviour>
	where TInterface : class
{
	public SerializedInterfaceMonoBehaviourReference(TInterface tInterface) : base(tInterface) { }
	
	public static implicit operator SerializedInterfaceMonoBehaviourReference<TInterface>(TInterface tInterface) => new(tInterface);
}

[Serializable]
public class SerializedInterfaceScriptableObjectReference<TInterface> : SerializedInterfaceObjectReference<TInterface, ScriptableObject>
	where TInterface : class
{
	public SerializedInterfaceScriptableObjectReference(TInterface tInterface) : base(tInterface) { }
	
	public static implicit operator SerializedInterfaceScriptableObjectReference<TInterface>(TInterface tInterface) => new(tInterface);
}