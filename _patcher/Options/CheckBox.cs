using System;
using System.Reflection;
using System.Reflection.Emit;
using _patcher.Helpers;

namespace _patcher.Options
{
    internal class CheckBox : Element
    {
        private static ConstructorInfo _boolCons;

        private static readonly ConstructorInfo BaseCheckBox = ILPatch.FindConstructorBySignature(new[]
        {
            OpCodes.Ldarg_0,
            OpCodes.Ldarg_3,
            OpCodes.Stfld,
            OpCodes.Ldarg_3,
            OpCodes.Brfalse_S,
            OpCodes.Ldarg_3,
            OpCodes.Ldarg_0,
            OpCodes.Ldftn,
            OpCodes.Newobj
        });

        public CheckBox(string title, string tooltip, bool initial, EventHandler onChanged)
            : base(CreateCheckBoxInstance(title, tooltip, initial, onChanged))
        {
        }

        private static object CreateCheckBoxInstance(string title, string tooltip, bool initial, EventHandler onChanged)
        {
            if (_boolCons == null)
            {
                _boolCons = BaseCheckBox
                    .GetParameters()[2]
                    .ParameterType
                    .GetConstructor(new[] 
                    { 
                        typeof(bool) 
                    });
            }

            object bindableBool = _boolCons.Invoke(new object[] { initial });

            return BaseCheckBox.Invoke(new object[]
            {
                title,
                tooltip,
                bindableBool,
                onChanged
            });
        }
    }
}