using Newtonsoft.Json;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public class ContentPackContent
	{
		public ISemanticVersion Format { get; set; }
		public IDictionary<string, string>? Fluent { get; set; }
		[JsonProperty("i18n")] public IDictionary<string, string>? I18n { get; set; }

		public ContentPackContent(
			ISemanticVersion format,
			IDictionary<string, string>? fluent,
			IDictionary<string, string>? i18n
		)
		{
			this.Format = format;
			this.I18n = i18n;
		}
	}
}