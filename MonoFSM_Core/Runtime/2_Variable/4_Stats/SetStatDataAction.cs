using MonoFSM_Core.Runtime.Action;

namespace RCGMaker.Runtime.FSM._4_Stats
{
    //在menu塞FSM  //用FSM調整難度，選難度的ACTION?
    //還是應該在scriptable就data driven...嗎
    //routing要condition，一定要有狀態cache比較好
    //DataBool 劇情模式
    public class SetStatDataAction : AbstractStateAction
    {
        public StatDataRef TargetStatData;
        public StatData SourceStatData;

        protected override void OnStateEnterImplement()
        {
            TargetStatData.BindingStatData = SourceStatData;
        }
    }
}