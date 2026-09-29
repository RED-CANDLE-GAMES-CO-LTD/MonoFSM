"""`up controller <path.controller | .overrideController>`：AnimatorController 的結構摘要（離線）。

為什麼要有：layer / state / transition / WriteDefaults 以前只能 grep .controller，
而 transition 是獨立 document、只存 fileID，grep 出來根本看不出「誰 → 誰、什麼條件」
（2026-09-29 StreetLamp emission 調查）。

- .controller：parameters → 每個 layer（weight / blending / mask / synced）→ state
  （motion、speed、WD、behaviours）→ transition（目標、conditions、exit time、duration）。
- .overrideController：base controller + 原 clip → override clip 對照，並列出原 clip
  在 base controller 裡被哪些 layer/state 用到（看得出「換掉的是哪一格」）。
"""

from __future__ import annotations

import os

import assetyaml as ay

USAGE = (
    "用法：up controller <path.controller | path.overrideController> [--layer KW] [--budget N]\n"
    "  path        AnimatorController / AnimatorOverrideController（只給檔名片段會列候選）\n"
    "  --layer KW  只列名稱含 KW 的 layer（.controller 用）\n"
    "  --budget N  輸出字元上限（預設 8000，0 = 不限）\n"
    "例：up controller \"Assets/0_Gameplay/Physics Object/Interact Device.controller\"\n"
    "    up controller \"Assets/0_Gameplay/Safe Zone 小廟安全點/Safe Zone 小廟安全點.overrideController\""
)

PARAM_TYPES = {"1": "Float", "3": "Int", "4": "Bool", "9": "Trigger"}
MODES = {"1": "If", "2": "IfNot", "3": ">", "4": "<", "6": "==", "7": "!="}
BLEND = {"0": "Override", "1": "Additive"}


