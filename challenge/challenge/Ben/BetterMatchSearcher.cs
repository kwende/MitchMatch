using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace challenge.Ben
{
    public class BetterMatchSearcher
    {
        private class Context
        {
            public row[] Rows { get; set; }
            public TransitiveClosure Closures { get; set; }
        }

        private CountdownEvent _countDown = new CountdownEvent(50);

        private void ThreadCallback(object context)
        {
            _countDown.Signal(); 
        }

        public void Search(row[] rows, TransitiveClosure tc)
        {
            for (int c = 0; c < 100; c++)
            {
                ThreadPool.QueueUserWorkItem(ThreadCallback, new Context { Closures = tc, Rows = rows }); 
            }
        }
    }
}
