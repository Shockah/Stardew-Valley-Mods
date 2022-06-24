using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shockah.ProjectFluent
{
	internal class FileFluent: IFluent<string>
	{
		private IFluent<string> Wrapped { get; set; }

		public FileFluent(IEnumerable<(string name, ContextfulFluentFunction function)> functions, IGameLocale locale, string path, IFluent<string> fallback)
		{
			if (!File.Exists(path))
			{
				Wrapped = fallback;
				return;
			}

			string content;
			try
			{
				content = File.ReadAllText(path);
				if (content.Contains('\t'))
				{
					ProjectFluent.Instance.Monitor.Log($"Fluent file \"{path}\" contains tab (\\t) characters. Those aren't officially supported and may cause problems. Replacing with 4 spaces.", LogLevel.Warn);
					content = content.Replace("\t", "    ");
				}
			}
			catch (Exception e)
			{
				ProjectFluent.Instance.Monitor.Log($"There was a problem reading \"{path}\":\n{e}", LogLevel.Error);
				Wrapped = fallback;
				return;
			}

			try
			{
				Wrapped = new FluentImpl(functions, locale, content, fallback);
			}
			catch (Exception e)
			{
				ProjectFluent.Instance.Monitor.Log($"There was a problem parsing \"{path}\":\n{e}", LogLevel.Error);
				Wrapped = fallback;
			}
		}

		public bool ContainsKey(string key)
			=> Wrapped.ContainsKey(key);

		public string Get(string key, object? tokens)
			=> Wrapped.Get(key, tokens);
	}
}