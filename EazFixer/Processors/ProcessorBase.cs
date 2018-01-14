using System;

namespace EazFixer.Processors
{
    internal abstract class ProcessorBase
    {
        public bool Initialized => _errorInitialized == null;
        public bool Processed => Initialized && _errorProcessed == null;
        public bool CleanedUp => Processed && _errorCleanup == null;

        public string ErrorMessage => _errorInitialized ?? _errorProcessed ?? _errorCleanup;

        protected EazContext Ctx;

        private string _errorInitialized;
        private string _errorProcessed;
        private string _errorCleanup;


        public void Initialize(EazContext ctx)
        {
            Ctx = ctx;

            try {
                InitializeInternal();
            } catch (Exception e) {
                _errorInitialized = "Init error: " + e.Message;
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
                _errorCleanup = "Cleanup error: " + e.Message;
            }
        }

        protected abstract void InitializeInternal();
        protected abstract void ProcessInternal();
        protected abstract void CleanupInternal();
    }
}
