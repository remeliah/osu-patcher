using _patcher.Helpers;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace _patcher.Options
{
    internal class Element
    {
        internal object V { get; set; }
        protected Element(object v) => V = v;

        private static readonly MethodBase BaseSetChildren = ILPatch.FindMethodBySignature(new[]
        {
            OpCodes.Ldarg_0,
            OpCodes.Ldarg_1,
            OpCodes.Stfld,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Brfalse_S,
            OpCodes.Ldarg_0,
            OpCodes.Ldfld,
            OpCodes.Callvirt,
            OpCodes.Stloc_0,
            OpCodes.Br_S,
            OpCodes.Ldloc_0,
            OpCodes.Callvirt,
            OpCodes.Stloc_1,
            OpCodes.Ldloc_1,
            OpCodes.Ldarg_0,
            OpCodes.Stfld,
            OpCodes.Ldarg_0,
            OpCodes.Call,
            OpCodes.Brfalse_S,
            OpCodes.Ldloc_1,
            OpCodes.Ldc_I4_1,
            OpCodes.Callvirt,
            OpCodes.Ldloc_0,
            OpCodes.Callvirt,
            OpCodes.Brtrue_S,
            OpCodes.Leave_S,
            OpCodes.Ldloc_0,
            OpCodes.Brfalse_S,
            OpCodes.Ldloc_0,
            OpCodes.Callvirt,
            OpCodes.Endfinally,

        });

        public static Array CreateArray(params Element[] elements)
        {
            Array array = Array.CreateInstance(elements
                .First()
                .V.GetType()
                .BaseType,
                elements.Length);

            for (int i = 0; i < elements.Length; i++)
                array.SetValue(elements[i].V, i);
            
            return array;
        }

        public void SetChildren(Array children) 
            => BaseSetChildren.Invoke(V, new object[] { children });
    }
}
