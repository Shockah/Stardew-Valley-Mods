using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace Shockah.MailPersistenceFramework
{
	internal class ModOverrideEntry
	{
		public IManifest Mod { get; private set; }
		public Action<string, string, string, Action<string>>? Text { get; private set; }
		public Action<string, string, IReadOnlyList<Item>, Action<IEnumerable<Item>>>? Items { get; private set; }
		public Action<string, string, string?, Action<string?>>? Recipe { get; private set; }

		public ModOverrideEntry(
			IManifest mod,
			Action<string, string, string, Action<string>>? text,
			Action<string, string, IReadOnlyList<Item>, Action<IEnumerable<Item>>>? items,
			Action<string, string, string?, Action<string?>>? recipe
		)
		{
			this.Mod = mod;
			this.Text = text;
			this.Items = items;
			this.Recipe = recipe;
		}
	}
}
