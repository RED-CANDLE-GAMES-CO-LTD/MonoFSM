namespace Dreamteck.Editor
{
    using UnityEngine;
    using UnityEditor;

    public static class ScriptingDefineUtility 
    {
        public static void Add(string define, BuildTargetGroup target, bool log = false)
        {
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            if (definesString.Contains(define)) return;
            string[] allDefines = definesString.Split(';');
            ArrayUtility.Add(ref allDefines, define);
            definesString = string.Join(";", allDefines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, definesString);
            Debug.Log("Added \"" + define + "\" from " + EditorUserBuildSettings.selectedBuildTargetGroup + " Scripting define in Player Settings");
        }

        public static void Remove(string define, BuildTargetGroup target, bool log = false)
        {
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            if (!definesString.Contains(define)) return;
            string[] allDefines = definesString.Split(';');
            ArrayUtility.Remove(ref allDefines, define);
            definesString = string.Join(";", allDefines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, definesString);
            Debug.Log("Removed \""+ define + "\" from " + EditorUserBuildSettings.selectedBuildTargetGroup + " Scripting define in Player Settings");
        }
    }
}
