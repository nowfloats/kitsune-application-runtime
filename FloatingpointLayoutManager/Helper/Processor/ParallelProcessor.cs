using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper.Processor
{
    public class ParallelProcessor
    {
        public delegate void Method();

        public static void ExecuteParallel(params Method[] methods)
        {
            ManualResetEvent[] blockers = new ManualResetEvent[methods.Length];
            for (int i = 0; i < methods.Length; i++)
            {
                blockers[i] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(new WaitCallback((object index) =>
                {
                    int methodIndex = (int)index;
                    methods[methodIndex]();
                    blockers[methodIndex].Set();
                }), i);
            }
            WaitHandle.WaitAll(blockers);
        }
    }
}
