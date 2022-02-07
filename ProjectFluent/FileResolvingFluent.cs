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

					try
					{
						var content = File.ReadAllText(filePathCandidate);
						var fluent = new FluentImpl(locale, content, fallback);
						return fluent;
					}
					catch (Exception)
					{
						// TODO: log at least?
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