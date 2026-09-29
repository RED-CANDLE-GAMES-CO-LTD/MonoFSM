using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoFSM.Core.Simulate;
using MonoFSM.FSM;
using MonoFSM.Runtime.Interact.EffectHit;
using MonoFSMCore.Runtime.LifeCycle;
using UnityEngine;

namespace MonoFSM.Editor.PrefabEditing
{
    /// <summary>
    /// `up hit` 的 Unity 端：Play Mode 下對 scene 上的 GeneralEffectReceiver 打一發 ForceDirectEffectHit，
    /// 走跟 PlayerInteractState 一樣的 CanHitReceiver → IsValid → EnterNode。不直接呼叫，而是排進
    /// TickActionQueue 等下一個 tick 內執行（Fusion 下 networked Var 才不會在 tick 外被寫、被 resim 蓋掉）；
    /// 所以拆成 Hit（排隊、回 id）跟 HitResult（CLI 輪詢）兩段，不卡 Editor main thread。
    /// 沒給 dealer 時自動找本機玩家（MonoObj.HasInputAuthority）身上同 effectType 的 dealer。
    /// </summary>
    public static class EditHit
    {
        private static int _nextId = 1;
        private static readonly Dictionary<int, ForceEffectHitRequest> _requests = new();

        public static string Hit(string receiverPath, string dealerPath, string effectFilter, int maxDeferTicks)
        {
            if (!Application.isPlaying)
                return "# 未執行：hit 只在 Play Mode 有用（要先 `up play play`）";

            try
            {
                var roots = EditResolve.RuntimeRoots();
                GeneralEffectDealer dealer = null;
                if (!string.IsNullOrEmpty(dealerPath))
                    dealer = One(Collect<GeneralEffectDealer>(EditResolve.NodeInRoots(roots, dealerPath)),
                        "dealer", dealerPath, "--dealer 給到那顆 [Dealer] 節點本身");

                var receiverNode = EditResolve.NodeInRoots(roots, receiverPath);
                var receivers = Collect<GeneralEffectReceiver>(receiverNode);
                if (receivers.Count == 0)
                    throw new EditResolve.EditAbort($"'{receiverPath}' 自己跟底下都沒有 GeneralEffectReceiver");

                //跟 PlayerInteractState 的 entity.GetReceiver(dealer._effectType) 同一套：有 dealer 就照它的 type 篩
                if (dealer != null)
                    receivers = receivers.Where(r => r._effectType == dealer._effectType).ToList();
                if (!string.IsNullOrEmpty(effectFilter))
                    receivers = FilterByEffectName(receivers, effectFilter);

                var receiver = PickActive(receivers, "receiver", receiverPath,
                    "直接給 [Receiver] 節點路徑，或加 --effect / --dealer 篩");

                dealer ??= FindLocalDealer(roots, receiver);

                var id = _nextId++;
                var req = new ForceEffectHitRequest(id, receiver, dealer, Math.Max(0, maxDeferTicks));
                if (!TickActionQueue.Enqueue(req))
                    return $"# 未執行：TickActionQueue 滿了（{TickActionQueue.Capacity}），simulator 可能沒在跑";
                _requests[id] = req;
                return $"OK id={id} receiver={FullPath(receiver.transform)} " +
                       $"dealer={FullPath(dealer.transform)} effectType={Name(receiver._effectType)} " +
                       $"fsmTrace={(FsmTrace.Enabled ? "on" : "off")}";
            }
            catch (EditResolve.EditAbort abort)
            {
                return $"# 未執行：{abort.Message}";
            }
        }

