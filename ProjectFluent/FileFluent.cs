using StardewModdingAPI;
using System;
using System.IO;

namespace Shockah.ProjectFluent
{
	internal class FileFluent: IFluent<string>
	{
		private IFluent<string> Wrapped { get; set; }

		public FileFluent(IGameLocale locale, string path, IFluent<string> fallback)
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
				Wrapped = new FluentImpl(locale, content, fallback);
			}
			catch (Exception e)
			{
				ProjectFluent.Instance.Monitor.Log($"There was a problem parsing \"{path}\":\n{e}", LogLevel.Error);
				Wrapped = fallback;
			}
		}

		public bool ContainsKey(string key)
		{
			return Wrapped.ContainsKey(key);
		}

		public string Get(string key, object? tokens)
		{
			return Wrapped.Get(key, tokens);
		}
	}
}