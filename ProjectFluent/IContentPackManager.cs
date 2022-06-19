using Shockah.CommonModCode;
using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	internal interface IContentPackManager
	{
		event Action<IContentPackManager>? ContentPacksContentsChanged;

		IEnumerable<(IContentPack pack, ContentPackContent content)> GetContentPackContents();
	}

	internal class ContentPackManager: IContentPackManager
	{
		public event Action<IContentPackManager>? ContentPacksContentsChanged;

		private IMonitor Monitor { get; set; }

		private IList<(IContentPack pack, ContentPackContent content)> ContentPackContents { get; set; } = new List<(IContentPack pack, ContentPackContent content)>();

		public ContentPackManager(IMonitor monitor, IContentPackHelper contentPackHelper)
		{
			this.Monitor = monitor;

			RegisterContentPacks(contentPackHelper);
		}

		private void RegisterContentPacks(IContentPackHelper contentPackHelper)
		{
			Monitor.Log("Loading content packs...", LogLevel.Info);
			foreach (var pack in contentPackHelper.GetOwned())
				RegisterContentPack(pack);
		}

		private void RegisterContentPack(IContentPack pack)
		{
			bool changedContentPacks = false;
			try
			{
				Monitor.Log($"Loading content pack `{pack.Manifest.UniqueID}`", LogLevel.Info);

				(IContentPack pack, ContentPackContent content)? existingEntry = ContentPackContents.FirstOrNull(e => e.pack.Manifest.UniqueID == pack.Manifest.UniqueID);
				if (existingEntry is not null)
				{
					ContentPackContents.Remove(existingEntry.Value);
					changedContentPacks = true;
				}

				if (!pack.HasFile("content.json"))
					return;

				try
				{
					var content = pack.ReadJsonFile<ContentPackContent>("content.json");
					if (content is null)
						return;

					ContentPackContents.Add((pack: pack, content: content));
					changedContentPacks = true;
				}
				catch (Exception ex)
				{
					Monitor.Log($"There was an error while reading `content.json` for the `{pack.Manifest.UniqueID}` content pack:\n{ex}", LogLevel.Error);
				}
			}
			finally
			{
				if (changedContentPacks)
					ContentPacksContentsChanged?.Invoke(this);
			}
			
		}

		public IEnumerable<(IContentPack pack, ContentPackContent content)> GetContentPackContents()
			=> ContentPackContents;
	}
}