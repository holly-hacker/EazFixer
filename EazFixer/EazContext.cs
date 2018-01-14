using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        //allow easily getting other processors by type
        public T Get<T>() where T : ProcessorBase => (T)this[typeof(T)];
        public ProcessorBase this[Type index] => Processors.Single(a => a.GetType() == index);
    }
}
