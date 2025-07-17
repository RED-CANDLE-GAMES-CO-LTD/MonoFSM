using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Variable.FieldReference
{
    /// <summary>
    /// 自定義的 FormerlyNamedAs 屬性，可以應用於 Class、Property、Field 等
    /// 支援多層級的名稱追踪，比 Unity 的 FormerlySerializedAs 更強大
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, 
                    AllowMultiple = true, Inherited = false)]
    public class FormerlyNamedAsAttribute : Attribute
    {
        /// <summary>
        /// 之前的名稱
        /// </summary>
        public string FormerName { get; }

        /// <summary>
        /// 重命名的版本或時間戳（可選，用於追踪）
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 說明重命名的原因（可選）
        /// </summary>
        public string Reason { get; set; }

        public FormerlyNamedAsAttribute(string formerName)
        {
            FormerName = formerName ?? throw new ArgumentNullException(nameof(formerName));
        }

        public FormerlyNamedAsAttribute(string formerName, string version) : this(formerName)
        {
            Version = version;
        }

        public FormerlyNamedAsAttribute(string formerName, string version, string reason) : this(formerName, version)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// 指定型別的完整名稱歷史，支援 namespace 和 class 名稱變更
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, 
                    AllowMultiple = true, Inherited = false)]
    public class FormerlyFullNameAttribute : Attribute
    {
        /// <summary>
        /// 之前的完整名稱（包含 namespace）
        /// </summary>
        public string FormerFullName { get; }

        /// <summary>
        /// 之前的 Assembly 名稱（如果有變更）
        /// </summary>
        public string FormerAssemblyName { get; set; }

        /// <summary>
        /// 版本資訊
        /// </summary>
        public string Version { get; set; }

        public FormerlyFullNameAttribute(string formerFullName)
        {
            FormerFullName = formerFullName ?? throw new ArgumentNullException(nameof(formerFullName));
        }

        public FormerlyFullNameAttribute(string formerFullName, string formerAssemblyName) : this(formerFullName)
        {
            FormerAssemblyName = formerAssemblyName;
        }
    }

    /// <summary>
    /// Refactor-Safe 名稱解析器，提供基於屬性的名稱追踪功能
    /// </summary>
    public static class RefactorSafeNameResolver
    {
        /// <summary>
        /// 根據當前名稱和歷史名稱，找到匹配的型別
        /// </summary>
        public static Type FindTypeByCurrentOrFormerName(string currentName, string assemblyName = null)
        {
            if (string.IsNullOrEmpty(currentName))
            {
                Debug.LogError("Current name is null or empty. Cannot find type.");
                return null;
            }

            // 1. 先嘗試直接用當前名稱查找
            var type = Type.GetType(currentName);
            if (type != null) return type;

            // 2. 搜尋所有已載入的 Assembly 中的型別

            var formerNameTypes = TypeCache.GetTypesWithAttribute<FormerlyNamedAsAttribute>();
            foreach (var formerType in formerNameTypes)
            {
                var formerNameAttrs = formerType.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                    .Cast<FormerlyNamedAsAttribute>();

                foreach (var attr in formerNameAttrs)
                {
                    Debug.Log("Checking former name: " + attr.FormerName + " for searchName: " + currentName);
                    if (currentName.Contains(attr.FormerName) ||
                        currentName.Contains($"{formerType.Namespace}.{attr.FormerName}"))
                    {
                        Debug.Log("Found type by former name: " + attr.FormerName);
                        return formerType;
                    }
                }
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            // 如果指定了 Assembly，優先搜尋該 Assembly
            if (!string.IsNullOrEmpty(assemblyName))
            {
                var targetAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == assemblyName);
                if (targetAssembly != null)
                {
                    var foundType = SearchTypeInAssembly(targetAssembly, currentName);
                    if (foundType != null) return foundType;
                }
            }

            // 3. 在所有 Assembly 中搜尋
            // foreach (var assembly in assemblies)
            // {
            //     var foundType = SearchTypeInAssembly(assembly, currentName);
            //     if (foundType != null) return foundType;
            // }
            Debug.LogError("Type not found: " + currentName +
                           ". Please check if the type has been renamed or moved to another assembly.");
            return null;
        }

        /// <summary>
        /// 在指定 Assembly 中搜尋型別（包含歷史名稱）
        /// </summary>
        private static Type SearchTypeInAssembly(System.Reflection.Assembly assembly, string searchName)
        {
            Debug.Log("Searching for type: " + searchName + " in assembly: " + assembly.GetName().Name);
            try
            {
                var types = assembly.GetTypes();
                
                foreach (var type in types)
                {
                    // 檢查當前名稱
                    if (type.FullName == searchName || type.Name == searchName)
                        return type;

                    // 檢查歷史完整名稱
                    var formerFullNameAttrs = type.GetCustomAttributes(typeof(FormerlyFullNameAttribute), false)
                        .Cast<FormerlyFullNameAttribute>();
                    
                    foreach (var attr in formerFullNameAttrs)
                    {
                        if (attr.FormerFullName == searchName)
                            return type;
                    }

                    // 檢查歷史簡單名稱
                    var formerNameAttrs = type.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                        .Cast<FormerlyNamedAsAttribute>();
                    
                    foreach (var attr in formerNameAttrs)
                    {
                        Debug.Log("Checking former name: " + attr.FormerName + " for searchName: " + searchName);
                        if (searchName.Contains(attr.FormerName) ||
                            searchName.Contains($"{type.Namespace}.{attr.FormerName}"))

                            return type;
                    }
                }
            }
            catch (System.Reflection.ReflectionTypeLoadException)
            {
                // 忽略無法載入的型別
            }
            catch (Exception)
            {
                // 忽略其他錯誤
            }

            return null;
        }

        /// <summary>
        /// 在指定型別中找到匹配的成員（Property 或 Field）
        /// </summary>
        public static System.Reflection.MemberInfo FindMemberByCurrentOrFormerName(Type type, string currentName)
        {
            if (type == null || string.IsNullOrEmpty(currentName)) return null;

            const System.Reflection.BindingFlags flags = 
                System.Reflection.BindingFlags.Public | 
                System.Reflection.BindingFlags.Instance;

            // 1. 先嘗試直接用當前名稱查找
            var member = type.GetProperty(currentName, flags) as System.Reflection.MemberInfo ??
                        type.GetField(currentName, flags);
            
            if (member != null) return member;

            // 2. 搜尋所有成員的歷史名稱
            var allMembers = type.GetProperties(flags).Cast<System.Reflection.MemberInfo>()
                               .Concat(type.GetFields(flags));

            foreach (var m in allMembers)
            {
                var formerNameAttrs = m.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                    .Cast<FormerlyNamedAsAttribute>();

                foreach (var attr in formerNameAttrs)
                {
                    if (attr.FormerName == currentName)
                        return m;
                }
            }

            return null;
        }

        /// <summary>
        /// 取得型別的所有歷史名稱
        /// </summary>
        public static List<string> GetTypeHistoryNames(Type type)
        {
            var names = new List<string> { type.FullName, type.Name };

            var formerFullNameAttrs = type.GetCustomAttributes(typeof(FormerlyFullNameAttribute), false)
                .Cast<FormerlyFullNameAttribute>();
            names.AddRange(formerFullNameAttrs.Select(attr => attr.FormerFullName));

            var formerNameAttrs = type.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                .Cast<FormerlyNamedAsAttribute>();
            names.AddRange(formerNameAttrs.Select(attr => attr.FormerName));
            names.AddRange(formerNameAttrs.Select(attr => $"{type.Namespace}.{attr.FormerName}"));

            return names.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        }

        /// <summary>
        /// 取得成員的所有歷史名稱
        /// </summary>
        public static List<string> GetMemberHistoryNames(System.Reflection.MemberInfo member)
        {
            var names = new List<string> { member.Name };

            var formerNameAttrs = member.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                .Cast<FormerlyNamedAsAttribute>();
            names.AddRange(formerNameAttrs.Select(attr => attr.FormerName));

            return names.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        }

        /// <summary>
        /// 檢查兩個名稱是否匹配（包含歷史名稱）
        /// </summary>
        public static bool IsNameMatch(Type type, string searchName)
        {
            if (type == null || string.IsNullOrEmpty(searchName)) return false;
            
            var historyNames = GetTypeHistoryNames(type);
            return historyNames.Any(name => string.Equals(name, searchName, StringComparison.Ordinal));
        }

        /// <summary>
        /// 檢查成員名稱是否匹配（包含歷史名稱）
        /// </summary>
        public static bool IsMemberNameMatch(System.Reflection.MemberInfo member, string searchName)
        {
            if (member == null || string.IsNullOrEmpty(searchName)) return false;
            
            var historyNames = GetMemberHistoryNames(member);
            return historyNames.Any(name => string.Equals(name, searchName, StringComparison.Ordinal));
        }

        /// <summary>
        /// 取得型別的重命名追踪資訊
        /// </summary>
        public static RefactorTrackingInfo GetTypeTrackingInfo(Type type)
        {
            var info = new RefactorTrackingInfo
            {
                CurrentName = type.FullName,
                CurrentSimpleName = type.Name,
                AssemblyName = type.Assembly.GetName().Name
            };

            var formerFullNameAttrs = type.GetCustomAttributes(typeof(FormerlyFullNameAttribute), false)
                .Cast<FormerlyFullNameAttribute>();
            
            foreach (var attr in formerFullNameAttrs)
            {
                info.FormerNames.Add(new RefactorHistoryEntry
                {
                    Name = attr.FormerFullName,
                    Version = attr.Version,
                    AssemblyName = attr.FormerAssemblyName
                });
            }

            var formerNameAttrs = type.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                .Cast<FormerlyNamedAsAttribute>();
            
            foreach (var attr in formerNameAttrs)
            {
                info.FormerNames.Add(new RefactorHistoryEntry
                {
                    Name = attr.FormerName,
                    Version = attr.Version,
                    Reason = attr.Reason
                });
            }

            return info;
        }

        /// <summary>
        /// 取得成員的重命名追踪資訊
        /// </summary>
        public static RefactorTrackingInfo GetMemberTrackingInfo(System.Reflection.MemberInfo member)
        {
            var info = new RefactorTrackingInfo
            {
                CurrentName = member.Name,
                CurrentSimpleName = member.Name
            };

            var formerNameAttrs = member.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                .Cast<FormerlyNamedAsAttribute>();
            
            foreach (var attr in formerNameAttrs)
            {
                info.FormerNames.Add(new RefactorHistoryEntry
                {
                    Name = attr.FormerName,
                    Version = attr.Version,
                    Reason = attr.Reason
                });
            }

            return info;
        }
    }

    /// <summary>
    /// Refactor 追踪資訊
    /// </summary>
    [Serializable]
    public class RefactorTrackingInfo
    {
        public string CurrentName;
        public string CurrentSimpleName;
        public string AssemblyName;
        public List<RefactorHistoryEntry> FormerNames = new List<RefactorHistoryEntry>();

        public bool HasFormerNames => FormerNames.Count > 0;
    }

    /// <summary>
    /// Refactor 歷史記錄項目
    /// </summary>
    [Serializable]
    public class RefactorHistoryEntry
    {
        public string Name;
        public string Version;
        public string Reason;
        public string AssemblyName;
    }
}