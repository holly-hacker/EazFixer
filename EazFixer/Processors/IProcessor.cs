using dnlib.DotNet;

namespace EazFixer.Processors
{
    internal interface IProcessor
    {
        void PreProcess(ModuleDef m);
        void Process(ModuleDef m);
        void PostProcess(ModuleDef m);
    }
}
