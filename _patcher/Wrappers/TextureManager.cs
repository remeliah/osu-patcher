using _patcher.Constants;
using _patcher.Graphics.Skinning;
using _patcher.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace _patcher.Wrappers
{
    internal class TextureManager
    {
        private static readonly MethodBase BaseLoad = ILPatch.FindMethodBySignature(Patterns.TextureManager_Load);

        public static object Load(
            string spriteName,
            SkinSource source)
         => BaseLoad.Invoke(null, new object[] { spriteName, source });
    }
}
