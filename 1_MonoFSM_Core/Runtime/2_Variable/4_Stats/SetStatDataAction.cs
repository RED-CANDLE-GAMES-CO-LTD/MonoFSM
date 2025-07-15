using MonoFSM.Core.Runtime.Action;

namespace MonoFSM.Runtime.FSM._4_Stats
{
    // 在 menu 塞 FSM
    // 用 FSM 調整難度，選難度的 ACTION?
    // 還是應該在 scriptable 就 data driven...嗎
    // routing 要 condition，一定要有狀態 cache 比較好
    // DataBool 劇情模式
    public class SetStatDataAction : AbstractStateAction
    {
        public StatDataRef TargetStatData;
        public StatData SourceStatData;

        protected override void OnActionExecuteImplement()
        {
            TargetStatData.BindingStatData = SourceStatData;
        }
    }
}