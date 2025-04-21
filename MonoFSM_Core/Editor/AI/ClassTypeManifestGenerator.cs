using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace MonoFSM_Core.Editor.AI
{
    public static class ClassTypeManifestGenerator
    {
        [MenuItem("Tools/Generate Class Type Manifest")]
        private static void Generate()
        {
            var filePath = "submodules/MonoFSM/MonoFSM_Core/.AI/MonoFSM_Core_Runtime_manifest.json";
            // calculate absolute path to project root
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fullPath = Path.Combine(projectRoot, filePath);
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
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, json);
            AssetDatabase.Refresh();
        }
    }
}