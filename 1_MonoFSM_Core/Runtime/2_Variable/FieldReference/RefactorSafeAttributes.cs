using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonoFSM.Variable.FieldReference
{
    /// <summary>
    /// 自定義的 FormerlyNamedAs 屬性，可以應用於 Class、Property、Field 等
    /// 支援多層級的名稱追踪，比 Unity 的 FormerlySerializedAs 更強大
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class
            | AttributeTargets.Property
            | AttributeTargets.Field
            | AttributeTargets.Method,
        AllowMultiple = true,
        Inherited = false
    )]
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

        public FormerlyNamedAsAttribute(string formerName, string version)
            : this(formerName)
        {
            Version = version;
        }

        public FormerlyNamedAsAttribute(string formerName, string version, string reason)
            : this(formerName, version)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// 指定型別的完整名稱歷史，支援 namespace 和 class 名稱變更
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        AllowMultiple = true,
        Inherited = false
    )]
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
            FormerFullName =
                formerFullName ?? throw new ArgumentNullException(nameof(formerFullName));
        }

        public FormerlyFullNameAttribute(string formerFullName, string formerAssemblyName)
            : this(formerFullName)
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
        /// 名稱 → 型別的解析結果快取，解析失敗的 null 也會被記住，避免同一個壞名稱反覆觸發全域掃描
        /// </summary>
        private static readonly Dictionary<string, Type> _resolvedTypeCache = new();

        /// <summary>
        /// 全域型別索引，只在第一次解析失敗時才建立
        /// </summary>
        private static Dictionary<string, Type> _fullNameIndex;

        /// <summary>
        /// 簡單名稱索引，撞名的項目值為 null 代表無法判定、不予採用
        /// </summary>
        private static Dictionary<string, Type> _simpleNameIndex;

        /// <summary>
        /// 根據當前名稱和歷史名稱，找到匹配的型別。
        /// 找不到就靜默回傳 null，錯誤訊息由呼叫端負責印（它才知道是哪個 asset 的哪個欄位）
        /// </summary>
        public static Type FindTypeByCurrentOrFormerName(
            string currentName,
            string assemblyName = null
        )
        {
            if (string.IsNullOrEmpty(currentName))
                return null;

            if (!_resolvedTypeCache.TryGetValue(currentName, out var type))
            {
                type = ResolveTypeUncached(currentName, assemblyName);
                _resolvedTypeCache[currentName] = type;
            }

            return type;
        }

        private static Type ResolveTypeUncached(string currentName, string assemblyName)
        {
            // 1. 直接解析，名稱與 Assembly 都沒變動時走這條
            var type = Type.GetType(currentName);
            if (type != null)
                return type;

            // 2. 剝掉 AssemblyQualifiedName 尾端的 Assembly 資訊，後續都用純型別全名比對
            var fullName = ExtractFullName(currentName);

            // 3. 有指定 Assembly 就優先在該 Assembly 裡找
            if (!string.IsNullOrEmpty(assemblyName))
            {
                var targetAssembly = AppDomain
                    .CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName);
                var foundType = targetAssembly?.GetType(fullName, false);
                if (foundType != null)
                    return foundType;
            }

            EnsureNameIndexBuilt();

            // 4. 型別全名沒變，只是被搬到另一個 Assembly（例如原本在 Assembly-CSharp，拆 asmdef 後搬走）
            if (_fullNameIndex.TryGetValue(fullName, out var byFullName))
                return byFullName;

            // 5. namespace 換過但類別名沒變，只有唯一候選時才敢認
            var simpleName = ExtractSimpleName(fullName);
            if (_simpleNameIndex.TryGetValue(simpleName, out var bySimpleName) && bySimpleName != null)
                return bySimpleName;

            return null;
        }

        /// <summary>
        /// 從 AssemblyQualifiedName 取出型別全名。泛型型別的名稱含有 '['，格式複雜且 Type.GetType 多半已能處理，維持原樣不動
        /// </summary>
        private static string ExtractFullName(string typeName)
        {
            if (typeName.IndexOf('[') >= 0)
                return typeName;

            var commaIndex = typeName.IndexOf(',');
            return commaIndex < 0 ? typeName : typeName.Substring(0, commaIndex).Trim();
        }

        private static readonly char[] _nameSeparators = { '.', '+' };

        private static string ExtractSimpleName(string fullName)
        {
            var separatorIndex = fullName.LastIndexOfAny(_nameSeparators);
            return separatorIndex < 0 ? fullName : fullName.Substring(separatorIndex + 1);
        }

        private static void EnsureNameIndexBuilt()
        {
            if (_fullNameIndex != null)
                return;

            _fullNameIndex = new Dictionary<string, Type>();
            _simpleNameIndex = new Dictionary<string, Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException e)
                {
                    types = e.Types; // 部分載入成功的型別仍然可用
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type == null)
                        continue;

                    RegisterFullName(type.FullName, type);
                    RegisterSimpleName(type.Name, type);
                }
            }

            RegisterFormerNames();
        }

        /// <summary>
        /// 把標了歷史名稱 attribute 的型別，用它的舊名字也登記進索引
        /// </summary>
        private static void RegisterFormerNames()
        {
#if UNITY_EDITOR
            var formerFullNameTypes = TypeCache.GetTypesWithAttribute<FormerlyFullNameAttribute>();
            var formerNameTypes = TypeCache.GetTypesWithAttribute<FormerlyNamedAsAttribute>();
#else
            // 迴圈中會寫回索引，必須先取出快照再迭代
            var allTypes = _fullNameIndex.Values.ToArray();
            var formerFullNameTypes = allTypes;
            var formerNameTypes = allTypes;
#endif

            foreach (var type in formerFullNameTypes)
            {
                foreach (
                    var attr in type.GetCustomAttributes(typeof(FormerlyFullNameAttribute), false)
                        .Cast<FormerlyFullNameAttribute>()
                )
                {
                    RegisterFullName(attr.FormerFullName, type);
                }
            }

            foreach (var type in formerNameTypes)
            {
                foreach (
                    var attr in type.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                        .Cast<FormerlyNamedAsAttribute>()
                )
                {
                    RegisterSimpleName(attr.FormerName, type);
                    RegisterFullName($"{type.Namespace}.{attr.FormerName}", type);
                }
            }
        }

        /// <summary>
        /// 先登記的優先，避免舊名稱蓋掉某個型別實際使用中的全名
        /// </summary>
        private static void RegisterFullName(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                return;
            if (!_fullNameIndex.ContainsKey(name))
                _fullNameIndex[name] = type;
        }

        /// <summary>
        /// 簡單名稱撞名時記成 null，寧可解不出來也不要認錯型別
        /// </summary>
        private static void RegisterSimpleName(string name, Type type)
        {
            if (string.IsNullOrEmpty(name))
                return;
            if (_simpleNameIndex.TryGetValue(name, out var existing))
            {
                if (existing != type)
                    _simpleNameIndex[name] = null;
                return;
            }

            _simpleNameIndex[name] = type;
        }

        /// <summary>
        /// 在指定型別中找到匹配的成員（Property 或 Field）
        /// TODO: 先把FormerName拿掉了，效能很差，要找不到才去找formerName
        /// </summary>
        public static System.Reflection.MemberInfo FindMemberByCurrentOrFormerName(
            Type type,
            string currentName
        )
        {
            if (type == null || string.IsNullOrEmpty(currentName))
                return null;

            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;

            // 1. 先嘗試直接用當前名稱查找
            var member =
                type.GetProperty(currentName, flags) as System.Reflection.MemberInfo
                ?? type.GetField(currentName, flags);

            if (member != null)
                return member;

            //這坨效能很爛，先註解掉
            // // 2. 搜尋所有成員的歷史名稱
            // var allMembers = type.GetProperties(flags).Cast<System.Reflection.MemberInfo>()
            //                    .Concat(type.GetFields(flags));
            //
            // foreach (var m in allMembers)
            // {
            //     var formerNameAttrs = m.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
            //         .Cast<FormerlyNamedAsAttribute>();
            //
            //     foreach (var attr in formerNameAttrs)
            //     {
            //         if (attr.FormerName == currentName)
            //             return m;
            //     }
            // }

            return null;
        }

        /// <summary>
        /// 取得型別的所有歷史名稱
        /// </summary>
        public static List<string> GetTypeHistoryNames(Type type)
        {
            var names = new List<string> { type.FullName, type.Name };

            var formerFullNameAttrs = type.GetCustomAttributes(
                    typeof(FormerlyFullNameAttribute),
                    false
                )
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

            var formerNameAttrs = member
                .GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                .Cast<FormerlyNamedAsAttribute>();
            names.AddRange(formerNameAttrs.Select(attr => attr.FormerName));

            return names.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        }

        /// <summary>
        /// 檢查兩個名稱是否匹配（包含歷史名稱）
        /// </summary>
        public static bool IsNameMatch(Type type, string searchName)
        {
            if (type == null || string.IsNullOrEmpty(searchName))
                return false;

            var historyNames = GetTypeHistoryNames(type);
            return historyNames.Any(name =>
                string.Equals(name, searchName, StringComparison.Ordinal)
            );
        }

        /// <summary>
        /// 檢查成員名稱是否匹配（包含歷史名稱）
        /// </summary>
        public static bool IsMemberNameMatch(System.Reflection.MemberInfo member, string searchName)
        {
            if (member == null || string.IsNullOrEmpty(searchName))
                return false;

            var historyNames = GetMemberHistoryNames(member);
            return historyNames.Any(name =>
                string.Equals(name, searchName, StringComparison.Ordinal)
            );
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
                AssemblyName = type.Assembly.GetName().Name,
            };

            var formerFullNameAttrs = type.GetCustomAttributes(
                    typeof(FormerlyFullNameAttribute),
                    false
                )
                .Cast<FormerlyFullNameAttribute>();

            foreach (var attr in formerFullNameAttrs)
            {
                info.FormerNames.Add(
                    new RefactorHistoryEntry
                    {
                        Name = attr.FormerFullName,
                        Version = attr.Version,
                        AssemblyName = attr.FormerAssemblyName,
                    }
                );
            }

            var formerNameAttrs = type.GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                .Cast<FormerlyNamedAsAttribute>();

            foreach (var attr in formerNameAttrs)
            {
                info.FormerNames.Add(
                    new RefactorHistoryEntry
                    {
                        Name = attr.FormerName,
                        Version = attr.Version,
                        Reason = attr.Reason,
                    }
                );
            }

            return info;
        }

        /// <summary>
        /// 取得成員的重命名追踪資訊
        /// </summary>
        public static RefactorTrackingInfo GetMemberTrackingInfo(
            System.Reflection.MemberInfo member
        )
        {
            var info = new RefactorTrackingInfo
            {
                CurrentName = member.Name,
                CurrentSimpleName = member.Name,
            };

            var formerNameAttrs = member
                .GetCustomAttributes(typeof(FormerlyNamedAsAttribute), false)
                .Cast<FormerlyNamedAsAttribute>();

            foreach (var attr in formerNameAttrs)
            {
                info.FormerNames.Add(
                    new RefactorHistoryEntry
                    {
                        Name = attr.FormerName,
                        Version = attr.Version,
                        Reason = attr.Reason,
                    }
                );
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
