using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public class ContentPackContent
	{
		public ISemanticVersion Format { get; set; }
		public IDictionary<string, string>? AdditionalFluentPaths { get; set; }
		public IDictionary<string, string>? AdditionalI18nPaths { get; set; }

		public ContentPackContent(
			ISemanticVersion format,
			IDictionary<string, string>? additionalFluentPaths,
			IDictionary<string, string>? additionalI18nPaths
		)
		{
			this.Format = format;
			this.AdditionalFluentPaths = additionalFluentPaths;
			this.AdditionalI18nPaths = additionalI18nPaths;
		}
	}
}