class Ctl:
    def __init__(self, root: str, path: str):
        self.root = root
        self.rel = os.path.relpath(path, root)
        self.docs = {d.file_id: d for d in ay.load(path)}
        self.main = next((d for d in self.docs.values() if d.class_id == 91), None)
        self.params: dict[str, str] = {}
        if self.main:
            for p in self.main.body.get("m_AnimatorParameters") or []:
                self.params[p.get("m_Name", "?")] = PARAM_TYPES.get(str(p.get("m_Type")), "?")
        self._motion_guids: set[str] = set()
        self._script_guids: set[str] = set()

    def body(self, fid: int) -> dict:
        d = self.docs.get(fid)
        return d.body if d else {}

    def name(self, fid: int) -> str:
        return self.body(fid).get("m_Name") or f"#{fid}"

    # ---- 先掃一遍收 guid，一次解完 ----
    def collect(self):
        for d in self.docs.values():
            b = d.body
            if d.class_id == 1102:
                _, g = ay.ref(b.get("m_Motion"))
                if g:
                    self._motion_guids.add(g)
            elif d.class_id == 206:
                for c in b.get("m_Childs") or []:
                    _, g = ay.ref(c.get("m_Motion"))
                    if g:
                        self._motion_guids.add(g)
            elif d.class_id == 114:
                _, g = ay.ref(b.get("m_Script"))
                if g:
                    self._script_guids.add(g)
        if self.main:
            for L in self.main.body.get("m_AnimatorLayers") or []:
                for m in L.get("m_Motions") or []:
                    _, g = ay.ref(m.get("m_Motion"))
                    if g:
                        self._motion_guids.add(g)
                _, g = ay.ref(L.get("m_Mask"))
                if g:
                    self._motion_guids.add(g)
        self.wd_all = {d.body.get("m_WriteDefaultValues", "1") for d in self.docs.values() if d.class_id == 1102}
        self.paths = ay.resolve_guids(self.root, self._motion_guids)
        self.scripts = _script_names(self.root, self._script_guids)

    def motion(self, r) -> str:
        fid, g = ay.ref(r)
        if not fid and not g:
            return "（無 motion）"
        if not g:
            bt = self.body(fid)
            if self.docs.get(fid) and self.docs[fid].class_id == 206:
                kids = []
                two_d = str(bt.get("m_BlendType", "0")) not in ("0", "4")  # 0=1D, 4=Direct
                childs = bt.get("m_Childs") or []
                for c in childs[:4]:
                    if two_d:
                        pos = c.get("m_Position") or {}
                        at = f"({ay.fmt_num(ay.num(pos.get('x')))},{ay.fmt_num(ay.num(pos.get('y')))})"
                    else:
                        at = ay.fmt_num(ay.num(c.get("m_Threshold")))
                    kids.append(f"{self.motion(c.get('m_Motion'))}@{at}")
                if len(childs) > 4:
                    kids.append(f"… 共 {len(childs)} 個")
                p = bt.get("m_BlendParameter") or "?"
                p2 = bt.get("m_BlendParameterY")
                ptxt = p + (f",{p2}" if p2 and two_d else "")
                return f"BlendTree「{bt.get('m_Name', '')}」({ptxt}) [{', '.join(kids)}]"
            return f"fileID {fid}"
        return clip_label(self.root, self.paths.get(g), fid, g)

    def cond(self, c: dict) -> str:
        p = c.get("m_ConditionEvent", "?")
        mode = str(c.get("m_ConditionMode"))
        t = self.params.get(p)
        th = ay.fmt_num(ay.num(c.get("m_EventTreshold")))
        if t == "Trigger":
            return f"{p}(trigger)"
        if mode == "1":
            return p if t == "Bool" else f"{p} If"
        if mode == "2":
            return f"!{p}" if t == "Bool" else f"{p} IfNot"
        op = MODES.get(mode, f"mode{mode}")
        miss = "" if t else "⚠沒這個 parameter"
        return f"{p} {op} {th}{miss}"

    def transition(self, fid: int) -> str:
        b = self.body(fid)
        if not b:
            return f"→ ⚠ transition #{fid} 不見了"
        if b.get("m_IsExit") == "1":
            dst = "Exit"
        else:
            sf, _ = ay.ref(b.get("m_DstState"))
            mf, _ = ay.ref(b.get("m_DstStateMachine"))
            dst = self.name(sf) if sf else (f"SM:{self.name(mf)}" if mf else "?")
        conds = [self.cond(c) for c in b.get("m_Conditions") or []]
        bits = []
        if conds:
            bits.append(" && ".join(conds))
        if b.get("m_HasExitTime") == "1":
            bits.append(f"exitTime {ay.fmt_num(ay.num(b.get('m_ExitTime')))}")
        elif not conds:
            bits.append("⚠ 沒條件也沒 exitTime（不會觸發）")
        dur = ay.num(b.get("m_TransitionDuration"))
        if dur:
            bits.append(f"dur {ay.fmt_num(dur)}{'s' if b.get('m_HasFixedDuration') == '1' else '×'}")
        if b.get("m_Mute") == "1":
            bits.append("MUTE")
        if b.get("m_Solo") == "1":
            bits.append("SOLO")
        it = str(b.get("m_InterruptionSource", "0"))
        if it != "0":
            bits.append(f"interrupt={ {'1': 'Source', '2': 'Dest', '3': 'Src→Dst', '4': 'Dst→Src'}.get(it, it) }")
        return f"→ {dst}" + (f"  [{'; '.join(bits)}]" if bits else "")

    def behaviours(self, refs) -> str:
        names = []
        for r in refs or []:
            fid, _ = ay.ref(r)
            _, sg = ay.ref(self.body(fid).get("m_Script"))
            names.append(self.scripts.get(sg, f"script {sg[:8] if sg else '?'}…"))
        return ", ".join(names)


