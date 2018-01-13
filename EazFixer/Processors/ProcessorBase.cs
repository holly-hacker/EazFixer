using System;
using System.Reflection;
using dnlib.DotNet;

namespace EazFixer.Processors
{
    internal abstract class ProcessorBase
    {
        public bool Initialized => _errorInitialized == null;
        public bool Processed => Initialized && _errorProcessed == null;
        public bool CleanedUp => Processed && _errorCleanup == null;

        public string ErrorMessage => _errorInitialized ?? _errorProcessed ?? _errorCleanup;

        private string _errorInitialized;
        private string _errorProcessed;
        private string _errorCleanup;

        private EazContext _context;
        protected ModuleDef Mod => _context.Module;
        protected Assembly Asm => _context.Assembly;
        protected ProcessorBase[] OtherProcessors => _context.Processors;


        public void Initialize(EazContext ctx)
        {
            _context = ctx;

            try {
                InitializeInternal();
            } catch (Exception e) {
                _errorInitialized = e.Message;
            }
        }

        public void Process()
        {
            try {
                ProcessInternal();
            } catch (Exception e) {
                _errorProcessed = e.Message;
            }
        }

        public void Cleanup()
        {
            try {
                CleanupInternal();
            } catch (Exception e) {
                _errorCleanup = e.Message;
            }
        }

        protected abstract void InitializeInternal();
        protected abstract void ProcessInternal();
        protected abstract void CleanupInternal();
    }
}
