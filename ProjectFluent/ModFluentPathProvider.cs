using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal interface IModFluentPathProvider
	{
		event Action<IModFluentPathProvider>? CandidatesChanged;

		IEnumerable<string> GetFilePathCandidates(IGameLocale locale, IManifest mod, string? name);
	}

	internal class SerialModDirectoryFluentPathProvider: IModFluentPathProvider, IDisposable
	{
		public event Action<IModFluentPathProvider>? CandidatesChanged;

		private IModFluentPathProvider[] Providers { get; set; }

		public SerialModDirectoryFluentPathProvider(params IModFluentPathProvider[] providers)
		{
			// making a copy on purpose
			this.Providers = providers.ToArray();

			foreach (var provider in providers)
				provider.CandidatesChanged += OnCandidatesChanged;
		}

		public void Dispose()
		{
			foreach (var provider in Providers)
				provider.CandidatesChanged -= OnCandidatesChanged;
		}

		public IEnumerable<string> GetFilePathCandidates(IGameLocale locale, IManifest mod, string? name)
		{
			foreach (var provider in Providers)
				foreach (var candidate in provider.GetFilePathCandidates(locale, mod, name))
					yield return candidate;
		}

		private void OnCandidatesChanged(IModFluentPathProvider provider)
			=> CandidatesChanged?.Invoke(this);
	}

	internal class ModFluentPathProvider: IModFluentPathProvider
	{
		// never invoked, this provider does not change the candidates
		public event Action<IModFluentPathProvider>? CandidatesChanged;

		private IModDirectoryProvider ModDirectoryProvider { get; set; }
		private IFluentPathProvider FluentPathProvider { get; set; }
		private IGameLocale? LocaleOverride { get; set; }

		public ModFluentPathProvider(IModDirectoryProvider modDirectoryProvider, IFluentPathProvider fluentPathProvider, IGameLocale? localeOverride = null)
		{
			this.ModDirectoryProvider = modDirectoryProvider;
			this.FluentPathProvider = fluentPathProvider;
			this.LocaleOverride = localeOverride;
		}

		public IEnumerable<string> GetFilePathCandidates(IGameLocale locale, IManifest mod, string? name)
		{
			var baseModPath = ModDirectoryProvider.GetModDirectoryPath(mod);
			if (baseModPath is null)
				yield break;
			foreach (var candidate in FluentPathProvider.GetFilePathCandidates(LocaleOverride ?? locale, Path.Combine(baseModPath, "i18n"), name))
				yield return candidate;
		}
	}

	internal class AssetAdditionalModFluentPathProvider: IModFluentPathProvider, IDisposable
	{
		private static readonly string AssetPath = "Shockah.ProjectFluent/AdditionalFluentPaths";

		public event Action<IModFluentPathProvider>? CandidatesChanged;

		private IContentEvents ContentEvents { get; set; }
		private IFluentPathProvider FluentPathProvider { get; set; }
		private IPathTokenReplacer PathTokenReplacer { get; set; }

		public AssetAdditionalModFluentPathProvider(IContentEvents contentEvents, IFluentPathProvider fluentPathProvider, IPathTokenReplacer pathTokenReplacer)
		{
			this.ContentEvents = contentEvents;
			this.FluentPathProvider = fluentPathProvider;
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
					CandidatesChanged?.Invoke(this);
					break;
				}
			}
		}

		public IEnumerable<string> GetFilePathCandidates(IGameLocale locale, IManifest mod, string? name)
		{
			var asset = Game1.content.Load<Dictionary<string, List<string>>>(AssetPath);
			if (asset is null)
				yield break;

			string requestedKey = name is null ? mod.UniqueID : $"{mod.UniqueID}::{name}";
			if (asset.TryGetValue(requestedKey, out var entries))
			{
				foreach (string entry in entries)
				{
					var split = entry.Split("::");
					string entryPath = split[0];
					string? entryName = split.Length >= 2 ? split[1] : null;

					entryPath = PathTokenReplacer.ReplaceTokens(entryPath, mod, locale);
					foreach (var candidate in FluentPathProvider.GetFilePathCandidates(locale, entryPath, entryName))
						yield return candidate;
				}
			}
		}
	}

	internal class ContentPackAdditionalModFluentPathProvider: IModFluentPathProvider, IDisposable
	{
		public event Action<IModFluentPathProvider>? CandidatesChanged;

		private IContentPackManager ContentPackManager { get; set; }
		private IFluentPathProvider FluentPathProvider { get; set; }
		private IPathTokenReplacer PathTokenReplacer { get; set; }

		public ContentPackAdditionalModFluentPathProvider(
			IContentPackManager contentPackManager,
			IFluentPathProvider fluentPathProvider,
			IPathTokenReplacer pathTokenReplacer
		)
		{
			this.ContentPackManager = contentPackManager;
			this.FluentPathProvider = fluentPathProvider;
			this.PathTokenReplacer = pathTokenReplacer;

			contentPackManager.ContentPacksContentsChanged += OnContentPacksContentsChanges;
		}

		public void Dispose()
		{
			ContentPackManager.ContentPacksContentsChanged -= OnContentPacksContentsChanges;
		}

		private void OnContentPacksContentsChanges(IContentPackManager contentPackManager)
			=> CandidatesChanged?.Invoke(this);

		public IEnumerable<string> GetFilePathCandidates(IGameLocale locale, IManifest mod, string? name)
		{
			string requestedKey = name is null ? mod.UniqueID : $"{mod.UniqueID}::{name}";
			foreach (var (pack, content) in ContentPackManager.GetContentPackContents())
			{
				if (content.AdditionalFluentPaths is null)
					continue;
				foreach (var (localizedModKey, entry) in content.AdditionalFluentPaths)
				{
					if (!localizedModKey.Equals(requestedKey, StringComparison.InvariantCultureIgnoreCase))
						continue;

					var split = entry.Split("::");
					string entryPath = split[0];
					string? entryName = split.Length >= 2 ? split[1] : null;

					entryPath = entryPath.Replace("%this%", pack.DirectoryPath, StringComparison.InvariantCultureIgnoreCase);
					entryPath = PathTokenReplacer.ReplaceTokens(entryPath, mod, locale);
					foreach (var candidate in FluentPathProvider.GetFilePathCandidates(locale, entryPath, entryName))
						yield return candidate;
				}
			}
		}
	}
}