def _script_names(root: str, guids: set[str]) -> dict[str, str]:
    if not guids:
        return {}
    try:
        import indexer
        con = indexer.connect(root)
        q = ",".join("?" * len(guids))
        return {g: c for g, c in con.execute(f"SELECT guid, class FROM scripts WHERE guid IN ({q})", tuple(guids))}
    except Exception:
        return {}


def clip_label(root: str, rel: str | None, fid: int, guid: str | None) -> str:
    if not rel:
        return f"⚠ missing clip (guid {guid})"
    base = os.path.basename(rel)
    if rel.lower().endswith(".anim"):
        return base
    n = ay.fbx_clip_name(root, rel, fid)
    return f"{base}:{n or fid}"


def _walk_sm(ctl: Ctl, sm_fid: int, out: list, depth: int, prefix: str, wd: set, motion_over: dict | None):
    sm = ctl.body(sm_fid)
    ind = "  " * depth
    dflt, _ = ay.ref(sm.get("m_DefaultState"))
    beh = ctl.behaviours(sm.get("m_StateMachineBehaviours"))
    if beh:
        out.append(f"{ind}  behaviours（state machine 層）：{beh}")
    for cs in sm.get("m_ChildStates") or []:
        sf, _ = ay.ref(cs.get("m_State"))
        st = ctl.body(sf)
        if not st:
            out.append(f"{ind}  ⚠ state #{sf} 不見了")
            continue
        wd.add(st.get("m_WriteDefaultValues", "1"))
        bits = []
        if motion_over is not None:
            bits.append(f"motion={motion_over.get(sf, '（沒覆寫，空）')}")
        else:
            bits.append(f"motion={ctl.motion(st.get('m_Motion'))}")
        sp = ay.num(st.get("m_Speed"), 1)
        if st.get("m_SpeedParameterActive") == "1":
            bits.append(f"speed={ay.fmt_num(sp)}×{st.get('m_SpeedParameter')}")
        elif sp != 1:
            bits.append(f"speed={ay.fmt_num(sp)}")
        if len(ctl.wd_all) > 1:  # 全部一樣就只在表頭講一次
            bits.append("WD" + ("on" if st.get("m_WriteDefaultValues", "1") == "1" else "off"))
        if st.get("m_Tag"):
            bits.append(f"tag={st.get('m_Tag')}")
        if st.get("m_Mirror") == "1":
            bits.append("mirror")
        b2 = ctl.behaviours(st.get("m_StateMachineBehaviours"))
        if b2:
            bits.append(f"behaviours: {b2}")
        mark = "●" if sf == dflt else "○"
        out.append(f"{ind}  {mark} {prefix}{st.get('m_Name', '?')}  " + "  ".join(bits))
        if motion_over is None:
            for t in st.get("m_Transitions") or []:
                tf, _ = ay.ref(t)
                out.append(f"{ind}      {ctl.transition(tf)}")
    if motion_over is None:
        for t in sm.get("m_AnyStateTransitions") or []:
            tf, _ = ay.ref(t)
            out.append(f"{ind}  AnyState {ctl.transition(tf)}")
        for t in sm.get("m_EntryTransitions") or []:
            tf, _ = ay.ref(t)
            out.append(f"{ind}  Entry {ctl.transition(tf)}")
    for csm in sm.get("m_ChildStateMachines") or []:
        cf, _ = ay.ref(csm.get("m_StateMachine"))
        out.append(f"{ind}  ▸ sub state machine「{ctl.name(cf)}」")
        _walk_sm(ctl, cf, out, depth + 1, prefix + ctl.name(cf) + "/", wd, motion_over)