        public static string HitResult(int id)
        {
            if (!_requests.TryGetValue(id, out var req))
                return $"UNKNOWN id={id}（domain reload 過、或 id 打錯）";
            if (!req._executed)
                return $"PENDING id={id} deferred={req._deferredTicks}/{req._maxDeferTicks} " +
                       $"reason={req._deferReason ?? "-"} queue={TickActionQueue.PendingCount} " +
                       $"lastDrainTick={TickActionQueue.LastDrainTick}";
            _requests.Remove(id);
            var head = req._timedOut ? "TIMEOUT" : "DONE";
            return $"{head} id={id} tick={req._executedTick} canHit={req._canHit} " +
                   $"deferred={req._deferredTicks}" +
                   (req._deferReason != null ? $"（原因：{req._deferReason}）" : "") +
                   $" failReason={req._failReason ?? "-"}\n" +
                   $"  dealer={(req._dealer != null ? FullPath(req._dealer.transform) : "<destroyed>")}\n" +
                   $"  receiver={(req._receiver != null ? FullPath(req._receiver.transform) : "<destroyed>")}";
        }

        //節點自己有就只拿自己的，不然往下找（跟 debug-effect-trace 一樣可以給祖先）
        private static List<T> Collect<T>(Transform node) where T : Component
        {
            var self = node.GetComponent<T>();
            return self != null ? new List<T> { self } : node.GetComponentsInChildren<T>(true).ToList();
        }

        private static List<GeneralEffectReceiver> FilterByEffectName(List<GeneralEffectReceiver> list, string kw)
        {
            var exact = list.Where(r => string.Equals(Name(r._effectType), kw, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return exact.Count > 0
                ? exact
                : list.Where(r => Name(r._effectType).IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        //剛好一顆 active 就用它；不然列候選 abort
        private static T PickActive<T>(List<T> list, string what, string path, string hint) where T : Behaviour
        {
            var active = list.Where(c => c.isActiveAndEnabled).ToList();
            if (active.Count == 1)
                return active[0];
            throw new EditResolve.EditAbort(Candidates(list, what, path, hint,
                active.Count == 0 ? "沒有 active 的" : $"有 {active.Count} 顆 active"));
        }

        private static T One<T>(List<T> list, string what, string path, string hint) where T : Behaviour
        {
            if (list.Count == 1)
                return list[0];
            throw new EditResolve.EditAbort(Candidates(list, what, path, hint, $"找到 {list.Count} 顆"));
        }

        private static GeneralEffectDealer FindLocalDealer(List<GameObject> roots, GeneralEffectReceiver receiver)
        {
            var sameType = roots.SelectMany(r => r.GetComponentsInChildren<GeneralEffectDealer>(true))
                .Where(d => d != null && d._effectType == receiver._effectType && d.isActiveAndEnabled)
                .ToList();
            //有網路 provider 的只留本機（InputAuthority）；單機沒有 provider，authority 問不出來，全部留著
            var local = sameType.Where(d =>
            {
                var obj = d.GetComponentInParent<MonoObj>(true);
                return obj == null || !obj.HasAuthorityProvider || obj.HasInputAuthority;
            }).ToList();
            if (local.Count == 1)
                return local[0];
            throw new EditResolve.EditAbort(Candidates(local.Count > 0 ? local : sameType, "本機玩家的 dealer",
                "effectType=" + Name(receiver._effectType), "用 --dealer 指定",
                local.Count == 0 ? "沒有本機（InputAuthority）的" : $"本機有 {local.Count} 顆"));
        }

        private static string Candidates<T>(List<T> list, string what, string path, string hint, string why)
            where T : Behaviour
        {
            var sb = new StringBuilder();
            sb.Append($"{what} 不是剛好一顆（{why}，'{path}'）。{hint}。候選：");
            foreach (var c in list.Take(12))
            {
                sb.Append("\n  ").Append(c.isActiveAndEnabled ? "[active]   " : "[inactive] ")
                    .Append(FullPath(c.transform));
                if (c is EffectResolver er)
                    sb.Append("  effectType=").Append(Name(er._effectType));
            }

            if (list.Count > 12)
                sb.Append($"\n  …另外 {list.Count - 12} 顆");
            return sb.ToString();
        }

        private static string Name(UnityEngine.Object o)
        {
            return o != null ? o.name : "(null)";
        }

        private static string FullPath(Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
