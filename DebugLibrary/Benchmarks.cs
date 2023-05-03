using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugLibrary {
    namespace Benchmarks {
        public static class Benchmark {
            public static void ExecuteMeasured(Action action, params object[] parameters) {
                Stopwatch sw = new();
                sw.Start();
                action.Invoke();
                sw.Stop();
                Debugger.Console.Log(sw.ElapsedMilliseconds + " Milliseconds");
            }

        }
    }
}
