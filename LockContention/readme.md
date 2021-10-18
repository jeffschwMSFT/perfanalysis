This example is a simple lock contention repro.

# Pre-requisites

 - dotnet tool install -g dotnet-counters
 - dotnet tool install -g dotnet-trace
 - dotnet tool install -g dotnet-dump
 - Download perfview (https://github.com/microsoft/perfview/blob/main/documentation/Downloading.md)

# Walk-through

Run the app
```
1> dotnet run
```

Monitor
```
2> dotnet-counters ps
      7948 dotnet     C:\Program Files\dotnet\dotnet.exe
     23328 dotnet     C:\Program Files\dotnet\dotnet.exe
     26516 lockcontention ...\perfanalysis\LockContention\bin\Debug\net6.0\LockContention.exe
2> dotnet-counters monitor -p 26516
    Monitor Lock Contention Count (Count / 1 sec)                  2
```

Notice that the number of lock contention events per second is 2.

Get a trace to analyze.
```
2> dotnet-trace collect -p 26516
No profile or providers specified, defaulting to trace profile 'cpu-sampling'

Provider Name                           Keywords            Level               Enabled By
Microsoft-DotNETCore-SampleProfiler     0x0000F00000000000  Informational(4)    --profile
Microsoft-Windows-DotNETRuntime         0x00000014C14FCCBD  Informational(4)    --profile

Process        : ...\perfanalysis\LockContention\bin\Debug\net6.0\LockContention.exe
Output File    : ...\LockContention.exe_20211013_151040.nettrace


[00:00:00:10]   Recording trace 1.1654   (MB)
Press <Enter> or <Ctrl+C> to exit...
Stopping the trace. This may take up to minutes depending on the application being traced.

Trace completed.
```

Open the trace in perfview and open the Events page.  Within this page find the Contention events.
```
Event Name                                      	Time MSec	Process Name          	Rest  
Microsoft-Windows-DotNETRuntime/Contention/Start	3,055.595	Process(26516) (26516)	HasStack="True" ThreadID="11,728" ProcessorNumber="0" ContentionFlags="Managed" ClrInstanceID="8" 
Microsoft-Windows-DotNETRuntime/Contention/Stop 	3,556.549	Process(26516) (26516)	ThreadID="9,736" ProcessorNumber="0" DURATION_MSEC="3,514.307" ContentionFlags="Managed" ClrInstanceID="8" DurationNs="3,514,362,800.000" 
```

If you click on these events and choose 'show any stacks' you can get a callstack where this is occuring.
```
Name
+ Process64 Process(26516) (26516) Args: 
 + Thread (12248)
 |+ module System.Private.CoreLib.il <<System.Private.CoreLib.il!Thread.StartCallback>>
 | + module LockContention <<LockContention!LockContention+<>c__DisplayClass1_1.<StartThreads>b__0()>>
 |  + Type: External
 |  |+ Event Microsoft-DotNETCore-SampleProfiler/Thread/Sample
```

This analysis can also be done via a dump.  Create a dump for the process.
```
2> dotnet-dump collect -p 23260

Writing full to ...\dump_20211013_152229.dmp
Complete
```

Open the dump in dotnet-dump analyze and you can inspect the threads that are holding and blocked on the lock.
```
2> dotnet-dump analyze ...\dump_20211013_152229.dmp
Loading core dump: ...\dump_20211013_152229.dmp ...
Ready to process analysis commands. Type 'help' to list available commands or 'help [command]' to get detailed help on a command.
Type 'quit' or 'exit' to exit the session.
> syncblk
Index         SyncBlock MonitorHeld Recursion Owning Thread Info          SyncBlock Owner
   11 00000207FCA2D548           15         1 00000207FCA2EE20 12f8   7   000002078000c410 System.Object
-----------------------------
Total           13
CCW             0
RCW             0
ComClassFactory 0
Free            0
> setthread 7
> clrstack
OS Thread Id: 0x12f8 (7)
        Child SP               IP Call Site
0000003C103FF118 00007ffedf5ed3f4 [HelperMethodFrame: 0000003c103ff118] System.Threading.Thread.SleepInternal(Int32)
0000003C103FF210 00007FFDF2522536 System.Threading.Thread.Sleep(Int32) [/_/src/libraries/System.Private.CoreLib/src/System/Threading/Thread.cs @ 356]
0000003C103FF250 00007FFDE0C7AED5 LockContention.ThreadOperation(System.Object, System.String) [...\perfanalysis\LockContention\Program.cs @ 40]
0000003C103FF300 00007FFDE0C79EC6 LockContention+<>c__DisplayClass1_1.<StartThreads>b__0() [...\perfanalysis\LockContention\Program.cs @ 26]
0000003C103FF330 00007FFDF2541E53 System.Threading.Tasks.Task.InnerInvoke() [/_/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/Task.cs @ 2381]
0000003C103FF370 00007FFDF2546849 System.Threading.Tasks.Task+<>c.<.cctor>b__271_0(System.Object)
0000003C103FF3A0 00007FFDF252C995 System.Threading.ExecutionContext.RunFromThreadPoolDispatchLoop(System.Threading.Thread, System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object) [/_/src/libraries/System.Private.CoreLib/src/System/Threading/ExecutionContext.cs @ 268]
0000003C103FF3F0 00007FFDF2541B58 System.Threading.Tasks.Task.ExecuteWithThreadLocal(System.Threading.Tasks.Task ByRef, System.Threading.Thread) [/_/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/Task.cs @ 2331]
0000003C103FF490 00007FFDF2541A63 System.Threading.Tasks.Task.ExecuteEntryUnsafe(System.Threading.Thread) [/_/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/Task.cs @ 2265]
0000003C103FF4D0 00007FFDF25353C7 System.Threading.ThreadPoolWorkQueue.Dispatch()
0000003C103FF560 00007FFDF253CEF5 System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart() [/_/src/libraries/System.Private.CoreLib/src/System/Threading/PortableThreadPool.WorkerThread.cs @ 63]
0000003C103FF670 00007FFDF252195F System.Threading.Thread.StartCallback() [/_/src/coreclr/System.Private.CoreLib/src/System/Threading/Thread.CoreCLR.cs @ 105]
0000003C103FF900 00007ffe408005c3 [DebuggerU2MCatchHandlerFrame: 0000003c103ff900]
```

