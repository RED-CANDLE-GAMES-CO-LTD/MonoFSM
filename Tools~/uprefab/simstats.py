"""`up sim-stats` —— Play Mode 中 WorldUpdateSimulator 註冊了哪些 MonoObj、各自有沒有在跑（需要 Unity）。

查「為什麼每 tick 這麼多 MonoObj 在 simulate」原本只能手寫 execute-dynamic-code
（2026-10-03 culling 調查寫了三支 probe）。這裡把那三支收成一條指令：

- 按 **root entity**（parent 鏈最上層的 MonoObj）的名字分組，`(Clone)` / ` (3)` / `_12` 尾巴去掉，
  同一種 prefab 的多個 instance 併成一列（`×N` 是 root 個數）。
- 欄位定義（跟 MonoObj 的判斷一致，不自己發明）：
  reg  = 在 `_monoObjectSet` 裡的數量
  off  = GO inactive 或 `IsActiveInSimulator == false`（已 despawn 還沒反註冊）
  cull = active 但 `IsSimulationCulling`（handle 鏈關掉 simulation）
  run  = active、沒被 cull、`ShouldSimulte`（這 tick 會跑 Before/Simulate/After）
  upd  = run 且有 IUpdateSimulate（`_updateSimulates` 非空）—— 真正在每 tick 跑 Simulate 的
  hdl  = 自己或 parent 鏈上有 SimulationCullingActiveHandle / CullingActiveHandle 的數量（0 = 永遠不會被 cull）
  dist = active 成員離 CullingGroupProxy `m_DistanceReferencePoint` 的距離範圍（沒有 ref point 就用 Camera.main）
- 全部走反射 / 型別名，不碰 PrefabEditing 的 C#：這條是唯讀診斷，Unity 端不需要 compile。
- CullingGroupProxy 上名字含 `fallback` 的欄位 / 屬性（另一隻 agent 2026-10-03 在加「camera 沒 render 時的
  fallback」）有就印、沒有就不印 —— 不寫死欄位名，免得對方改名這條就壞。
"""

from __future__ import annotations

import unity

USAGE = (
    "用法：up sim-stats [-n N] [--sort run|upd|reg|cull|off] [--filter KW]\n"
    "  Play Mode 中列 WorldUpdateSimulator 註冊的 MonoObj，按 root entity 分組（需要 Unity）\n"
    "  -n N          列前幾組（預設 20，0 = 全部）\n"
    "  --sort        排序欄位（預設 run）\n"
    "  --filter KW   只看 root 名含 KW 的組（不分大小寫）\n"
    "  欄位：reg 註冊 / run 這 tick 會跑 / upd 有 IUpdateSimulate 且在跑 / cull 被 simulation culling 擋掉 /\n"
    "        off inactive 或已 despawn / hdl 鏈上有 culling handle 的數量 / dist 離 culling reference point 的距離"
)

SORT_COL = {"reg": 0, "run": 1, "upd": 2, "cull": 3, "off": 4}

