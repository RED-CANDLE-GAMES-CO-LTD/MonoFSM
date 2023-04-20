using System.Diagnostics;

namespace RCGMaker.Core
{
    public interface IAnimatorPlayAction
    {
#if UNITY_EDITOR
        public void EditClip();
#endif
    }
}