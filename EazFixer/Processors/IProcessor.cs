using System.Reflection;
using dnlib.DotNet;

namespace EazFixer.Processors
{
    internal interface IProcessor
    {
        void PreProcess(ModuleDef mod);
        void Process(ModuleDef mod, Assembly asm);
        void PostProcess(ModuleDef mod);
    }
}