# __LIMIT__ / __SORT__ / __FILTER__ 由 Python 端代換（不用 str.format —— C# 的大括號太多）
CODE = r"""
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MonoFSMCore.Runtime.LifeCycle;
using MonoFSM.Core.Simulate;
if (!Application.isPlaying) return "#NOT-PLAYING";
var bf = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
var setF = typeof(WorldUpdateSimulator).GetField("_monoObjectSet", bf);
var updF = typeof(MonoObj).GetField("_updateSimulates", bf);
if (setF == null) return "#REFLECT-FAIL WorldUpdateSimulator._monoObjectSet";
int limit = __LIMIT__; int sortCol = __SORT__; string filter = __FILTER__;
var sb = new System.Text.StringBuilder();

// culling reference point：第一個 active 且有 ref point 的 CullingGroupProxy
var pT = typeof(MackySoft.Vision.CullingGroupProxy);
var refF = pT.GetField("m_DistanceReferencePoint", bf);
var camF = pT.GetField("m_TargetCamera", bf);
Transform rp = null; string rpFrom = "";
var proxyLines = new List<string>();
foreach (var px in Object.FindObjectsByType<MackySoft.Vision.CullingGroupProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
{
    var r = refF != null ? refF.GetValue(px) as Transform : null;
    var c = camF != null ? camF.GetValue(px) as Camera : null;
    var extra = new List<string>();
    foreach (var f in pT.GetFields(bf))
    {
        if (f.Name.IndexOf("fallback", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
        var ft = f.FieldType;
        if (ft == typeof(bool) || ft.IsEnum || ft == typeof(string) || ft == typeof(int) || ft == typeof(float))
            extra.Add(f.Name + "=" + f.GetValue(px));
    }
    foreach (var p in pT.GetProperties(bf))
    {
        if (p.Name.IndexOf("fallback", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
        if (!p.CanRead || p.GetIndexParameters().Length > 0) continue;
        var ft = p.PropertyType;
        if (!(ft == typeof(bool) || ft.IsEnum || ft == typeof(string) || ft == typeof(int) || ft == typeof(float))) continue;
        try { extra.Add(p.Name + "=" + p.GetValue(px)); } catch { }
    }
    proxyLines.Add("# proxy " + px.transform.root.name + "/" + px.name + (px.isActiveAndEnabled ? "" : "（inactive）")
        + " ref=" + (r ? r.name : "null") + " cam=" + (c ? c.name + (c.isActiveAndEnabled ? "" : "(disabled)") : "null")
        + (extra.Count > 0 ? " " + string.Join(" ", extra) : ""));
    if (rp == null && r != null && px.isActiveAndEnabled) { rp = r; rpFrom = "proxy ref " + r.name; }
}
var refPos = Vector3.zero;
if (rp != null) refPos = rp.position;
else if (Camera.main != null) { refPos = Camera.main.transform.position; rpFrom = "Camera.main（沒有 proxy ref point）"; }
else rpFrom = "原點（沒有 proxy ref point 也沒有 Camera.main）";

var worlds = Object.FindObjectsByType<WorldUpdateSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
var seen = new HashSet<MonoObj>();
var stat = new Dictionary<string, int[]>();          // reg run upd cull off hdl
var dmin = new Dictionary<string, float>();
var dmax = new Dictionary<string, float>();
var roots = new Dictionary<string, HashSet<int>>();
var tot = new int[6];
var rx = new System.Text.RegularExpressions.Regex(@"(\(Clone\))|( \(\d+\))|(_\d+$)");
var worldLines = new List<string>();
foreach (var w in worlds)
{
    var set = setF.GetValue(w) as System.Collections.IEnumerable;
    int wc = 0;
    if (set != null) foreach (var o in set)
    {
        var m = o as MonoObj;
        if (m == null || !seen.Add(m)) continue;
        wc++;
        var chain = m.GetComponentsInParent<MonoObj>(true);
        var top = chain[chain.Length - 1];
        var key = rx.Replace(top.name, "").Trim();
        if (filter != null && key.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
        bool active = m.isActiveAndEnabled && m.IsActiveInSimulator;
        bool culled = active && m.IsSimulationCulling;
        bool run = active && !culled && m.ShouldSimulte;
        var arr = updF != null ? updF.GetValue(m) as System.Array : null;
        bool upd = run && arr != null && arr.Length > 0;
        bool hdl = false;
        foreach (var x in chain) if (x._simulationCullingHandle != null || x._cullingHandle != null) { hdl = true; break; }
        if (!stat.TryGetValue(key, out var g)) { g = new int[6]; stat[key] = g; dmin[key] = float.MaxValue; dmax[key] = -1f; roots[key] = new HashSet<int>(); }
        roots[key].Add(top.GetInstanceID());
        g[0]++; tot[0]++;
        if (run) { g[1]++; tot[1]++; }
        if (upd) { g[2]++; tot[2]++; }
        if (culled) { g[3]++; tot[3]++; }
        if (!active) { g[4]++; tot[4]++; }
        if (hdl) { g[5]++; tot[5]++; }
        if (active)
        {
            var d = Vector3.Distance(refPos, m.transform.position);
            if (d < dmin[key]) dmin[key] = d;
            if (d > dmax[key]) dmax[key] = d;
        }
    }
    worldLines.Add(w.transform.root.name + (w.isActiveAndEnabled ? "" : "(inactive)") + "=" + wc);
}
if (worlds.Length == 0) return "#NO-WORLD";
sb.AppendLine("# world: " + string.Join(", ", worldLines) + "；距離基準：" + rpFrom + (filter != null ? "；filter=" + filter : ""));
sb.AppendLine("  reg  run  upd cull  off  hdl  dist      root entity");
var rows = stat.OrderByDescending(k => k.Value[sortCol]).ThenByDescending(k => k.Value[0]).ToList();
int shown = limit > 0 ? System.Math.Min(limit, rows.Count) : rows.Count;
for (int i = 0; i < shown; i++)
{
    var k = rows[i].Key; var g = rows[i].Value;
    var dist = dmax[k] < 0 ? "-" : ((int)dmin[k]) + "-" + ((int)dmax[k]);
    var label = k.Length > 48 ? k.Substring(0, 47) + "…" : k;
    var n = roots[k].Count;
    sb.AppendLine(g[0].ToString().PadLeft(5) + g[1].ToString().PadLeft(5) + g[2].ToString().PadLeft(5)
        + g[3].ToString().PadLeft(5) + g[4].ToString().PadLeft(5) + g[5].ToString().PadLeft(5)
        + "  " + dist.PadRight(9) + " " + label + (n > 1 ? " ×" + n : ""));
}
if (shown < rows.Count)
{
    var rest = new int[6];
    for (int i = shown; i < rows.Count; i++) for (int j = 0; j < 6; j++) rest[j] += rows[i].Value[j];
    sb.AppendLine(string.Join("", rest.Select(v => v.ToString().PadLeft(5))) + "  " + "".PadRight(9) + " （其餘 " + (rows.Count - shown) + " 組，-n 0 看全部）");
}
sb.AppendLine(string.Join("", tot.Select(v => v.ToString().PadLeft(5))) + "  " + "".PadRight(9) + " = 總計（" + rows.Count + " 組）");
foreach (var l in proxyLines.Take(3)) sb.AppendLine(l);
if (proxyLines.Count > 3) sb.AppendLine("# （另有 " + (proxyLines.Count - 3) + " 個 proxy）");
var cets = Object.FindObjectsByType<MackySoft.Vision.CullingEventTarget>(FindObjectsSortMode.None);
if (cets.Length > 0)
    sb.AppendLine("# CullingEventTarget active=" + cets.Length + " DistanceLevel 分佈 "
        + string.Join(" ", cets.GroupBy(c => c.DistanceLevel).OrderBy(x => x.Key).Select(x => x.Key + ":" + x.Count()))
        + "（-1 = 還沒被 CullingGroup 回報過）");
return sb.ToString();
"""


