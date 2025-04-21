namespace MonoFSM.Condition
{
    /// <summary>
    /// 用來監聽Condition的變化,當Condition改變時會被通知
    /// </summary>
    public interface IConditionChangeListener
    {
        void OnConditionChanged();
    }
}