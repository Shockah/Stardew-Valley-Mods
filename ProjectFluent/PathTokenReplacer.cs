using StardewModdingAPI;
using System.Text.RegularExpressions;

namespace Shockah.ProjectFluent
{
	internal interface IPathTokenReplacer
	{
		string ReplaceTokens(string path, IManifest localizedMod, IGameLocale? locale);
	}

	internal class ModDirectoryPathTokenReplacer: IPathTokenReplacer
	{
		private IModRegistry ModRegistry { get; set; }
		private IModDirectoryProvider ModDirectoryProvider { get; set; }

		private Regex PathTokenRegex { get; set; } = new("%([\\w\\.]+?)%", RegexOptions.IgnoreCase);

		public ModDirectoryPathTokenReplacer(IModRegistry modRegistry, IModDirectoryProvider modDirectoryProvider)
		{
			this.ModRegistry = modRegistry;
			this.ModDirectoryProvider = modDirectoryProvider;
		}

		public string ReplaceTokens(string path, IManifest localizedMod, IGameLocale? locale)
		{
			return PathTokenRegex.Replace(path, m =>
			{
				string token = m.Groups[1].Value;
				var anotherMod = ModRegistry.Get(token);
				if (anotherMod is null)
					return m.Value;
				return ModDirectoryProvider.GetModDirectoryPath(anotherMod.Manifest);
			});
		}
	}
}