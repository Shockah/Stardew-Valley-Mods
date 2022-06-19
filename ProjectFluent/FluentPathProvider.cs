using System.Collections.Generic;
using System.IO;

namespace Shockah.ProjectFluent
{
	internal interface IFluentPathProvider
	{
		IEnumerable<string> GetFilePathCandidates(IGameLocale locale, string directory, string? name);
	}

	internal class FluentPathProvider: IFluentPathProvider
	{
		public IEnumerable<string> GetFilePathCandidates(IGameLocale locale, string directory, string? name)
		{
			foreach (var relevantLocale in locale.GetRelevantLocaleCodes())
			{
				string fileNameWithoutExtension = $"{(string.IsNullOrEmpty(name) ? "" : $"{name}.")}{relevantLocale}";
				yield return Path.Combine(directory, $"{fileNameWithoutExtension}.ftl");
			}
		}
	}
}