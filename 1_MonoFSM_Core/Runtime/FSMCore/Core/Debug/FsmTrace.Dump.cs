using System.IO;
using System.Text;
using _1_MonoFSM_Core.Runtime.FSMCore.Core.StateBehaviour;
using MonoFSM.Core.Simulate;
using MonoFSM.Runtime;
using MonoFSM.Runtime.Interact.EffectHit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.FSM
{
    //Dump / 條件快照：只在呼叫時組字串（Editor / debug 用途），不在任何每 tick 的路徑上
    public static partial class FsmTrace
    {
        /// <summary>Dump 檔的分段標記：這行之後是 CaptureSnapshot 的內容（`up fsm-trace` 靠它切段）。</summary>
        public const string SnapshotSectionMarker = "## SNAPSHOT";

        /// <summary>
        /// Editor：&lt;專案根&gt;/Library/FsmTrace；Development Build：persistentDataPath/FsmTrace。
        /// </summary>
        public static string DumpFolder =>
#if UNITY_EDITOR
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "FsmTrace"));
#else
            Path.Combine(Application.persistentDataPath, "FsmTrace");
#endif

        /// <summary>
        /// 把 buffer 寫成 &lt;DumpFolder&gt;/&lt;name&gt;.txt，一行一筆（舊 → 新），回傳檔案路徑。
        /// entity 不為 null 時只留跟它（含子樹）有關的紀錄，並在檔尾附上它的 <see cref="CaptureSnapshot(MonoEntity)"/>。
        /// 正式 build 沒有 buffer，回 null。
        /// </summary>
        public static string Dump(string name, MonoEntity entity = null, bool includeSnapshot = true)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (string.IsNullOrEmpty(name))
                name = "trace";
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            var sb = new StringBuilder(64 * 1024);
            var filterRoot = entity != null ? entity.transform : null;
            sb.Append("# FsmTrace name=").Append(name)
                .Append(" enabled=").Append(_enabled)
                .Append(" entries=").Append(_count)
                .Append(" total=").Append(_totalWritten)
                .Append(" dropped=").Append(_totalWritten - _count)
                .Append(" tick=").Append(WorldUpdateSimulator.CurrentTick)
                .Append(" frame=").Append(Time.frameCount)
                .Append(" time=").Append(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Append('\n');
            if (filterRoot != null)
                sb.Append("# entity=").Append(entity.name).Append('\n');
            sb.Append("# 欄位：seq frame tick [R=resim] kind 內容\n");

            for (var i = 0; i < _count; i++)
            {
                ref readonly var e = ref GetEntry(i);
                if (filterRoot != null && !IsEntryUnder(e, filterRoot))
                    continue;
                AppendEntry(sb, e);
                sb.Append('\n');
            }

            if (entity != null && includeSnapshot)
            {
                sb.Append(SnapshotSectionMarker).Append('\n');
                AppendSnapshot(sb, entity);
            }

            var folder = DumpFolder;
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, name + ".txt");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
#else
            return null;
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool IsEntryUnder(in FsmTraceEntry e, Transform root)
        {
            return IsUnder(e._machine, root)
                   || IsUnder(e._fromState, root)
                   || IsUnder(e._toState, root)
                   || IsUnder(e._dealer, root)
                   || IsUnder(e._receiver, root)
                   || IsUnder(e._var, root);
        }

        private static bool IsUnder(Object o, Transform root)
        {
            return o is Component c && c != null && c.transform.IsChildOf(root);
        }

        private static void AppendEntry(StringBuilder sb, in FsmTraceEntry e)
        {
            sb.Append(e._seq.ToString("D5"))
                .Append(" f").Append(e._frame)
                .Append(" t").Append(e._tick)
                .Append(e._isResim ? " R " : "   ")
                .Append(e._kind.ToString());

            switch (e._kind)
            {
                case FsmTraceKind.HitEnter:
                case FsmTraceKind.HitExit:
                case FsmTraceKind.HitBlocked:
                    if (e._kind == FsmTraceKind.HitBlocked)
                        sb.Append(' ').Append(e._blockReason.ToString());
                    sb.Append(" dealer=").Append(Label(e._dealer))
                        .Append(" receiver=").Append(Label(e._receiver));
                    if (e._failConditionIndex != NoFailIndex)
                        sb.Append(" failCond=").Append(FailConditionLabel(e));
                    break;
                case FsmTraceKind.EventSkipped:
                    sb.Append(' ').Append(e._skipReason.ToString())
                        .Append(' ').Append(Label(e._receiver));
                    break;
                case FsmTraceKind.VarChange:
                    sb.Append(' ').Append(Label(e._var)).Append(": ");
                    AppendValue(sb, e, true);
                    sb.Append(" -> ");
                    AppendValue(sb, e, false);
                    if (!(e._writer is null))
                        sb.Append(" by ").Append(Label(e._writer));
                    break;
                default:
                    sb.Append(' ').Append(MachineLabel(e._machine, e._toState != null ? e._toState : e._fromState))
                        .Append(": ").Append(NodeName(e._fromState))
                        .Append(" -> ").Append(NodeName(e._toState));
                    if (e._kind == FsmTraceKind.Transition)
                        sb.Append(" via ").Append(e._transition is null ? "(direct TryActivateState)" : NodeName(e._transition));
                    break;
            }
        }

        private static void AppendValue(StringBuilder sb, in FsmTraceEntry e, bool old)
        {
            var num = old ? e._oldNum : e._newNum;
            switch (e._valueKind)
            {
                case FsmTraceValueKind.Bool:
                    sb.Append(num != 0 ? "True" : "False");
                    break;
                case FsmTraceValueKind.Int:
                    sb.Append((int)num);
                    break;
                case FsmTraceValueKind.Float:
                    sb.Append(((float)num).ToString("G6", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case FsmTraceValueKind.Object:
                    sb.Append(Label(old ? e._oldObj : e._newObj));
                    break;
                case FsmTraceValueKind.Ref:
                    var r = old ? e._oldRef : e._newRef;
                    sb.Append(r == null ? "null" : "\"" + r + "\"");
                    break;
                default:
                    sb.Append('?');
                    break;
            }
        }

        private static string FailConditionLabel(in FsmTraceEntry e)
        {
            if (e._failConditionIndex == InactiveFailIndex)
                return "(resolver inactive)";
            var i = e._failConditionIndex;
            Object cond = null;
            switch (e._blockReason)
            {
                case FsmTraceBlockReason.ReceiverInvalid:
                    if (e._receiver is EffectResolver rr && rr != null)
                        cond = At(rr.ResolverConditions, i);
                    break;
                case FsmTraceBlockReason.DealerInvalid:
                    if (e._dealer is EffectResolver dr && dr != null)
                        cond = At(dr.ResolverConditions, i);
                    break;
                case FsmTraceBlockReason.EffectConditionFailed:
                    if (e._dealer is GeneralEffectDealer gd && gd != null && gd.EffectConditions != null
                        && i >= 0 && i < gd.EffectConditions.Length)
                        cond = gd.EffectConditions[i];
                    break;
            }

            return "#" + i + " " + NodeName(cond);
        }

        private static Object At(AbstractConditionBehaviour[] arr, int i)
        {
            return arr != null && i >= 0 && i < arr.Length ? arr[i] : null;
        }
#endif

        // LABEL HELPERS

        private static string NodeName(Object o)
        {
            if (o is null)
                return "-";
            if (o == null)
                return "<destroyed>";
            return o.name;
        }

        /// <summary>「entity 名/節點名」；節點本身就是 entity 或上面沒有 entity 時只印節點名。</summary>
        private static string Label(Object o)
        {
            if (o is null)
                return "-";
            if (o == null)
                return "<destroyed>";
            if (o is Component c)
            {
                var entity = c.GetComponentInParent<MonoEntity>(true);
                if (entity != null && entity.gameObject != c.gameObject)
                    return entity.name + "/" + c.name;
            }

            return o.name;
        }

        //StateMachine.Name = owner.transform.parent.name，這裡用 state 反查同一個名字
        private static string MachineLabel(StateMachineLogic logic, Object state)
        {
            var entityName = logic != null && logic.ParentEntity != null ? logic.ParentEntity.name : NodeName(logic);
            string machineName = null;
            if (state is MonoStateBehaviour s && s != null && s.Owner != null)
                machineName = s.Owner.transform.parent != null ? s.Owner.transform.parent.name : s.Owner.name;
            return machineName == null ? entityName : entityName + "/" + machineName;
        }

        // SNAPSHOT

        /// <summary>
        /// 這顆 entity 每顆 machine 的條件快照：目前 state、每條 outgoing transition（含 AnyState，照
        /// IMonoState.OnFixedUpdate 的實際評估順序）、每顆 condition 現在 true / false。
        /// 用在「等了很久都沒轉出去」的時候拍一張，取代每 tick 記「沒轉出去」。會組字串，只在除錯時呼叫。
        /// </summary>
        public static string CaptureSnapshot(MonoEntity entity)
        {
            var sb = new StringBuilder(4096);
            AppendSnapshot(sb, entity);
            return sb.ToString();
        }

        /// <inheritdoc cref="CaptureSnapshot(MonoEntity)"/>
        public static string CaptureSnapshot(StateMachineLogic logic)
        {
            var sb = new StringBuilder(4096);
            AppendSnapshot(sb, logic);
            return sb.ToString();
        }

        public static void AppendSnapshot(StringBuilder sb, MonoEntity entity)
        {
            if (entity == null)
            {
                sb.Append("(entity is null)\n");
                return;
            }

            var logic = entity.FsmLogic;
            if (logic == null)
            {
                sb.Append("[entity] ").Append(entity.name).Append(": 沒有 StateMachineLogic\n");
                return;
            }

            AppendSnapshot(sb, logic);
        }

        public static void AppendSnapshot(StringBuilder sb, StateMachineLogic logic)
        {
            if (logic == null)
            {
                sb.Append("(StateMachineLogic is null)\n");
                return;
            }

            sb.Append("[snapshot] ").Append(Label(logic))
                .Append(" tick=").Append(WorldUpdateSimulator.CurrentTick)
                .Append(" frame=").Append(Time.frameCount).Append('\n');

            var machines = logic.StateMachines;
            if (machines == null || machines.Count == 0)
            {
                sb.Append("  (沒有 machine，StateMachineLogic 還沒 CollectStateMachines？)\n");
                return;
            }

            for (var m = 0; m < machines.Count; m++)
                AppendMachine(sb, logic, machines[m]);
        }

        private static void AppendMachine(StringBuilder sb, StateMachineLogic logic, IMonoStateMachine machine)
        {
            var active = machine.ActiveState as MonoStateBehaviour;
            var entityName = logic.ParentEntity != null ? logic.ParentEntity.name : logic.name;
            sb.Append("[machine] ").Append(entityName).Append('/').Append(machine.Name)
                .Append("  state=").Append(active != null ? active.name : "(none)")
                .Append("  prev=").Append(machine.PreviousState is Object prev ? NodeName(prev) : "-")
                .Append('\n');
            if (active == null)
                return;

            var order = 0;
            var picked = false;
            var transitions = active.Transitions;
            if (transitions != null)
                foreach (var t in transitions)
                    AppendTransition(sb, active, null, t, ref order, ref picked);

            var anyStates = active.BindingAnyStates;
            if (anyStates != null)
                foreach (var anyState in anyStates)
                {
                    if (anyState == null || anyState.Transitions == null)
                        continue;
                    foreach (var t in anyState.Transitions)
                        AppendTransition(sb, active, anyState, t, ref order, ref picked);
                }

            if (order == 0)
                sb.Append("  (沒有 outgoing transition)\n");
            if (!picked)
                sb.Append("  => 目前沒有任何一條會成立\n");
        }

        private static void AppendTransition(
            StringBuilder sb,
            MonoStateBehaviour active,
            MonoStateBehaviour anyState,
            TransitionBehaviour<MonoStateBehaviour> t,
            ref int order,
            ref bool picked
        )
        {
            if (t == null)
                return;
            order++;
            var target = t.TargetState;
            sb.Append("  ").Append(order).Append(". ")
                .Append(anyState != null ? "[Any:" + anyState.name + "] " : "")
                .Append("=> ").Append(target != null ? target.name : "(null target)")
                .Append("  (").Append(t.name).Append(')');

            if (!t.isActiveAndEnabled)
            {
                sb.Append("  disabled，略過\n");
                return;
            }

            if (anyState != null && target == active)
            {
                sb.Append("  target 是目前 state，AnyState 不轉自己，略過\n");
                return;
            }

            AbstractConditionBehaviour[] conditions = null;
            if (t is TransitionBehaviour tb)
                conditions = tb.Conditions;
            else if (t is TransitionRef tr)
                conditions = tr.SourceTransition != null ? tr.SourceTransition.Conditions : null;

            var firstFail = conditions.FirstFailedIndex();
            var condOk = firstFail < 0;
            //跟實際判定同一套：transition 那邊的 CanTransition 問 source(anyState 或自己).CanExit + target.CanEnter，
            //TryActivateState 再問一次目前 state 的 CanExit（含 priority）+ target.CanEnter，且 target 不能是目前 state
            var canExitCurrent = target != null && ((IMonoState)active).CanExitState(target, false);
            var canExitAny = anyState == null || target == null || ((IMonoState)anyState).CanExitState(target, false);
            var canEnter = target != null && ((IMonoState)target).CanEnterState();
            var notSelf = target != active;
            var pass = target != null && condOk && canExitCurrent && canExitAny && canEnter && notSelf;

            sb.Append("  conds=").Append(condOk ? "TRUE" : "FALSE")
                .Append(" canExit=").Append(YN(canExitCurrent));
            if (anyState != null)
                sb.Append(" anyCanExit=").Append(YN(canExitAny));
            sb.Append(" canEnter=").Append(YN(canEnter));
            if (!notSelf)
                sb.Append(" (target == 目前 state，TryActivateState 會拒絕)");
            if (pass && !picked)
            {
                sb.Append("  <= 下個 tick 會走這條");
                picked = true;
            }

            sb.Append('\n');

            if (conditions == null || conditions.Length == 0)
            {
                sb.Append("       (沒有 condition = 一律成立)\n");
                return;
            }

            for (var i = 0; i < conditions.Length; i++)
            {
                var result = conditions.EvaluateAt(i);
                sb.Append("       [").Append(i).Append("] ").Append(result.ToString().PadRight(15))
                    .Append(NodeName(conditions[i]));
                if (i == firstFail)
                    sb.Append("   <- 第一顆失敗（IsAllValid 在這裡短路）");
                sb.Append('\n');
            }
        }

        private static string YN(bool b)
        {
            return b ? "Y" : "N";
        }
    }
}
