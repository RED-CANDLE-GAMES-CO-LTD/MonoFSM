using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace MonoFSM.Core.AI
{
    public static class ClassTypeManifestGenerator
    {
        /// <summary>
        /// Gets the MonoFSM package path dynamically
        /// </summary>
        /// <returns>The absolute path to the MonoFSM package, or null if not found</returns>
        private static string GetMonoFSMPackagePath()
        {
            // Method 1: Try to find by looking for the current script's path
            string currentScriptPath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
            if (!string.IsNullOrEmpty(currentScriptPath))
            {
                // Convert to Unity-style path
                currentScriptPath = currentScriptPath.Replace('\\', '/');

                // Look for MonoFSM in the path
                int monoFsmIndex = currentScriptPath.IndexOf("MonoFSM", StringComparison.OrdinalIgnoreCase);
                if (monoFsmIndex >= 0)
                {
                    // Find the root MonoFSM directory
                    string beforeMonoFsm = currentScriptPath.Substring(0, monoFsmIndex);
                    string monoFsmRoot = beforeMonoFsm + "MonoFSM/1_MonoFSM_Core";
                    if (Directory.Exists(monoFsmRoot))
                    {
                        return monoFsmRoot;
                    }
                }
            }

            // Method 2: Try to find using PackageManager if it's a package
            try
            {
                var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
                var monoFsmPackage = packages.FirstOrDefault(p => p.name.Contains("MonoFSM") || p.displayName.Contains("MonoFSM"));
                if (monoFsmPackage != null)
                {
                    return monoFsmPackage.assetPath;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not query PackageManager: {ex.Message}");
            }

            // Method 3: Search in common locations
            var searchPaths = new[]
            {
                Path.Combine(Directory.GetParent(Application.dataPath).FullName, "MonoFSM/1_MonoFSM_Core"),
                Path.Combine(Directory.GetParent(Application.dataPath).FullName, "submodules/MonoFSM/1_MonoFSM_Core"),
                Path.Combine(Application.dataPath, "MonoFSM/1_MonoFSM_Core"),
                Path.Combine(Application.dataPath, "../MonoFSM/1_MonoFSM_Core")
            };

            foreach (string searchPath in searchPaths)
            {
                if (Directory.Exists(searchPath))
                {
                    return searchPath;
                }
            }

            return null;
        }
        [MenuItem("Tools/MonoFSM/Open Persistent Data Folder")]
        private static void OpenPersistentDataFolder()
        {
            var persistentDataPath = Application.persistentDataPath;
            if (Directory.Exists(persistentDataPath))
                UnityEditor.EditorUtility.RevealInFinder(persistentDataPath);
            else
                Debug.LogError($"Persistent data folder does not exist: {persistentDataPath}");
        }

        [MenuItem("Tools/MonoFSM/Open Project Folder")]
        private static void OpenDataFolder()
        {
            var dataPath = Application.dataPath;
            if (Directory.Exists(dataPath))
                UnityEditor.EditorUtility.RevealInFinder(dataPath);
            else
                Debug.LogError($"Data folder does not exist: {dataPath}");
        }

        //Application.temporaryCachePath
        [MenuItem("Tools/MonoFSM/Open Temporary Cache Folder")]
        private static void OpenTemporaryCacheFolder()
        {
            var tempCachePath = Application.temporaryCachePath;
            if (Directory.Exists(tempCachePath))
                UnityEditor.EditorUtility.RevealInFinder(tempCachePath);
            else
                Debug.LogError($"Temporary cache folder does not exist: {tempCachePath}");
        }

        [MenuItem("Tools/MonoFSM/Generate Class Type Manifest")]
        private static void Generate()
        {
            // Method 1: Get package path dynamically using PackageInfo
            string packagePath = GetMonoFSMPackagePath();
            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogError("Could not find MonoFSM package path");
                return;
            }

            var filePath = Path.Combine(packagePath, ".AI/MonoFSM_Core_Runtime_manifest.json");
            var fullPath = filePath;
            // build manifest data
            var manifest = new Dictionary<string, object>
            {
                ["manifestVersion"] = "1.0.0",
                ["description"] = "MonoFSM API and file manifest for tooling and automation.",
                ["intendedFor"] = new[] { "AI", "IDE", "DocsGen" },
                ["customData"] = new Dictionary<string, object>()
            };
            var typesList = new List<Dictionary<string, object>>();
            var interfaceTypes = new HashSet<Type>();
            // scan assemblies for runtime types
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var asmName = assembly.GetName().Name;
                if (!asmName.Contains("MonoFSM.Core.Runtime"))
                    continue;
                foreach (var type in assembly.GetTypes())
                {
                    if (type.BaseType == null || (!type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsSubclassOf(typeof(ScriptableObject))))
                        continue;

                    var typeEntry = new Dictionary<string, object>
                    {
                        ["class"] = type.Name
                    };
                    if (!string.IsNullOrEmpty(type.Namespace))
                        typeEntry["namespace"] = type.Namespace;
                    if (type.BaseType != null)
                        typeEntry["base"] = type.BaseType.Name;
                    var implIfaces = type.GetInterfaces();
                    var interfaces = implIfaces.Select(i => i.Name).ToArray();
                    if (interfaces.Length > 0)
                    {
                        typeEntry["interfaces"] = interfaces;
                        foreach (var iface in implIfaces)
                            interfaceTypes.Add(iface);
                    }
                    if (type.IsSubclassOf(typeof(MonoBehaviour)))
                        typeEntry["isComponent"] = true;
                    if (type.IsSubclassOf(typeof(ScriptableObject)))
                        typeEntry["isScriptableObject"] = true;
                    // extract auto references
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var autoRefs = new List<Dictionary<string, string>>();
                    var mcpProps = new List<Dictionary<string, string>>();
                    foreach (var f in fields)
                    {
                        var relevantAttr = f.GetCustomAttributes(false)
                            .FirstOrDefault(a =>
                            {
                                var name = a.GetType().Name;
                                return name == "AutoAttribute" || name == "AutoParentAttribute" || name == "AutoChildrenAttribute" || name == "MCPExtractableAttribute";
                            });
                        if (relevantAttr != null)
                        {
                            var attrName = relevantAttr.GetType().Name.Replace("Attribute", "");
                            if (attrName == "MCPExtractable")
                            {
                                mcpProps.Add(new Dictionary<string, string>
                                {
                                    ["type"] = f.FieldType.Name,
                                    ["name"] = f.Name
                                });
                            }
                            else
                            {
                                autoRefs.Add(new Dictionary<string, string>
                                {
                                    ["attribute"] = attrName,
                                    ["type"] = f.FieldType.Name,
                                    ["name"] = f.Name
                                });
                            }
                        }
                    }
                    if (autoRefs.Count > 0)
                        typeEntry["autoReferences"] = autoRefs;
                    if (mcpProps.Count > 0)
                        typeEntry["properties"] = mcpProps;
                    typesList.Add(typeEntry);
                }
            }
            // build global interface definitions
            var globalInterfaceDefs = new Dictionary<string, object>();
            foreach (var iface in interfaceTypes)
            {
                var methods = iface.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                   .Select(m => m.Name).ToArray();
                var properties = iface.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                      .Select(p => p.Name).ToArray();
                var defEntry = new Dictionary<string, object>();
                if (methods.Length > 0) defEntry["methods"] = methods;
                if (properties.Length > 0) defEntry["properties"] = properties;
                globalInterfaceDefs[iface.Name] = defEntry;
            }
            manifest["interfaceDefinitions"] = globalInterfaceDefs;
            manifest["types"] = typesList;
            // serialize and write to file
            var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
            Debug.Log($"Generated MonoFSM Core Runtime manifest at {fullPath}");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, json);
            AssetDatabase.Refresh();
        }
    }
}