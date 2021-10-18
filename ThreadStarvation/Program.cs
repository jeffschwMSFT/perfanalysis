using System;
using System.Threading;

public class Program
{ 
    public static void Main()
    {
        // Queue some simple work items that replace themselves and continually keep the thread pool busy.
        for (int i = 0; i < Environment.ProcessorCount; ++i)
        {
            ThreadPool.QueueUserWorkItem(MiscWorkItem, null);
        }

        Console.WriteLine("Press enter to queue blocking work items");
        Console.ReadLine();

        // Queue some work items that block indefinitely. Soon, all thread pool threads would end up running one of these work
        // items and the thread pool runs into starvation where no work items complete for a while.
        for (int i = 0; i < 100; ++i)
        {
            ThreadPool.QueueUserWorkItem(BlockingWorkItem, null);
        }

        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
    }

    #region private
    private static readonly ManualResetEvent s_unsignaledEvent = new ManualResetEvent(false);

    // general ThreadPool work item that generates more work items
    private static void MiscWorkItem(object? _)
    {
        ThreadPool.QueueUserWorkItem(MiscWorkItem, null);
    }

    // blocking ThreadPool call that ties up thread indefinetly
    private static void BlockingWorkItem(object? _)
    {
        s_unsignaledEvent.WaitOne();
    }
    #endregion
}
