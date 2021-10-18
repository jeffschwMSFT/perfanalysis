using System;
using System.Threading;
using System.Threading.Tasks;

public class LockContention
{
	public static void Main(string[] args)
	{
		// start threads that exhibit lock contention
		StartThreads(System.Environment.ProcessorCount);

		// wait for user input to exit
		Console.WriteLine("press <enter> to exit");
		Console.ReadLine();
	}

    #region private
    private static void StartThreads(int threadCount)
	{
		// start 'threadCount' threads that will contend on 'mylock'
		var mylock = new object();
 		var tasks = new Task[threadCount];
		for(int i=0; i<tasks.Length; i++)
		{
			var name = $"Thread {i}";
			tasks[i] = Task.Run(() => { ThreadOperation(mylock, name); });
		}
	}
 
	private static void ThreadOperation(object mylock, string threadName)
	{
		// attempt to acquire the lock and sleep for 500 ms
		var count = 0;
		while(true)
 		{
 	 		lock (mylock)
 	 		{
 	 		 	Console.WriteLine($"{threadName} is running iteration {count}");
				count++;
 	 		 	Thread.Sleep(500);
 	 		}
 		}
	}
    #endregion
}
