using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public class ContentPackContent
	{
		public ISemanticVersion Format { get; set; }
		public IDictionary<string, string> Fluent { get; set; }

		public ContentPackContent(
			ISemanticVersion format,
			IDictionary<string, string> fluent
		)
		{
			this.Format = format;
			this.Fluent = fluent;
		}
	}
}