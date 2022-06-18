using StardewModdingAPI;
using System.Collections.Generic;
using System.IO;

namespace Shockah.ProjectFluent
{
	internal interface IModDirectoryFluentPathProvider
	{
		IEnumerable<string> GetFilePathCandidates(IManifest mod, string? name, IGameLocale locale);
	}

	internal class ModDirectoryFluentPathProvider: IModDirectoryFluentPathProvider
	{
		private IModDirectoryProvider ModDirectoryProvider { get; set; }
		private IFluentPathProvider FluentPathProvider { get; set; }

		public ModDirectoryFluentPathProvider(IModDirectoryProvider modDirectoryProvider, IFluentPathProvider fluentPathProvider)
		{
			this.ModDirectoryProvider = modDirectoryProvider;
			this.FluentPathProvider = fluentPathProvider;
		}

		public IEnumerable<string> GetFilePathCandidates(IManifest mod, string? name, IGameLocale locale)
		{
			var baseModPath = ModDirectoryProvider.GetModDirectoryPath(mod);
			if (baseModPath is null)
				yield break;
			foreach (var candidate in FluentPathProvider.GetFilePathCandidates(Path.Combine(baseModPath, "i18n"), name, locale))
				yield return candidate;
		}
	}
}