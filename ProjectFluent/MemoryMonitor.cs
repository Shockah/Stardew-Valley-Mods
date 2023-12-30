using StardewModdingAPI;
using System.Collections.Generic;
using StardewModdingAPI.Framework.Logging;

namespace Shockah.ProjectFluent
{
	internal class MemoryMonitor : IMonitor
	{
		public bool IsVerbose
			=> true;

		private List<(string message, LogLevel? level, bool once, bool verbose)> Logs { get; set; } = new();

		public void Log(string message, LogLevel level = LogLevel.Trace)
			=> Logs.Add((message, level, once: false, verbose: false));

		public void LogOnce(string message, LogLevel level = LogLevel.Trace)
			=> Logs.Add((message, level, once: true, verbose: false));

		public void VerboseLog(ref VerboseLogStringHandler message)
		{
			if (this.IsVerbose)
				Logs.Add((message.ToString(), level: null, once: false, verbose: true));	
		}
		public void VerboseLog(string message)
			=> Logs.Add((message, level: null, once: false, verbose: true));

		public void Clear()
			=> Logs.Clear();

		public void FlushToMonitor(IMonitor monitor, bool clear = true)
		{
			foreach (var (message, level, once, verbose) in Logs)
			{
				if (verbose)
					monitor.VerboseLog(message);
				else if (once)
					monitor.LogOnce(message, level!.Value);
				else
					monitor.Log(message, level!.Value);
			}
			if (clear)
				Clear();
		}
	}
}