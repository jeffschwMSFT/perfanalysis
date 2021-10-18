using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Diagnostics.Tracing;

namespace Analysis
{
	public class Program
	{
		public static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("./analysis [path to etl file]");
				return 1;
			}

			var localStorage = Path.Combine(Environment.CurrentDirectory, "extract_zip");
			try
			{
				// setup
				if (Directory.Exists(localStorage)) Directory.Delete(localStorage, true);

				// process the etl file
				var source = OpenTraceForProcessing(args[0], localStorage);

				// process for lock contention
				var locks = LockContention.Process(source);
			}
			finally
			{
				// cleanup
				if (Directory.Exists(localStorage)) Directory.Delete(localStorage, true);
			}

			return 0;
		}

		#region private
		private static TraceEventDispatcher OpenTraceForProcessing(string etlFilename, string localStorage)
		{
			// extract the contents from the zip if required
			etlFilename = ExtractEtlPath(etlFilename, localStorage);

			// get the source provider
			return OpenEtlfile(etlFilename);
		}

		private static string ExtractEtlPath(string etlFilename, string localStorage)
		{
			if (etlFilename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
			{
				if (Directory.Exists(localStorage)) throw new Exception("must provide an empty/non-existent directory for local storage");

				// create the local directory
				Directory.CreateDirectory(localStorage);

				// unzip to a local location
				ZipFile.ExtractToDirectory(etlFilename, localStorage);

				// search for the *.etl file and set that as the filename
				var foundetl = false;
				foreach (var file in Directory.GetFiles(localStorage, "*.etl"))
				{
					if (File.Exists(file))
					{
						etlFilename = file;
						foundetl = true;
						break;
					}
				}
				if (!foundetl)
				{
					// search for *.nettrace
					foreach (var file in Directory.GetFiles(localStorage, "*.nettrace"))
					{
						if (File.Exists(file))
						{
							etlFilename = file;
							foundetl = true;
							break;
						}
					}
				}
				if (!foundetl) throw new Exception("failed to find an etl in the zip file");
			}

			return etlFilename;
		}

		private static TraceEventDispatcher OpenEtlfile(string etlFilename)
		{
			if (etlFilename.ToLower().EndsWith(".nettrace", StringComparison.OrdinalIgnoreCase))
			{
				return new EventPipeEventSource(etlFilename);
			}
			else
			{
				return TraceEventDispatcher.GetDispatcherFromFileName(etlFilename, new TraceEventDispatcherOptions());
			}
		}
		#endregion
	}
}