def cmd(args, root, cfg) -> None:
    code = (CODE.replace("__LIMIT__", str(max(0, args.limit)))
            .replace("__SORT__", str(SORT_COL[args.sort]))
            .replace("__FILTER__", unity.lit(args.filter)))
    out = unity.csharp(code, method="uprefab.simstats.Run")
    if out.startswith("#NOT-PLAYING"):
        raise SystemExit("# 不在 Play Mode：sim-stats 讀的是 runtime 註冊表。先 `up play play`（讀完 `up play stop`）")
    if out.startswith("#NO-WORLD"):
        raise SystemExit("# 場上沒有 WorldUpdateSimulator（Play Mode 剛進去還沒 spawn？等幾秒再跑）")
    if out.startswith("#REFLECT-FAIL"):
        raise SystemExit(f"# {out[1:]} —— 欄位改名了，更新 MonoFSM/Tools~/uprefab/simstats.py 的 CODE")
    print(out.rstrip())


def register(sub) -> None:
    ps = sub.add_parser(
        "sim-stats",
        help="Play Mode 中 WorldUpdateSimulator 註冊的 MonoObj，按 root entity 分組看 run / cull / 距離（需要 Unity）",
        description="查「為什麼每 tick 這麼多 MonoObj 在 simulate」。\n" + USAGE,
        epilog=USAGE,
    )
    ps.add_argument("-n", "--limit", type=int, default=20, help="列前幾組（預設 20，0 = 全部）")
    ps.add_argument("--sort", choices=sorted(SORT_COL), default="run", help="排序欄位（預設 run）")
    ps.add_argument("--filter", metavar="KW", help="只看 root 名含 KW 的組")
    ps.set_defaults(fn=cmd)
