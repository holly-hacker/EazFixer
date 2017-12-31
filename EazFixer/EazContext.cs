using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;
using EazFixer.Processors;

namespace EazFixer
{
    internal class EazContext : IEnumerable<ProcessorBase>
    {
        public ModuleDef Module;
        public Assembly Assembly;
        public ProcessorBase[] Processors;

        public EazContext(string file, ProcessorBase[] procs)
        {
            Module = ModuleDefMD.Load(file);
            Assembly = Assembly.LoadFile(file);
            Processors = procs;
        }

        //allow enumerating Processors
        public IEnumerator<ProcessorBase> GetEnumerator() => Processors.AsEnumerable().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
