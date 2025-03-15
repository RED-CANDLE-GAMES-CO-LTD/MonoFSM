#if UNITY_EDITOR

using System;
using UnityEditor;

namespace Auto.Utils{

	[InitializeOnLoad]
	public class ScriptExecutionManagerEditor : UnityEditor.AssetModificationProcessor
	{
		static ScriptExecutionManagerEditor()
		{
			foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
			{
				if (monoScript.GetClass() != null)
				{
					var scriptTimingAtt = Attribute.GetCustomAttribute(monoScript.GetClass(), typeof(ScriptTiming)) as ScriptTiming;
					if(scriptTimingAtt != null)
					{
						ProcessScriptTimingAttribute(monoScript, scriptTimingAtt);
					}
				}
			}

			EditorApplication.UnlockReloadAssemblies();
		}

		private static void ProcessScriptTimingAttribute(MonoScript monoScript, ScriptTiming scriptTiming)
		{
			var currentTiming = MonoImporter.GetExecutionOrder(monoScript);
			var newTiming = scriptTiming.timing_offset;
			if (currentTiming != newTiming)
				MonoImporter.SetExecutionOrder(monoScript, newTiming);
		}
		
	}
}

#endif