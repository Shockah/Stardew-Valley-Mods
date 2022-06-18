using StardewValley;
using System.Collections.Generic;
using System.IO;

namespace Shockah.ProjectFluent
{
	internal interface IFluentPathProvider
	{
		IEnumerable<string> GetFilePathCandidates(string directory, string? name, IGameLocale locale);
	}

	internal class FluentPathProvider: IFluentPathProvider
	{
		public IEnumerable<string> GetFilePathCandidates(string directory, string? name, IGameLocale locale)
		{
			foreach (var relevantLocale in locale.GetRelevantLocaleCodes())
			{
				string fileNameWithoutExtension = $"{(string.IsNullOrEmpty(name) ? "" : $"{name}.")}{relevantLocale}";
				yield return Path.Combine(directory, $"{fileNameWithoutExtension}.ftl");
			}
			foreach (var relevantLocale in new IGameLocale.BuiltIn(LocalizedContentManager.LanguageCode.en).GetRelevantLocaleCodes())
			{
				string fileNameWithoutExtension = $"{(string.IsNullOrEmpty(name) ? "" : $"{name}.")}{relevantLocale}";
				yield return Path.Combine(directory, $"{fileNameWithoutExtension}.ftl");
			}
		}
	}
}