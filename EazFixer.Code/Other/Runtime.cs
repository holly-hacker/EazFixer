using System;
using System.Diagnostics;
using System.Reflection;
using Harmony;

namespace EazFixer.Code.Other {
    internal class Runtime {
        private static int patchCount;

        public static void Patch() {
            var harmonyInstance = HarmonyInstance.Create("holly.eazfixer");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(StackFrame), "GetMethod")]
        public class PatchStackTraceGetMethod {
            public static MethodInfo MethodToReplace;

            public static void Postfix(ref MethodBase __result) {
                if (__result.DeclaringType == typeof(RuntimeMethodHandle)) {
                    // replace stack trace with a method
                    __result = MethodToReplace ?? MethodBase.GetCurrentMethod();
                    Logger.Message($"Patched stack strace {patchCount}.");
                    patchCount++;
                }
            }
        }
    }
}