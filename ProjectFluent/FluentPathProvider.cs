using System.Collections.Generic;
using System.IO;

namespace Shockah.ProjectFluent
{
	internal interface IFluentPathProvider
	{
		IEnumerable<string> GetFilePathCandidates(IGameLocale locale, string directory, string? file);
	}

	internal class FluentPathProvider: IFluentPathProvider
	{
		public IEnumerable<string> GetFilePathCandidates(IGameLocale locale, string directory, string? file)
		{
			foreach (var relevantLocale in locale.GetRelevantLocaleCodes())
			{
				string fileNameWithoutExtension = $"{(string.IsNullOrEmpty(file) ? "" : $"{file}.")}{relevantLocale}";
				yield return Path.Combine(directory, $"{fileNameWithoutExtension}.ftl");
			}
		}
	}
}