using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InProcessAnalysis
{
    [EventSource(Name = "InProcessAnalysis.DotNETRuntimeEventSource")]
    public sealed class DotNETRuntimeEventSource : EventSource
    {
        public static DotNETRuntimeEventSource Log = new DotNETRuntimeEventSource();

        [Event(1, Message = "Start", Level = EventLevel.Informational)]
        public void Start() { WriteEvent(1); }
    }
}
