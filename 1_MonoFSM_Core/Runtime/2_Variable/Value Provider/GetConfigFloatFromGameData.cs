using MonoFSM.Foundation;
using MonoFSM.Variable;

namespace MonoFSM.Core.DataProvider
{
    //取出 GameData 上任意 tag 的 config float（GameData.Config 那張表），讓 VarFloat 能拿到「目前選取 GameData 的某項數值」
    //跟 GetPriceFromGameData 的差別：Price 是固定欄位，這裡是用 VariableTag 查 config 表，可以指到任何一個 config entry
    public class GetConfigFloatFromGameData : AbstractValueSource<float>
    {
        public VarGameData _gameData;
        public VariableTag _configTag;

        public override bool HasValue =>
            _gameData != null
            && _gameData.Value != null
            && _configTag != null
            && _gameData.Value.HasConfig(_configTag);

        public override float Value =>
            HasValue && _gameData.Value.TryGetConfig(_configTag, out var value) ? value : 0f;

        public override string Description =>
            $"{(_gameData != null ? _gameData.name : "?")}.Config[{(_configTag != null ? _configTag.name : "?")}]";
    }
}
