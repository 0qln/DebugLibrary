using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugLibrary {
    namespace Benchmark {
        public static class Measure {
            public static Stopwatch Execute(Action action, params object[] parameters) {
                Stopwatch sw = new();
                sw.Start();
                action.Invoke();
                sw.Stop();
                return sw;
            }
        }
    }
}
