using System.Reflection;
using System.Reflection.Emit;
using _patcher.Constants;
using _patcher.Helpers;

namespace _patcher.Options
{
    /// <summary>
    /// Represents a section within a category in the options menu.
    /// </summary>
    internal class Section : Element
    {
        private static readonly ConstructorInfo BaseSection = ILPatch.FindConstructorBySignature(Patterns.Section_Constructor);

        public Section(string title) 
            : base(CreateSectionInstance(title))
        {
        }

        private static object CreateSectionInstance(string title) => 
            BaseSection.Invoke(new object[] { title, null });
    }
}
