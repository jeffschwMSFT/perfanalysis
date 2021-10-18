Demonstration of Threadpool starvation analysis.  The .NET Threadpool will fire a starvation event when work is queued and there are no threads available to process the work items.

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
     15744 dotnet     C:\Program Files\dotnet\dotnet.exe
     19132 dotnet     C:\Program Files\dotnet\dotnet.exe
     10620 starvation ...\bin\Debug\net6.0\starvation.exe
2> dotnet-counters monitor -p 10620
```

Notice that completed work items continues to climb and queque length remains constant and low.
```
    ThreadPool Completed Work Item Count (Count / 1 sec)   6,774,822
    ThreadPool Queue Length                                        5
    ThreadPool Thread Count                                        9
```

Induce starvation
```
>1 <enter>
```

The completed work item count plummetted and the queue length climbs.
```
    ThreadPool Completed Work Item Count (Count / 1 sec)           0
    ThreadPool Queue Length                                       55
    ThreadPool Thread Count                                       53
```

Grab a trace to understand what is happening.
```
2> dotnet-trace ps
     15744 dotnet     C:\Program Files\dotnet\dotnet.exe
     19132 dotnet     C:\Program Files\dotnet\dotnet.exe
     10620 starvation ...\bin\Debug\net6.0\starvation.exe
2> dotnet-trace collect -p 10620
<let it run for 30 seconds and hit enter - ~10mb>
```

Open the trace in perfview and view the Events page.  Within this page find the Threadpool Worker thread adjustment event.  This event will indicate why threads are being added, and in this case it is due to starvtion.
```
Event: Microsoft-Windows-DotNETRuntime/ThreadPoolWorkerThreadAdjustment/Adjustment
10,490.988    Process(9880) (9880) ThreadID="17,512" ProcessorNumber="0" AverageThroughput="0.000" NewWorkerThreadCount="13" Reason="Starvation" ClrInstanceID="0" 
```

Note that starvation events only occur when threads are added due to starvation, and may not always be observable in a trace taken at a random time.

Investigate cause of the blocking issue in perfview.
```
Name                                                                                   	 Inc %	         Inc
 UNMANAGED_CODE_TIME                                                                   	 100.0	   1,986,209
+ module System.Private.CoreLib.il <<System.Private.CoreLib.il!WaitHandle.WaitOne>>    	  57.9	1,149,105.375
|+ module starvation <<starvation!Program.BlockingWorkItem(class System.Object)>>      	  57.9	1,149,105.375
| + module System.Private.CoreLib.il <<System.Private.CoreLib.il!Thread.StartCallback>>	  57.9	1,149,105.375
|  + Thread (20932)                                                                    	   1.9	  38,356.289
|  |+ Threads                                                                          	   1.9	  38,356.289
|  | + (Non-Activities)                                                                	   1.9	  38,356.289
|  |  + Process64 Process(6124) (6124) Args:                                           	   1.9	  38,356.289
|  |   + ROOT                                                                          	   1.9	  38,356.289
```

Capture a dump and do further investigation.
```
2> dotnet-dump collect -p 10620
```

Open the dump in Visual Studio, debug with 'managed code only', and view the Threads window.

Open the dump in dotnet-dump and inspect the threadpool state.
```
2> dotnet-dump analysis

```
> threadpool
logStart: 0
logSize: 48
CPU utilization: 36 %
Worker Thread: Total: 7 Running: 7 Idle: 0 MaxLimit: 12 MinLimit: 4
Work Request in Queue: 0
--------------------------------------
Number of Timers: 0
--------------------------------------
Completion Port Thread:Total: 0 Free: 0 MaxFree: 0 CurrentLimit: 0 MaxLimit: 1000 MinLimit: 0
> threads
*0 0x502C (20524)
 1 0x6918 (26904)
 2 0x53F0 (21488)
 3 0x628C (25228)
 4 0x20C8 (8392)
 5 0x17C0 (6080)
 6 0x30F4 (12532)
 7 0x3A80 (14976)
 8 0x61C0 (25024)
 9 0x2740 (10048)
 10 0x121C (4636)
 11 0x14D8 (5336)
 12 0x5314 (21268)
 13 0x66C0 (26304)
 14 0x6904 (26884)
 15 0x2D7C (11644)
> setthread 14
> clrstack
OS Thread Id: 0x6904 (14)
        Child SP               IP Call Site
00000072228FF248 00007ffedf5ed8c4 [HelperMethodFrame: 00000072228ff248] System.Threading.WaitHandle.WaitOneCore(IntPtr, Int32)
00000072228FF350 00007FFD92986807 System.Threading.WaitHandle.WaitOneNoCheck(Int32) [/_/src/libraries/System.Private.CoreLib/src/System/Threading/WaitHandle.cs @ 139]
00000072228FF3B0 00007FFDF1C5824F System.Threading.WaitHandle.WaitOne() [/_/src/libraries/System.Private.CoreLib/src/System/Threading/WaitHandle.cs @ 420]
00000072228FF3E0 00007FFD92986B4A Program.BlockingWorkItem(System.Object) [...\perfanalysis\ThreadStarvation\Program.cs @ 40]
00000072228FF420 00007FFDF1C6529E System.Threading.ThreadPoolWorkQueue.Dispatch()
00000072228FF4B0 00007FFDF1C6CEF5 System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart() [/_/src/libraries/System.Private.CoreLib/src/System/Threading/PortableThreadPool.WorkerThread.cs @ 63]
00000072228FF5C0 00007FFDF1C5195F System.Threading.Thread.StartCallback() [/_/src/coreclr/System.Private.CoreLib/src/System/Threading/Thread.CoreCLR.cs @ 105]
00000072228FF850 00007ffdf25005c3 [DebuggerU2MCatchHandlerFrame: 00000072228ff850]
```

# Mitigation
The recommended mitigitation is to set an appropriate min thread count.

In code:
```
System.Threading.ThreadPool.SetMinThreads(int,int)
```

.NET Core project setting:
```
 <PropertyGroup>
    <ThreadPoolMinThreads>4</ThreadPoolMinThreads>
    <ThreadPoolMaxThreads>12</ThreadPoolMaxThreads>
  </PropertyGroup>
```
