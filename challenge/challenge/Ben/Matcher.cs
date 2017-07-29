using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace challenge.Ben
{
    public class Matcher
    {
        class ThreadContext
        {
            public TransitiveClosure TransitiveClosure { get; set; }
            public row[] AllData { get; set; }
            public Random Random { get; set; }
            public string OutputDirectory { get; set; }
        }

        private ThreadContext _context;

        private static void ThreadWorkerCallback(object context)
        {
            ThreadContext threadContext = context as ThreadContext;

            int theRowIndex = threadContext.Random.Next(0, threadContext.AllData.Length - 1);
            row theRow = threadContext.AllData[theRowIndex];

            List<row> betterMatches = EditDistance.FindClosestMatchesForRowInEntireDataSet(theRow,
                threadContext.AllData, threadContext.TransitiveClosure);

            lock (context)
            {
                Console.WriteLine($"Found {betterMatches.Count}");
            }

            if (betterMatches.Count > 0)
            {
                // easier to just surround this with a try/catch in the event
                // of the super unlikely match. 
                try
                {
                    using (StreamWriter sw = File.CreateText(
                        Path.Combine(threadContext.OutputDirectory, theRow.EnterpriseID.ToString()) + ".csv"))
                    {
                        sw.WriteLine(theRow.ToString());
                        row[] closedSet = threadContext.TransitiveClosure.FindClosedSetForRow(theRow);
                        foreach (row item in closedSet)
                        {
                            if (item != theRow)
                            {
                                sw.WriteLine(item.ToString());
                            }
                        }
                        sw.WriteLine();
                        foreach (row betterMatch in betterMatches)
                        {
                            sw.WriteLine(betterMatch.ToString());
                        }
                    }
                }
                catch
                {

                }
            }


            ThreadPool.QueueUserWorkItem(ThreadWorkerCallback, context);
        }

        public Matcher(row[] alldata, TransitiveClosure tc, string outputDirectory)
        {
            _context = new ThreadContext
            {
                AllData = alldata,
                TransitiveClosure = tc,
                OutputDirectory = outputDirectory,
                Random = new Random()
            };
        }
        public void DoIt()
        {
            // queue up 100 requests. 
            for (int c = 0; c < 100; c++)
            {
                ThreadPool.QueueUserWorkItem(ThreadWorkerCallback, _context);
            }
        }
    }
}
