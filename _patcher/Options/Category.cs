using System.Reflection;
using System.Reflection.Emit;
using _patcher.Helpers;

namespace _patcher.Options
{
    internal class Category : Element
    {
        // last OsuString
        private const int TryLazer = 0x507;
        
        private static readonly ConstructorInfo BaseCategoryConstructor = ILPatch.FindConstructorBySignature(new[]
        {
            OpCodes.Ldarg_0,
            OpCodes.Ldarga_S,
            OpCodes.Constrained,
            OpCodes.Callvirt,
            OpCodes.Call,
            OpCodes.Ldarg_0,
            OpCodes.Ldarga_S,
            OpCodes.Constrained,
            OpCodes.Callvirt,
            OpCodes.Call
        });

        public Category(FontAwesome icon)
            : base(CreateCategoryInstance(icon))
        {
        }

        private static object CreateCategoryInstance(FontAwesome icon)
            => BaseCategoryConstructor.Invoke(new object[] { TryLazer + 1, icon });
    }
}