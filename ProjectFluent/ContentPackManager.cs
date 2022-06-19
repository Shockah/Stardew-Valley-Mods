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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested interfaces")]
		internal interface WithRegisteringCapability: IContentPackManager
		{
			void RegisterAllContentPacks();
			void RegisterContentPack(IContentPack pack);
		}
	}

	internal class ContentPackManager: IContentPackManager.WithRegisteringCapability
	{
		public event Action<IContentPackManager>? ContentPacksContentsChanged;

		private ISemanticVersion ProjectFluentVersion { get; set; }
		private IMonitor Monitor { get; set; }
		private IModRegistry ModRegistry { get; set; }
		private IContentPackHelper ContentPackHelper { get; set; }

		private IList<(IContentPack pack, ContentPackContent content)> ContentPackContents { get; set; } = new List<(IContentPack pack, ContentPackContent content)>();

		public ContentPackManager(ISemanticVersion projectFluentVersion, IMonitor monitor, IModRegistry modRegistry, IContentPackHelper contentPackHelper)
		{
			this.ProjectFluentVersion = projectFluentVersion;
			this.Monitor = monitor;
			this.ModRegistry = modRegistry;
			this.ContentPackHelper = contentPackHelper;
		}

		public void RegisterAllContentPacks()
		{
			Monitor.Log("Loading content packs...", LogLevel.Info);
			foreach (var pack in ContentPackHelper.GetOwned())
				RegisterContentPack(pack);
		}

		public void RegisterContentPack(IContentPack pack)
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
					var rawContent = pack.ReadJsonFile<RawContentPackContent>("content.json");
					if (rawContent is null)
						return;

					var content = Parse(pack, rawContent);
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

		private ContentPackContent? Parse(IContentPack pack, RawContentPackContent content)
		{
			if (content.Format is null)
			{
				Monitor.Log($"`{pack.Manifest.UniqueID}`: `content.json` is missing the `Format` field.", LogLevel.Error);
				return null;
			}

			if (content.Format.IsNewerThan(ProjectFluentVersion))
			{
				Monitor.Log($"`{pack.Manifest.UniqueID}`: `content.json`: `Format` is newer than {ProjectFluentVersion} and cannot be parsed.", LogLevel.Error);
				return null;
			}

			List<ContentPackContent.AdditionalFluentPath> additionalFluentPaths = new();
			if (content.AdditionalFluentPaths is not null)
			{
				foreach (var entry in content.AdditionalFluentPaths)
				{
					if (entry.LocalizedMod is null)
					{
						Monitor.Log($"`{pack.Manifest.UniqueID}`: `content.json`: `AdditionalFluentPaths`: An entry is missing the `LocalizedMod` field.", LogLevel.Error);
						return null;
					}

					string localizingMod = entry.LocalizingMod ?? "this";
					if (!localizingMod.Equals("this", StringComparison.InvariantCultureIgnoreCase))
					{
						var localizingModInstance = ModRegistry.Get(localizingMod);
						if (localizingModInstance is null)
						{
							Monitor.Log($"`{pack.Manifest.UniqueID}`: `content.json`: `AdditionalFluentPaths`: Provided `LocalizingMod` `{localizingMod}` is not currently loaded.", LogLevel.Error);
							return null;
						}
					}

					additionalFluentPaths.Add(new(
						entry.LocalizedMod,
						localizingMod,
						entry.LocalizingFile,
						entry.LocalizedFile,
						entry.LocalizingSubdirectory
					));
				}
			}

			List<ContentPackContent.AdditionalI18nPath> additionalI18nPaths = new();
			if (content.AdditionalI18nPaths is not null)
			{
				foreach (var entry in content.AdditionalI18nPaths)
				{
					if (entry.LocalizedMod is null)
					{
						Monitor.Log($"`{pack.Manifest.UniqueID}`: `content.json`: `AdditionalI18nPaths`: An entry is missing the `LocalizedMod` field.", LogLevel.Error);
						return null;
					}

					string localizingMod = entry.LocalizingMod ?? "this";
					if (!localizingMod.Equals("this", StringComparison.InvariantCultureIgnoreCase))
					{
						var localizingModInstance = ModRegistry.Get(localizingMod);
						if (localizingModInstance is null)
						{
							Monitor.Log($"`{pack.Manifest.UniqueID}`: `content.json`: `AdditionalI18nPaths`: Provided `LocalizingMod` `{localizingMod}` is not currently loaded.", LogLevel.Error);
							return null;
						}
					}

					additionalI18nPaths.Add(new(
						entry.LocalizedMod,
						localizingMod,
						entry.LocalizingSubdirectory
					));
				}
			}

			return new(
				content.Format,
				additionalFluentPaths,
				additionalI18nPaths
			);
		}

		public IEnumerable<(IContentPack pack, ContentPackContent content)> GetContentPackContents()
			=> ContentPackContents;
	}
}