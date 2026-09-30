using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Attributes;
using MonoFSM.Foundation;
using MonoFSM.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MonoFSM.Variable
{
    /// <summary>
    ///     讓 module 的 Var 同時支援兩種規格：往上找 ancestor entity（跳過 module 自己）有沒有同 _varTag 的 var，
    ///     有 → 新規格，host 的值一變就抄進來（掛在 Var 的子節點，零設定；不要跟 Var 同一顆 GameObject，
    ///     兩個 AbstractDescriptionBehaviour 會搶自動命名，Var 節點名會被改掉）；
    ///     沒有 → 舊規格，完全不動，Var 照舊吃自己的預設值和別人對它的 SetVar。
    ///     ex: 落雷 module 的 Attack Enabled，在神像上跟著神像的 Attack Enabled，在舊敵人上照舊用自己的。
    ///     寫入走 Var.SetValueFromVar，非 StateAuthority 端會被 Var 自己擋掉，所以每一端都掛著也沒關係，值由 SA 同步出去。
    /// </summary>
    public class VarMirrorFromHostEntity : AbstractDescriptionBehaviour, ISceneStart, IVarChangedListener
    {
        public enum MirrorStatus
        {
            NotStarted,
            Mirroring,
            NoOwnerVar,
            NoVarTag,
            NoOwnerEntity,
            NoHostVar, // 舊規格：上面沒有 host var，不動
        }

        [AutoParent] private AbstractMonoVariable _ownerVar;
        [AutoParent] private MonoEntity _ownerEntity;

        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private MirrorStatus _status = MirrorStatus.NotStarted;

        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private AbstractMonoVariable _hostVar;

        [ShowInInspector] [Sirenix.OdinInspector.ReadOnly]
        private MonoEntity _hostEntity => _hostVar != null ? _hostVar.GetComponentInParent<MonoEntity>() : null;

        public override string Description =>
            $"Mirror [{(_ownerVar != null && _ownerVar._varTag != null ? _ownerVar._varTag.name : "?")}] from host entity";

        public void EnterSceneStart()
        {
            if (_ownerVar == null)
            {
                _status = MirrorStatus.NoOwnerVar;
                return;
            }
            if (_ownerVar._varTag == null)
            {
                _status = MirrorStatus.NoVarTag;
                return;
            }
            if (_ownerEntity == null)
            {
                _status = MirrorStatus.NoOwnerEntity;
                return;
            }

            _hostVar = FindHostVar();
            if (_hostVar == null)
            {
                _status = MirrorStatus.NoHostVar;
                return;
            }

            _hostVar.AddListener(this);
            _status = MirrorStatus.Mirroring;
            _ownerVar.SetValueFromVar(_hostVar, this);
        }

        // 從 module 自己的 entity 開始往上找，第一個「同 tag 且不是自己那顆 Var」的就是 host
        private AbstractMonoVariable FindHostVar()
        {
            var entity = _ownerEntity;
            while (entity != null)
            {
                var v = entity.GetVar(_ownerVar._varTag);
                if (v != null && v != _ownerVar)
                    return v;
                var parent = entity.transform.parent;
                entity = parent != null ? parent.GetComponentInParent<MonoEntity>(true) : null;
            }
            return null;
        }

        public void OnVarChanged(AbstractMonoVariable variable)
        {
            if (_ownerVar != null)
                _ownerVar.SetValueFromVar(variable, this);
        }

        private void OnDestroy()
        {
            if (_hostVar != null)
                _hostVar.RemoveListener(this);
        }
    }
}
