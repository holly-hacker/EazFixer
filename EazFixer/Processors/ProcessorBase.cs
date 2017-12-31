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

        private EazContext _context;
        protected ModuleDef Mod => _context.Module;
        protected Assembly Asm => _context.Assembly;
        protected ProcessorBase[] OtherProcessors => _context.Processors;


        public bool Initialize(EazContext ctx)
        {
            _context = ctx;

            try {
                InitializeInternal();
                return Initialized = true;
            } catch (Exception e) {
                Debug.WriteLine($"[D] Could not initialize {MethodBase.GetCurrentMethod().DeclaringType?.Name}: {e.Message}");
                return Initialized = false;
            }
        }

        public bool Process()
        {
            try {
                ProcessInternal();
                return Success = true;
            } catch (Exception e) {
                Debug.WriteLine($"[D] Could not execute {MethodBase.GetCurrentMethod().DeclaringType?.Name}: {e.Message}");
                return Success = false;
            }
        }

        public void Cleanup()
        {
            try {
                CleanupInternal();
            } catch (Exception e) {
                Debug.WriteLine($"[D] Could not clean up {MethodBase.GetCurrentMethod().DeclaringType?.Name}: {e.Message}");
            }
        }

        protected abstract void InitializeInternal();
        protected abstract void ProcessInternal();
        protected abstract void CleanupInternal();
    }
}
