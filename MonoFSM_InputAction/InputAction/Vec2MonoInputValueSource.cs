using MonoFSM_InputAction;
using MonoFSM.Foundation;
using UnityEngine;

namespace Fusion.Addons.KCC.ECM2.Examples.Networking.Fusion_v2.Characters.Scripts.Input
{
    public class Vec2MonoInputValueSource : AbstractValueSource<Vector2>
    {
        //怎麼寫一個很容易拿到action的ValueSource? Condition,,,,
        [SerializeField]
        [DropDownRef]
        private MonoInputAction _inputAction;

        public override string Description =>
            _inputAction != null ? _inputAction.name : "No InputAction";

        // protected override string DescriptionTag => "Vec2";
        public override Vector2 Value =>
            Application.isPlaying ? _inputAction.ReadValueVec2 : Vector2.zero;
    }
}
