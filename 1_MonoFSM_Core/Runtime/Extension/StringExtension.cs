

namespace MonoFSM.Core
{
    public static class StringExtension
    {
        public static string FolderPath(this string path)
        {
            return path[..path.LastIndexOf('/')];
        }

     
    }
}