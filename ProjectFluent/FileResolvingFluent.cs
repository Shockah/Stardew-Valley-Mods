using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.ProjectFluent
{
	internal class FileResolvingFluent: IFluent<string>
	{
		private readonly Lazy<IFluent<string>> wrapped;
		
		public FileResolvingFluent(GameLocale locale, IEnumerable<string> filePathCandidates, IFluent<string> fallback)
		{
			wrapped = new(() =>
			{
				foreach (var filePathCandidate in filePathCandidates)
				{
					if (!File.Exists(filePathCandidate))
						continue;

					String content;
					try
					{
						content = File.ReadAllText(filePathCandidate);
					}
					catch (Exception e)
					{
						ProjectFluent.Instance.Monitor.Log($"There was a problem reading {filePathCandidate}:\n{e}", LogLevel.Error);
						continue;
					}

					try
					{
						return new FluentImpl(locale, content, fallback);
					}
					catch (Exception e)
					{
						ProjectFluent.Instance.Monitor.Log($"There was a problem parsing {filePathCandidate}:\n{e}", LogLevel.Error);
					}
				}

				return fallback;
			});
		}

		public string Get(string key, object tokens)
		{
			return wrapped.Value.Get(key, tokens);
		}
	}
}