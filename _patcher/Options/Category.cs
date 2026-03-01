using System.Reflection;
using System.Reflection.Emit;
using _patcher.Constants;
using _patcher.Helpers;

namespace _patcher.Options
{
    /// <summary>
    /// Represents a category in the options menu.
    /// </summary>
    internal class Category : Element
    {
        private static readonly ConstructorInfo BaseCategoryConstructor = ILPatch.FindConstructorBySignature(Patterns.Category_Constructor);

        public Category(FontAwesome icon)
            : base(CreateCategoryInstance(icon))
        {
        }

        private static object CreateCategoryInstance(FontAwesome icon)
            => BaseCategoryConstructor.Invoke(new object[] { OsuConstants.TryLazer + 1, icon });
    }
}
