using System.Collections.Generic;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using _1_MonoFSM_Core.Runtime.LifeCycle.Update;
using MonoFSM.FSM;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HierarchyFavorites.Editor
{
    internal class VariableGroup
    {
        public string Name;
        public Transform FolderTransform;
        public List<VariableEntry> Items = new();
    }

    internal class VariableEntry
    {
        public Transform Target;
        public string Label;
        public string TypeName;
        public string TagName;
    }

    /// <summary>
    /// Variables / Effects / States 三種 tab 的收集邏輯。
    /// 分組規則共用：最近的 MonoModulePack 祖先名，找不到就用 prefab root 名。
    /// </summary>
    internal static class VariableFolderCollector
    {
        // ---- Variables ----
        // 從 root 直接撈全部 AbstractMonoVariable（涵蓋不在 VariableFolder 下的），
        // 不在 folder 下的放到獨立的 "<group> (No Folder)" group，排在對應 group 後面
        public static List<VariableGroup> GetActiveGroups()
        {
            var result = new List<VariableGroup>();

            var inFolderGroups = new Dictionary<string, VariableGroup>();
            var noFolderGroups = new Dictionary<string, VariableGroup>();
            var baseNameOrder = new List<string>();

            foreach (var root in GetActiveRoots())
            {
                var variables = root.GetComponentsInChildren<AbstractMonoVariable>(true);
                foreach (var v in variables)
                {
                    if (v == null) continue;

                    var groupTransform = GetGroupTransform(root, v.transform);
                    var baseName = groupTransform.name;
                    // 自己就掛在 folder GameObject 上也算在 folder 內（GetComponentInParent 含 self）
                    var inFolder = v.GetComponentInParent<VariableFolder>(true) != null;

                    if (!baseNameOrder.Contains(baseName))
                        baseNameOrder.Add(baseName);

                    var map = inFolder ? inFolderGroups : noFolderGroups;
                    if (!map.TryGetValue(baseName, out var group))
                    {
                        group = new VariableGroup
                        {
                            Name = inFolder ? baseName : $"{baseName} (No Folder)",
                            FolderTransform = groupTransform,
                        };
                        map[baseName] = group;
                    }

                    group.Items.Add(new VariableEntry
                    {
                        Target = v.transform,
                        Label = v.gameObject.name,
                        TypeName = v.GetType().Name,
                        TagName = v._varTag != null ? v._varTag.name : string.Empty,
                    });
                }
            }

            // (No Folder) group 排在對應 group 後面，讓遺漏的一眼可見
            foreach (var baseName in baseNameOrder)
            {
                if (inFolderGroups.TryGetValue(baseName, out var g)) result.Add(g);
                if (noFolderGroups.TryGetValue(baseName, out var ng)) result.Add(ng);
            }

            return result;
        }

        // ---- Effects ----
        // 收集實作 IEffectDealer / IEffectReceiver 的 MonoBehaviour，同一 GameObject 去重
        public static List<VariableGroup> GetEffectGroups()
        {
            var result = new List<VariableGroup>();
            var groups = new Dictionary<Transform, VariableGroup>();

            foreach (var root in GetActiveRoots())
            {
                // 同一 GameObject 上可能掛多個 dealer/receiver component，先彙整
                var goOrder = new List<GameObject>();
                var dealerSet = new HashSet<GameObject>();
                var receiverSet = new HashSet<GameObject>();
                var typeNames = new Dictionary<GameObject, List<string>>();

                var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var mb in behaviours)
                {
                    if (mb == null) continue; //missing script 防呆
                    var isDealer = mb is IEffectDealer;
                    var isReceiver = mb is IEffectReceiver;
                    if (!isDealer && !isReceiver) continue;

                    var go = mb.gameObject;
                    if (!typeNames.TryGetValue(go, out var names))
                    {
                        names = new List<string>();
                        typeNames[go] = names;
                        goOrder.Add(go);
                    }

                    var typeName = mb.GetType().Name;
                    if (!names.Contains(typeName)) names.Add(typeName);
                    if (isDealer) dealerSet.Add(go);
                    if (isReceiver) receiverSet.Add(go);
                }

                foreach (var go in goOrder)
                {
                    var groupTransform = GetGroupTransform(root, go.transform);
                    var group = GetOrAddGroup(result, groups, groupTransform, groupTransform.name);

                    var isDealer = dealerSet.Contains(go);
                    var isReceiver = receiverSet.Contains(go);
                    group.Items.Add(new VariableEntry
                    {
                        Target = go.transform,
                        Label = go.name,
                        TypeName = string.Join(", ", typeNames[go]),
                        TagName = isDealer && isReceiver ? "D/R" : isDealer ? "D" : "R",
                    });
                }
            }

            return result;
        }

        // ---- States ----
        // 分組用最近的 MonoStateMachineController 祖先（一台 FSM 一組），
        // 找不到 controller 才 fallback 到 ModulePack / prefab root 規則
        public static List<VariableGroup> GetStateGroups()
        {
            var result = new List<VariableGroup>();
            var groups = new Dictionary<Transform, VariableGroup>();

            foreach (var root in GetActiveRoots())
            {
                var states = root.GetComponentsInChildren<MonoStateBehaviour>(true);
                foreach (var state in states)
                {
                    if (state == null) continue;

                    var controller = state.GetComponentInParent<MonoStateMachineController>(true);
                    var groupTransform =
                        controller != null && controller.transform.IsChildOf(root)
                            ? controller.transform
                            : GetGroupTransform(root, state.transform);

                    var group = GetOrAddGroup(result, groups, groupTransform, groupTransform.name);
                    group.Items.Add(new VariableEntry
                    {
                        Target = state.transform,
                        Label = state.gameObject.name,
                        TypeName = state.GetType().Name,
                        TagName = string.Empty,
                    });
                }
            }

            return result;
        }

        // ---- Descriptions ----
        // 撈 root 底下全部 AbstractDescriptionBehaviour（涵蓋 States/Actions/Variables 等所有子類），
        // 分組比照 Variables/Effects：最近的 MonoModulePack 祖先，找不到用 prefab root
        public static List<VariableGroup> GetDescriptionGroups()
        {
            var result = new List<VariableGroup>();
            var groups = new Dictionary<Transform, VariableGroup>();

            foreach (var root in GetActiveRoots())
            {
                var descriptions = root.GetComponentsInChildren<AbstractDescriptionBehaviour>(true);
                foreach (var desc in descriptions)
                {
                    if (desc == null) continue;

                    var groupTransform = GetGroupTransform(root, desc.transform);
                    var group = GetOrAddGroup(result, groups, groupTransform, groupTransform.name);
                    group.Items.Add(new VariableEntry
                    {
                        Target = desc.transform,
                        Label = desc.gameObject.name,
                        TypeName = desc.GetType().Name,
                        TagName = string.Empty,
                    });
                }
            }

            return result;
        }

        // ---- 共用 helpers ----

        private static VariableGroup GetOrAddGroup(List<VariableGroup> result,
            Dictionary<Transform, VariableGroup> groups, Transform groupTransform, string name)
        {
            if (groups.TryGetValue(groupTransform, out var group)) return group;
            group = new VariableGroup { Name = name, FolderTransform = groupTransform };
            groups[groupTransform] = group;
            result.Add(group);
            return group;
        }

        private static List<Transform> GetActiveRoots() =>
            HierarchyFavoritesRootResolver.GetActiveRoots();

        // 分組依據：最近的 MonoModulePack 祖先（含 self），沒有就用 prefab root（= prefab 名稱）
        private static Transform GetGroupTransform(Transform root, Transform target)
        {
            var pack = target.GetComponentInParent<MonoModulePack>(true);
            if (pack != null && pack.transform.IsChildOf(root))
                return pack.transform;
            return root;
        }
    }
}
