using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Analysis;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Analysis
{
	public class LockContention
	{
		public static LockContention Process(TraceEventDispatcher source)
		{
			var locks = new LockContention();

			// setup callbacks
			source.NeedLoadedDotNetRuntimes();
			source.Clr.ContentionStart += locks.CallbackContentionStart;
			source.Clr.ContentionStop += locks.CallbackContentionStop;

			// process the contents of the etl
			source.Process();

			return locks;
		}

        #region private
        private void CallbackContentionStart(ContentionStartTraceData data)
		{
			Console.WriteLine($"ContentStart: {data.ThreadID}");
		}

		private void CallbackContentionStop(ContentionStopTraceData data)
		{
			Console.WriteLine($"ContentStop : {data.ThreadID}");
		}
		#endregion
	}
}