def _run_controller(args, path: str, root: str) -> None:
    ctl = Ctl(root, path)
    if not ctl.main:
        raise SystemExit(f"# {ctl.rel} 裡沒有 AnimatorController document\n{USAGE}")
    ctl.collect()
    mb = ctl.main.body
    print(f"# controller: {mb.get('m_Name', '?')}  ({ctl.rel})")
    ps = []
    for p in mb.get("m_AnimatorParameters") or []:
        t = PARAM_TYPES.get(str(p.get("m_Type")), "?")
        d = {"Float": ay.fmt_num(ay.num(p.get("m_DefaultFloat"))), "Int": str(p.get("m_DefaultInt")),
             "Bool": "true" if p.get("m_DefaultBool") == "1" else "false"}.get(t)
        ps.append(f"{p.get('m_Name')}:{t}" + (f"={d}" if d and d not in ("0", "false") else ""))
    print(f"# parameters {len(ps)} 個：{', '.join(ps) if ps else '（無）'}")
    layers = mb.get("m_AnimatorLayers") or []
    kw = (args.layer or "").lower()
    out: list[str] = []
    wd: set[str] = set()
    for i, L in enumerate(layers):
        name = L.get("m_Name", "?")
        if kw and kw not in name.lower():
            continue
        bits = [f"weight={'1(第 0 層固定)' if i == 0 else ay.fmt_num(ay.num(L.get('m_DefaultWeight')))}",
                BLEND.get(str(L.get("m_BlendingMode")), "?")]
        mf, mg = ay.ref(L.get("m_Mask"))
        if mg:
            bits.append(f"mask={os.path.basename(ctl.paths.get(mg, mg))}")
        if L.get("m_IKPass") == "1":
            bits.append("IK")
        sync = int(ay.num(L.get("m_SyncedLayerIndex"), -1))
        if sync >= 0:
            src = layers[sync].get("m_Name", "?") if sync < len(layers) else "?"
            bits.append(f"synced ← layer {sync}「{src}」" + ("（含 timing）" if L.get("m_SyncedLayerAffectsTiming") == "1" else ""))
        out.append(f"## layer {i}「{name}」 " + "  ".join(bits))
        if sync >= 0 and sync < len(layers):
            over = {}
            for m in L.get("m_Motions") or []:
                sf, _ = ay.ref(m.get("m_State"))
                over[sf] = ctl.motion(m.get("m_Motion"))
            sf0, _ = ay.ref(layers[sync].get("m_StateMachine"))
            out.append("  （synced layer：state / transition 跟來源 layer 一樣，這裡只列各 state 的 motion 覆寫）")
            _walk_sm(ctl, sf0, out, 0, "", set(), over)
            continue
        sf, _ = ay.ref(L.get("m_StateMachine"))
        _walk_sm(ctl, sf, out, 0, "", wd, None)
    if len(ctl.wd_all) > 1:
        print("# ⚠ WriteDefaults 混用（有 on 有 off，每個 state 標 WDon/WDoff）—— 常見的「值沒回復 / 殘留」來源")
    elif ctl.wd_all:
        print(f"# WriteDefaults 全部 {'on' if '1' in ctl.wd_all else 'off'}")
    print("# 圖例：● default state  ○ 其他；→ transition [條件; exitTime; dur(s=秒 ×=比例)]")
    budget = ay.Budget(args.budget, "用 --layer KW 只看一層")
    for ln in out:
        budget.emit(ln)
    budget.finish()


