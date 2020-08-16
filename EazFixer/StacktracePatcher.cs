using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace EazFixer
{
    public static class StacktracePatcher
    {
        private const string HarmonyId = "holly.eazfixer.stacktrace";
        private static Harmony _harmony;

        public static void Patch()
        {
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void UnPatch()
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _harmony = null;
        }

        [HarmonyPatch(typeof(StackFrame), "GetMethod")]
        public class PatchStackTraceGetMethod
        {
            public static MethodInfo MethodToReplace;

            public static void Postfix(ref MethodBase __result)
            {
                if (__result.DeclaringType == typeof(RuntimeMethodHandle))
                {
                    //just replace it with a method
                    __result = MethodToReplace ?? MethodBase.GetCurrentMethod();
                    Debug.WriteLine("[D] Patched stacktrace entry");
                }
            }
        }
    }
}
