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
						if (content.Contains('\t'))
						{
							ProjectFluent.Instance.Monitor.Log($"Fluent file \"{filePathCandidate}\" contains tab (\\t) characters. Those aren't officially supported and may cause problems; replacing with 4 spaces.", LogLevel.Warn);
							content = content.Replace("\t", "    ");
						}
					}
					catch (Exception e)
					{
						ProjectFluent.Instance.Monitor.Log($"There was a problem reading \"{filePathCandidate}\":\n{e}", LogLevel.Error);
						continue;
					}

					try
					{
						return new FluentImpl(locale, content, fallback);
					}
					catch (Exception e)
					{
						ProjectFluent.Instance.Monitor.Log($"There was a problem parsing \"{filePathCandidate}\":\n{e}", LogLevel.Error);
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