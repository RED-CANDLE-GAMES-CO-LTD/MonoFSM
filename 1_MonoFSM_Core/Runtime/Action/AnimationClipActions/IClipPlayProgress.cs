namespace MonoFSM.Animation
{
    /// <summary>
    /// tick-based clip 播放進度（AnimationClipPlayAction / RootMotionClipMoveAction 共用），
    /// 給 AnimationClipPlayDoneCondition 等 done transition 條件使用。
    /// </summary>
    public interface IClipPlayProgress
    {
        bool IsDone { get; }
        bool IsProgressPassedRatio(float ratio);
    }
}