def _run_override(args, path: str, root: str) -> None:
    rel = os.path.relpath(path, root)
    doc = next((d for d in ay.load(path) if d.class_id == 221), None)
    if not doc:
        raise SystemExit(f"# {rel} 裡沒有 AnimatorOverrideController document\n{USAGE}")
    b = doc.body
    bf, bg = ay.ref(b.get("m_Controller"))
    pairs = []
    guids = {bg} if bg else set()
    for c in b.get("m_Clips") or []:
        of, og = ay.ref(c.get("m_OriginalClip"))
        vf, vg = ay.ref(c.get("m_OverrideClip"))
        pairs.append((of, og, vf, vg))
        guids.update(g for g in (og, vg) if g)
    paths = ay.resolve_guids(root, guids)
    base_rel = paths.get(bg) if bg else None
    print(f"# overrideController: {b.get('m_Name', '?')}  ({rel})")
    if not base_rel:
        print(f"# ⚠ base controller 找不到（guid {bg}）")
    else:
        print(f"# base controller: {base_rel}（結構看 `up controller \"{base_rel}\"`）")
    # 原 clip 在 base 裡被哪些 state 用到
    uses: dict[str, list[str]] = {}
    if base_rel and base_rel.lower().endswith(".controller"):
        ctl = Ctl(root, ay.disk_path(root, base_rel))
        if ctl.main:
            for i, L in enumerate(ctl.main.body.get("m_AnimatorLayers") or []):
                lname = L.get("m_Name", "?")
                for m in L.get("m_Motions") or []:
                    sf, _ = ay.ref(m.get("m_State"))
                    _, g = ay.ref(m.get("m_Motion"))
                    if g:
                        uses.setdefault(g, []).append(f"{lname}/{ctl.name(sf)}")
                sf0, _ = ay.ref(L.get("m_StateMachine"))
                if int(ay.num(L.get("m_SyncedLayerIndex"), -1)) >= 0:
                    continue
                stack = [sf0]
                while stack:
                    sm = ctl.body(stack.pop())
                    for cs in sm.get("m_ChildStates") or []:
                        s, _ = ay.ref(cs.get("m_State"))
                        _, g = ay.ref(ctl.body(s).get("m_Motion"))
                        if g:
                            uses.setdefault(g, []).append(f"{lname}/{ctl.name(s)}")
                    for csm in sm.get("m_ChildStateMachines") or []:
                        stack.append(ay.ref(csm.get("m_StateMachine"))[0])
    n_over = sum(1 for p in pairs if p[3])
    print(f"# clip 對照 {len(pairs)} 組（{n_over} 組有覆寫）：原 clip → override clip   [原 clip 在 base 用在哪]")
    budget = ay.Budget(args.budget, "")
    for of, og, vf, vg in pairs:
        o = clip_label(root, paths.get(og), of, og) if og else "（空）"
        v = clip_label(root, paths.get(vg), vf, vg) if vg else "（沒覆寫，用原 clip）"
        where = uses.get(og or "", [])
        wtxt = f"   [{', '.join(where[:4])}{' …' if len(where) > 4 else ''}]" if where else \
            ("   [⚠ base 裡沒有 state 用這支 clip]" if uses else "")
        budget.emit(f"  {o} → {v}{wtxt}")
    budget.finish()
    vs = [paths.get(p[3]) for p in pairs if p[3] and paths.get(p[3], "").endswith(".anim")]
    if vs:
        print(f"# 看 override clip 設了什麼值：up anim \"{vs[0]}\" --values")


def run(args, root: str) -> None:
    path = ay.find_file(root, args.path, (".controller", ".overridecontroller"), "controller", USAGE)
    if path.lower().endswith(".overridecontroller"):
        _run_override(args, path, root)
    else:
        _run_controller(args, path, root)


def register(sub) -> None:
    pc = sub.add_parser(
        "controller",
        help="AnimatorController 結構（parameters / layer / state / transition / WD）；"
             ".overrideController 印 base + clip 對照（離線）",
        description="讀 .controller 印 parameters、每個 layer 的 state（motion / speed / WriteDefaults / behaviours）"
                    "與 transition（目標 / conditions / exitTime / duration）；.overrideController 印 base controller"
                    " 與原 clip → override clip 對照。\n" + USAGE,
        epilog=USAGE)
    pc.add_argument("path", help=".controller / .overrideController 路徑（或檔名片段）")
    pc.add_argument("--layer", metavar="KW", help="只列名稱含 KW 的 layer")
    pc.add_argument("--budget", type=int, default=8000, help="輸出字元上限（預設 8000，0 = 不限）")
    pc.set_defaults(fn=lambda args, root, cfg: run(args, root))
