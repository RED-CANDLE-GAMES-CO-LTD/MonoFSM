using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// FSM 的複合操作 —— 把「建節點 + 掛 component + 接引用」這組固定樣板收成一個 verb。
    ///
    /// 為什麼：組一個 state 用原語要三到四行，而每行都要重寫一次那條長路徑
    /// （`[StateFolder] StateFolder/[State] idle/[Event] OnStateEnter/[Action] X`）。
    /// 一個十個 state 的 FSM 因此變成上百行、其中大半是重複的路徑字串。
    /// 這裡只做「一定會這樣做」的部分：節點命名慣例（`[State]` / `[Transition] =&gt;` /
    /// `[If]` / `[Action]` / `[Event]` 前綴）、handler 型別對照、transition 的 `_target`。
    /// 其餘欄位照舊用 `set` / `ref` 接（配合 EditBatch 的 `$` 代換就不必重寫路徑）。
    ///
    /// prefab 與 scene 共用：差異只在「怎麼把路徑解成 Transform」跟「要不要標髒」，
    /// 由呼叫端用 <see cref="Ctx"/> 注進來。
    /// </summary>
    internal static class EditFsm
    {
        internal sealed class Ctx
        {
            /// <summary>路徑 → Transform（找不到就自己 abort）。</summary>
            public Func<string, Transform> Node;

            /// <summary>scene 要標髒；prefab 傳 null（整批結束才存）。</summary>
            public Action Dirty;
        }

        /// <summary>state 的 OnXxx 事件：phase 關鍵字 → (節點名, handler 型別)。</summary>
        private static readonly Dictionary<string, (string Node, string Handler)> Phases = new()
        {
            ["enter"] = ("[Event] OnStateEnter", "OnStateEnterHandler"),
            ["exit"] = ("[Event] OnStateExit", "OnStateExitHandler"),
            ["update"] = ("[Event] OnStateUpdate", "OnStateUpdateHandler"),
            ["enterrender"] = ("[Event] OnStateEnterRender", "OnStateEnterRenderHandler"),
            ["exitrender"] = ("[Event] OnStateExitRender", "OnStateExitRenderHandler"),
        };

        internal static readonly string Verbs = "state trans if act";

        /// <summary>不是 FSM verb 就回 false，讓呼叫端的 switch 走它自己的 default。</summary>
        internal static bool TryDispatch(Ctx ctx, string verb, string[] a, out string result)
        {
            switch (verb)
            {
                case "state":
                    result = State(ctx, a);
                    return true;
                case "trans":
                    result = Trans(ctx, a);
                    return true;
                case "if":
                    result = If(ctx, a);
                    return true;
                case "act":
                    result = Act(ctx, a);
                    return true;
                default:
                    result = null;
                    return false;
            }
        }

        // state|<folder>|<name>[|<stateType>]
        private static string State(Ctx ctx, string[] a)
        {
            var folderPath = EditBatch.At(a, 0);
            var name = Tagged("[State]", EditBatch.Need(a, 1, "state", "name"));
            var type = EditBatch.At(a, 2) ?? "GeneralState";

            Ensure(ctx, folderPath, name, out var full, out var created, type);
            return created ? $"建立 {full}  <{type}>" : $"（已存在）{full}  <{type}>";
        }

        // trans|<fromState>|<toState>[|<name>]
        private static string Trans(Ctx ctx, string[] a)
        {
            var fromPath = EditBatch.Need(a, 0, "trans", "fromState");
            var toPath = EditBatch.Need(a, 1, "trans", "toState");
            var to = ctx.Node(toPath);

            // 節點名沿用 TransitionBehaviour.Description 的寫法：`=> ` 後面是去掉 [State] 的目標名
            var name = EditBatch.At(a, 2)
                       ?? $"[Transition] => {to.name.Replace("[State]", "").Trim()}";
            name = Tagged("[Transition]", name);

            var node = Ensure(ctx, fromPath, name, out var full, out _, "TransitionBehaviour");
            var trans = EditResolve.Comp(node, full, "TransitionBehaviour");
            SetRef(trans, "_target", to, toPath);
            return $"建立 {full}  _target -> {EditResolve.Describe(toPath)}";
        }

        // if|<node>|<name>|<condType>[|<field>|<target>]
        private static string If(Ctx ctx, string[] a)
        {
            var parentPath = EditBatch.Need(a, 0, "if", "transition / state 節點");
            var name = Tagged("[If]", EditBatch.Need(a, 1, "if", "name"));
            var condType = EditBatch.Need(a, 2, "if", "conditionType");

            var node = Ensure(ctx, parentPath, name, out var full, out _, condType);
            var tail = "";

            var field = EditBatch.At(a, 3);
            if (field != null)
            {
                var targetPath = EditBatch.Need(a, 4, "if", "target");
                var comp = EditResolve.Comp(node, full, condType);
                SetRef(comp, field, ctx.Node(targetPath), targetPath);
                tail = $"  {field} -> {EditResolve.Describe(targetPath)}";
            }

            return $"建立 {full}  <{condType}>{tail}";
        }

        // act|<state>|<phase>|<name>|<actionType>
        private static string Act(Ctx ctx, string[] a)
        {
            var statePath = EditBatch.Need(a, 0, "act", "state 節點");
            var phaseKey = EditBatch.Need(a, 1, "act", "phase").Trim().ToLowerInvariant()
                .Replace("-", "").Replace("_", "");
            if (!Phases.TryGetValue(phaseKey, out var phase))
                throw new EditResolve.EditAbort(
                    $"`act` 的 phase 只能是 {string.Join(" / ", Phases.Keys)}，收到 '{phaseKey}'");

            var name = Tagged("[Action]", EditBatch.Need(a, 2, "act", "name"));
            var actionType = EditBatch.Need(a, 3, "act", "actionType");

            // 事件節點多個 action 共用，所以是 ensure 而不是 add
            Ensure(ctx, statePath, phase.Node, out var eventPath, out var newEvent, phase.Handler);
            Ensure(ctx, eventPath, name, out var full, out _, actionType);

            return $"建立 {full}  <{actionType}>" +
                   (newEvent ? $"（順手建了 {phase.Node}）" : "");
        }

        // ---- 共用 ----

        /// <summary>MonoFSM 的節點命名慣例：沒帶 `[Tag]` 前綴就補上。</summary>
        private static string Tagged(string tag, string name)
        {
            name = name.Trim();
            return name.StartsWith("[") ? name : $"{tag} {name}";
        }

        /// <summary>
        /// 建節點並掛 component；已存在就沿用（`created` 回 false）。
        /// 「已存在就沿用」而不是 abort，理由同 add：批次常常要修一行再整份重跑。
        /// </summary>
        private static Transform Ensure(
            Ctx ctx, string parentPath, string name, out string full, out bool created,
            params string[] types)
        {
            var parent = ctx.Node(parentPath);
            full = string.IsNullOrEmpty(parentPath) ? name : $"{parentPath}/{name}";

            var node = parent.Find(name);
            created = node == null;
            if (created)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                node = go.transform;
            }

            foreach (var typeName in types)
            {
                if (string.IsNullOrWhiteSpace(typeName)) continue;
                var type = EditResolve.CompType(typeName);
                if (node.GetComponent(type) == null) node.gameObject.AddComponent(type);
            }

            ctx.Dirty?.Invoke();
            EditBatch.Touch(full);
            return node;
        }

        private static void SetRef(Component comp, string fieldPath, Transform target,
            string targetPath)
        {
            var so = new SerializedObject(comp);
            var prop = EditResolve.Prop(so, fieldPath, comp);
            if (prop.propertyType != SerializedPropertyType.ObjectReference)
                throw new EditResolve.EditAbort(
                    $"'{fieldPath}' 是 {prop.propertyType}，不是物件引用；請改用 set");
            prop.objectReferenceValue =
                EditResolve.RefTarget(target, targetPath, comp, fieldPath, null);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
