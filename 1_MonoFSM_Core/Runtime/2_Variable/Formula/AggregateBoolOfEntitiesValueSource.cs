namespace MonoFSM.Core.Formula
{
    /// <summary>
    ///     把 _entities 這串 entity 的 _boolVarTag 用 AND 合併：全部是 true 才回 true（目前沒有 OR 模式）。
    ///     沒有這顆 var、或 var 被 disable / inactive 的 entity 直接跳過不算；清單空的時候回 true；沒設 tag 回 false。
    /// </summary>
    public class AggregateBoolOfEntitiesValueSource : AbstractEntityBoolVarSource<bool>
    {
        //TODO: OR, And?
        //and, 需要 or?
        public override bool Value
        {
            get
            {
                //維持原行為：沒設 tag 直接算 false
                if (_boolVarTag == null)
                    return false;

                var list = GetSourceList();
                if (list == null)
                    return false;

                foreach (var entity in list)
                    //找不到這顆 var 的 entity 不影響 AND 結果（維持原行為）
                    if (TryGetBool(entity, out var isTrue) && !isTrue)
                        return false;

                return true;
            }
        }
    }
}
