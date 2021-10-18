Small starting sample for processing traces collected from [PerfView](https://github.com/microsoft/perfview) or [dotnet-trace](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace).

Additional [TraceEvent Documentation](https://github.com/microsoft/perfview/blob/main/documentation/TraceEvent/TraceEventLibrary.md) is available for additional details.

Example to try:
```
1> cd LockContention
1> dotnet run
2> dotnet-trace ps
   ### LockContention
2> dotnet-trace collect -p ###
  Output <path to .nettrace>
2> cd Analysis
2> dotnet run -- <path to .nettrace>
```
