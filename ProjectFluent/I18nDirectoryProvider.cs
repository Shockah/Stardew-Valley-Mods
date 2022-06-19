using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal interface II18nDirectoryProvider
	{
		event Action<II18nDirectoryProvider>? DirectoriesChanged;

		IEnumerable<string> GetI18nDirectories(IManifest mod);
	}

	internal class SerialI18nDirectoryProvider: II18nDirectoryProvider
	{
		public event Action<II18nDirectoryProvider>? DirectoriesChanged;

		private II18nDirectoryProvider[] Providers { get; set; }

		public SerialI18nDirectoryProvider(params II18nDirectoryProvider[] providers)
		{
			// making a copy on purpose
			this.Providers = providers.ToArray();

			foreach (var provider in providers)
				provider.DirectoriesChanged += OnDirectoriesChanged;
		}

		public IEnumerable<string> GetI18nDirectories(IManifest mod)
		{
			foreach (var provider in Providers)
				foreach (var path in provider.GetI18nDirectories(mod))
					yield return path;
		}

		private void OnDirectoriesChanged(II18nDirectoryProvider provider)
			=> DirectoriesChanged?.Invoke(this);
	}

	internal class AssetI18nDirectoryProvider: II18nDirectoryProvider, IDisposable
	{
		private static readonly string AssetPath = "Shockah.ProjectFluent/AdditionalI18nPaths";

		public event Action<II18nDirectoryProvider>? DirectoriesChanged;

		private IContentEvents ContentEvents { get; set; }
		private IPathTokenReplacer PathTokenReplacer { get; set; }

		public AssetI18nDirectoryProvider(IContentEvents contentEvents, IPathTokenReplacer pathTokenReplacer)
		{
			this.ContentEvents = contentEvents;
			this.PathTokenReplacer = pathTokenReplacer;

			contentEvents.AssetRequested += OnAssetRequested;
			contentEvents.AssetsInvalidated += OnAssetsInvalidated;
		}

		public void Dispose()
		{
			ContentEvents.AssetRequested -= OnAssetRequested;
			ContentEvents.AssetsInvalidated -= OnAssetsInvalidated;
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.Name.IsEquivalentTo(AssetPath))
				e.LoadFrom(() => new Dictionary<string, List<string>>(), AssetLoadPriority.Exclusive);
		}

		private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
		{
			foreach (var name in e.Names)
			{
				if (name.IsEquivalentTo(AssetPath))
				{
					DirectoriesChanged?.Invoke(this);
					break;
				}
			}
		}

		public IEnumerable<string> GetI18nDirectories(IManifest mod)
		{
			var asset = Game1.content.Load<Dictionary<string, List<string>>>(AssetPath);
			if (asset is null)
				yield break;

			if (asset.TryGetValue(mod.UniqueID, out var entries))
			{
				foreach (string entry in entries)
				{
					string entryPath = entry;
					entryPath = PathTokenReplacer.ReplaceTokens(entryPath, mod, null);
					yield return entryPath;
				}
			}
		}
	}

	internal class ContentPackI18nDirectoryProvider: II18nDirectoryProvider, IDisposable
	{
		public event Action<II18nDirectoryProvider>? DirectoriesChanged;

		private IContentPackManager ContentPackManager { get; set; }
		private IPathTokenReplacer PathTokenReplacer { get; set; }

		public ContentPackI18nDirectoryProvider(
			IContentPackManager contentPackManager,
			IPathTokenReplacer pathTokenReplacer
		)
		{
			this.ContentPackManager = contentPackManager;
			this.PathTokenReplacer = pathTokenReplacer;

			contentPackManager.ContentPacksContentsChanged += OnContentPacksContentsChanges;
		}

		public void Dispose()
		{
			ContentPackManager.ContentPacksContentsChanged -= OnContentPacksContentsChanges;
		}

		private void OnContentPacksContentsChanges(IContentPackManager contentPackManager)
			=> DirectoriesChanged?.Invoke(this);

		public IEnumerable<string> GetI18nDirectories(IManifest mod)
		{
			foreach (var (pack, content) in ContentPackManager.GetContentPackContents())
			{
				if (content.AdditionalI18nPaths is null)
					continue;
				foreach (var (localizedModID, entry) in content.AdditionalI18nPaths)
				{
					if (!localizedModID.Equals(mod.UniqueID, StringComparison.InvariantCultureIgnoreCase))
						continue;

					string entryPath = entry;
					entryPath = entryPath.Replace("%this%", pack.DirectoryPath, StringComparison.InvariantCultureIgnoreCase);
					entryPath = PathTokenReplacer.ReplaceTokens(entryPath, mod, null);
					yield return entryPath;
				}
			}
		}
	}
}