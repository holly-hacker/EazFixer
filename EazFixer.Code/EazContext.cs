using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;

namespace EazFixer.Code {
    internal class EazContext : IEnumerable<ProcessorBase> {
        public Assembly Assembly;
        public ModuleDef Module;
        public ProcessorBase[] Processors;

        public EazContext(string file, ProcessorBase[] Processors) {
            Module = ModuleDefMD.Load(file);
            Assembly = Assembly.Load(File.ReadAllBytes(file));
            this.Processors = Processors;
        }

        public ProcessorBase this[Type index] => Processors.Single(a => a.GetType() == index);

        //allow enumerating Processors
        public IEnumerator<ProcessorBase> GetEnumerator() {
            return Processors.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        //allow easily getting other processors by type
        public T Get<T>() where T : ProcessorBase {
            return (T) this[typeof(T)];
        }
    }
}