using System;
using System.Reflection;
using System.Reflection.Emit;
using _patcher.Helpers;
using HarmonyLib;

namespace _patcher.Patches
{
    /// <summary>
    /// Base class for all patches.
    /// </summary>
    public abstract class BasePatch
    {
        /// <summary>
        /// Finds and returns the target method based on IL opcodes.
        /// </summary>
        /// <param name="signature">The IL signature to match.</param>
        /// <returns>The MethodBase if found.</returns>
        protected static MethodBase TargetMethodBySignature(OpCode[] signature)
        {
            return ILPatch.FindMethodBySignature(signature);
        }
    }
}
