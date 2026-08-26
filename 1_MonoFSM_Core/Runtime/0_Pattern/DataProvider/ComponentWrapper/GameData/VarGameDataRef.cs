using MonoFSM.Core.DataProvider;
using MonoFSM.Foundation;
using MonoFSM.Variable;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MonoFSM.Core.Runtime._0_Pattern.DataProvider.ComponentWrapper
{
    /// <summary>
    /// 指向遠端 VarGameData 的 ValueSource，可寫回來源。
    /// 取代 obsolete 的 VarDescriptableDataRef（那個走 VariableTag + blackboard 查找），
    /// 這裡直接用 DropDownRef 指到目標 VarGameData，同 VarFloatRef / VarBoolRef 的慣例。
    /// 實作 IGameDataProvider，可被 GetMonoPoolObjFromDescriptableData 這類 [Auto] 元件接續取用 bindPrefab。
    /// </summary>
    public class VarGameDataRef
        : AbstractValueSource<GameData>,
            IGameDataProvider,
            IValueSettable<GameData>
    {
        protected override bool HasError()
        {
            // 指向自己或祖先層的 VarGameData 會形成引用環（Value => _dropDownRef.Value）而遞迴爆掉
            if (_dropDownRef != null)
            {
                var parentVars = GetComponentsInParent<VarGameData>(true);
                foreach (var varGameData in parentVars)
                    if (_dropDownRef == varGameData)
                    {
                        _errorMessage = "DropDownRef不能指向自己或父物件上的VarGameData";
                        return true;
                    }
            }

            return base.HasError();
        }

        [Required] [DropDownRef] public VarGameData _dropDownRef;

        public override GameData Value => _dropDownRef != null ? _dropDownRef.Value : null;

        public GameData GameData => Value;

        public override string Description => _dropDownRef?.Description;

        public void SetValue(GameData value, Object byWho = null, string reason = null)
        {
            _dropDownRef?.SetValue(value, byWho, reason);
        }
    }
}
