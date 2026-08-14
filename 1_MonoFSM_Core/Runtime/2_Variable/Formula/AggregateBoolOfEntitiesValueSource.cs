namespace MonoFSM.Core.Formula
{
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
