using StardewModdingAPI;
using StardewModdingAPI.Framework.Logging;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shockah.ProjectFluent
{
	internal class MemoryMonitor : IMonitor
	{
		public bool IsVerbose
			=> true;

		private List<(string message, LogLevel level, bool once, bool verbose)> Logs { get; set; } = [];

		public void Log(string message, LogLevel level = LogLevel.Trace)
			=> Logs.Add((message, level, once: false, verbose: false));

		public void LogOnce(string message, LogLevel level = LogLevel.Trace)
			=> Logs.Add((message, level, once: true, verbose: false));

		public void VerboseLog(string message)
			=> Logs.Add((message, LogLevel.Trace, once: false, verbose: true));

		public void VerboseLog([InterpolatedStringHandlerArgument("")] ref VerboseLogStringHandler message)
		{
			if (IsVerbose)
				Logs.Add((message.ToString(), LogLevel.Trace, once: false, verbose: true));
		}

		public void Clear()
			=> Logs.Clear();

		public void FlushToMonitor(IMonitor monitor, bool clear = true)
		{
			foreach (var (message, level, once, verbose) in Logs)
			{
				if (verbose)
					monitor.VerboseLog(message);
				else if (once)
					monitor.LogOnce(message, level);
				else
					monitor.Log(message, level);
			}
			if (clear)
				Clear();
		}
	}
}