namespace MonoFSM.Core
{
    public class EditorResetHandler : AbstractEventHandler, IEditorResetToPlayTest
    {
        public void OnEditorResetToPlayTest()
        {
            EventHandle();
        }
    }
}
