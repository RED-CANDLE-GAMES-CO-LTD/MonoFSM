using System.Globalization;
using MonoFSM.Core.Attributes;
using MonoFSM.Core.DataProvider;
using MonoFSM.Core.Simulate;
using MonoFSM.EditorExtension;
using MonoFSM.Variable.Attributes;
using MonoFSM.Variable.FieldReference;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//CountdownTimer...直接掛在這個下面？
namespace MonoFSM.Variable
{
    /// <summary>
    /// 讓 Variable 在 ResetStateRestore 時參考此 interface 來決定還原值
    /// </summary>
    public interface IRestoreValueOverrider<T>
    {
        bool ShouldOverrideRestoreValue { get; }
        T GetRestoreValue();
    }


    /// <summary>
    /// A MonoBehaviour representation of a float variable that can be bound to scriptable data.
    /// This class provides functionality for float values that can be accessed, modified, and tracked
    /// across the application.
    /// </summary>
    public class VarFloat
        : AbstractFieldVariable<GameDataFloat, FlagFieldFloat, float>,
            ISerializedFloatValue, IStringTokenVar
    {
        public bool IsDirty => CurrentValue != LastValue; //這樣只會一個frame耶？完全不用resolve啊...?

        //FIXME: 需要一個reset value source? 回到maxValue or minValue之類的...?
        // public override GameFlagBase FinalData => BindData;

        [Auto] VarFloatChangeModifier _changeModifier; //SetValue要過這個嗎，感覺很容易出bug

        public void AddBy(float delta, Object byWhot)
        {
            if (_changeModifier != null)
            {
                delta = _changeModifier.ProcessDelta(delta);
                // Debug.Log(
                //     $"VarFloat '{name}' AddBy: delta={delta}, byWhot={byWhot}, CurrentValue={CurrentValue}",
                //     this
                // );
            }

            SetValue(Value + delta, byWhot, "ChangeBy");
        }

        [ShowInDebugMode]
        public int IntValue => Mathf.CeilToInt(CurrentValue);

        [ShowInPlayMode]
        public float Percentage => (CurrentValue - Min) / (Max - Min);

        //FIXME: 要editor time的時候GetComponent嗎？
        [ShowInPlayMode]
        public float Min
        {
            get
            {
                var varRefFloat = varRef as VarFloat;
                if (varRefFloat != null)
                {
                    return varRefFloat.Min;
                }
                if (valueSource is IFloatBoundProvider boundProvider)
                    return boundProvider.Min;
                return _boundModifier ? _boundModifier.MinValue : 0;
            }
        }

        [ShowInPlayMode]
        public float Max
        {
            get
            {
                var varRefFloat = varRef as VarFloat;
                if (varRefFloat != null)
                {
                    return varRefFloat.Max;
                }
                if (valueSource is IFloatBoundProvider boundProvider)
                    return boundProvider.Max;
                return _boundModifier ? _boundModifier.MaxValue : float.MaxValue;
            }
        }

        public override void OnBeforePrefabSave()
        {
            base.OnBeforePrefabSave();
            if (_boundModifier != null)
            {
                //FIXME: 蛤？
                // Field.ResetToDefault();
                Field.Init(TestMode.Production, this);
                _boundModifier.EditorBoundCheck(ref Field.ProductionValue);
                _boundModifier.EditorBoundCheck(ref Field.DevValue);
                Debug.Log(
                    $"VarFloat OnBeforePrefabSave: Min={Min}, Max={Max}, CurrentValue={CurrentValue}",
                    this
                );
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        public bool IsMax => CurrentValue >= Max;
        public bool IsMin => CurrentValue <= Min;

        [ShowInDebugMode]
        public bool IsDecreasing =>
            _lastDecreasingTime > 0 &&
            WorldUpdateSimulator.SimulationTime - _lastDecreasingTime < 0.2f;

        [ShowInDebugMode]
        private float _lastDecreasingTime;

        /// <summary>
        /// 把值寫入，表示
        /// </summary>
        /// <param name="lastValue"></param>
        /// <param name="currentValue"></param>
        protected override void OnValueSet(float oldValue, float newValue)
        {
            if (newValue < oldValue)
                _lastDecreasingTime = WorldUpdateSimulator.SimulationTime;
        }

        [ShowInDebugMode]
        public bool IsIncreasing => CurrentValue > LastValue;

        [InfoBox(
            "已勾選 BoundModifier 的 isResetToMaxOnRestore：Restore 時會直接還原成 Max，" +
            "下方 Field.ProductionValue 在 runtime 不會生效（僅剩 editor 顯示用途）。",
            InfoMessageType.Warning,
            nameof(_isResetToMaxOnRestoreActive))]
        [Component(AddComponentAt.Same)]
        [AutoChildren(false)] //[PreviewInInspector]
        [SerializeField]
        private VariableFloatBoundModifier _boundModifier; //FIXME: Nested Prefab時會有髒髒狀態？ 還是要Editor都寫GetComponent...?

        /// <summary>
        /// BoundModifier 勾了 isResetToMaxOnRestore 時，Restore 會把值蓋成 Max，
        /// 此時 Field.ProductionValue 無意義。給 InfoBox 判斷是否顯示警告用。
        /// </summary>
        private bool _isResetToMaxOnRestoreActive =>
            _boundModifier != null && _boundModifier._isResetToMaxOnRestore;


        [CompRef] [AutoChildren(false)]
        private IRestoreValueOverrider<float> _restoreValueOverrider;

        // [PreviewInInspector] [Component] [AutoChildren]
        // AbstractVariableModifier<float>[] _setOperations;

        // [Button]
        // void TestAdd(float value)
        // {
        //     Value += value;
        // }
        // public float Value => CurrentValue;

        public override string ValueInfo
        {
            get
            {
                // Editor time 的 CurrentValue 其實是 Field.ProductionValue（設計預設值），
                // 不是 runtime 真值，標明「預設」避免誤會。
                if (!Application.isPlaying)
                {
                    var editorValue = CurrentValue.ToString(CultureInfo.CurrentCulture);
                    // 若 restore 會被蓋成 Max，額外提示開場真值，避免以為 ProductionValue 會生效
                    if (_isResetToMaxOnRestoreActive)
                        return $"預設 {editorValue}（Restore→Max {Max.ToString(CultureInfo.CurrentCulture)}）";
                    return $"預設 {editorValue}";
                }

                return CurrentValue.ToString(CultureInfo.CurrentCulture);
            }
        }
        public override bool IsDrawingValueInfo => true;

        public override bool IsValueExist => Field.CurrentValue != 0f; //  CurrentValue != 0f;

        public override void ResetStateRestore(bool IsHardReset)
        {
            base.ResetStateRestore(false);

            // 如果有 overrider 且需要覆蓋，使用 overrider 提供的值
            if (_restoreValueOverrider is not { ShouldOverrideRestoreValue: true }) return;


            var restoreValue = _restoreValueOverrider.GetRestoreValue();
            // Debug.Log(
            //     $"VarFloat '{name}' resetting state restore with overrider value: {restoreValue}",
            //     this
            // );
            SetValue(restoreValue, this, "RestoreValueOverrider");
        }
    }
}
