using System;
using System.Diagnostics.Tracing;
using System.Threading;

namespace InProcessAnalysis
{
    // Details on .NET ETW Events - https://docs.microsoft.com/en-us/dotnet/framework/performance/clr-etw-events

    public class DotNETRuntimeEventListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime", StringComparison.OrdinalIgnoreCase))
            {
                // todo - add an EventKeyword on what events to include
                EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)0x0);
            }

            base.OnEventSourceCreated(eventSource);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // todo - add event filters to select only relevant events
            base.OnEventWritten(eventData);
        }
    }
}
