using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public class RawContentPackContent
	{
		public ISemanticVersion? Format { get; set; }
		public IList<AdditionalFluentPath>? AdditionalFluentPaths { get; set; }
		public IList<AdditionalI18nPath>? AdditionalI18nPaths { get; set; }

		public class AdditionalFluentPath
		{
			public string? LocalizedMod { get; set; }
			public string? LocalizingMod { get; set; }
			public string? LocalizedFile { get; set; }
			public string? LocalizingFile { get; set; }
			public string? LocalizingSubdirectory { get; set; }
		}

		public class AdditionalI18nPath
		{
			public string? LocalizedMod { get; set; }
			public string? LocalizingMod { get; set; }
			public string? LocalizingSubdirectory { get; set; }
		}
	}

	public record ContentPackContent
	{
		public ISemanticVersion Format { get; init; }
		public IList<AdditionalFluentPath> AdditionalFluentPaths { get; init; }
		public IList<AdditionalI18nPath> AdditionalI18nPaths { get; init; }

		public ContentPackContent(ISemanticVersion format, IList<AdditionalFluentPath> additionalFluentPaths, IList<AdditionalI18nPath> additionalI18nPaths)
		{
			this.Format = format;
			this.AdditionalFluentPaths = additionalFluentPaths;
			this.AdditionalI18nPaths = additionalI18nPaths;
		}

		public record AdditionalFluentPath
		{
			public string LocalizedMod { get; init; }
			public string LocalizingMod { get; init; }
			public string? LocalizedFile { get; init; }
			public string? LocalizingFile { get; init; }
			public string? LocalizingSubdirectory { get; set; }

			public AdditionalFluentPath(string localizedMod, string localizingMod, string? localizedFile, string? localizingFile, string? localizingSubdirectory)
			{
				this.LocalizedMod = localizedMod;
				this.LocalizingMod = localizingMod;
				this.LocalizedFile = localizedFile;
				this.LocalizingFile = localizingFile;
				this.LocalizingSubdirectory = localizingSubdirectory;
			}
		}

		public record AdditionalI18nPath
		{
			public string LocalizedMod { get; init; }
			public string LocalizingMod { get; init; }
			public string? LocalizingSubdirectory { get; set; }

			public AdditionalI18nPath(string localizedMod, string localizingMod, string? localizingSubdirectory)
			{
				this.LocalizedMod = localizedMod;
				this.LocalizingMod = localizingMod;
				this.LocalizingSubdirectory = localizingSubdirectory;
			}
		}
	}
}