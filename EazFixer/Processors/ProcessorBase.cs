using System;
using System.Diagnostics;
using System.Reflection;
using dnlib.DotNet;

namespace EazFixer.Processors
{
    internal abstract class ProcessorBase
    {
        public bool Initialized = false;
        public bool Success = false;

        public bool Initialize(ModuleDef mod)
        {
            try {
                InitializeInternal(mod);
                return Initialized = true;
            } catch (Exception e) {
                Debug.WriteLine($"[D] Could not initialize {MethodBase.GetCurrentMethod().DeclaringType?.Name}: {e.Message}");
                return Initialized = false;
            }
        }

        public bool Process(ModuleDef mod, Assembly asm)
        {
            try {
                InitializeInternal(mod);
                return Success = true;
            } catch (Exception e) {
                Debug.WriteLine($"[D] Could not execute {MethodBase.GetCurrentMethod().DeclaringType?.Name}: {e.Message}");
                return Success = false;
            }
        }

        public void Cleanup(ModuleDef mod) => CleanupInternal(mod);

        protected abstract void InitializeInternal(ModuleDef mod);
        protected abstract void ProcessInternal(ModuleDef mod, Assembly asm);
        protected abstract void CleanupInternal(ModuleDef mod);
    }
}
