using System;
using UnityEditor;

namespace MonoFSM.Editor
{
    public class MenuWrapper : NonPublicClassWrapper
    {
        public static MenuWrapper Instance
        {
            get
            {
                objectInstance ??= new MenuWrapper();
                return objectInstance;
            }
        }

        static MenuWrapper objectInstance;

        protected override Type LoadOriginalType()
        {
            // return UnityEditorAssembly.GetClassType("Menu");
            return typeof(Menu);
        }

        public string[] ExtractSubmenus(string menuPath)
        {
            return InvokeStaticMethod<string[]>("ExtractSubmenus", new object[] { menuPath });
        }
    }
}