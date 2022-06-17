using Shockah.CommonModCode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal class AssetManager
	{
		private static readonly string FluentPathsAssetPath = "Shockah.ProjectFluent/Fluent";

		private bool DidRegisterEvents { get; set; } = false;
		private IList<(IContentPack pack, ContentPackContent content)> ContentPackContents { get; set; } = new List<(IContentPack pack, ContentPackContent content)>();
		private IList<WeakReference<AssetOverrideFileResolvingFluent>> Fluents { get; set; } = new List<WeakReference<AssetOverrideFileResolvingFluent>>();

		internal void RegisterEvents(IModEvents events)
		{
			if (DidRegisterEvents)
				return;
			events.Content.AssetRequested += OnAssetRequested;
			events.Content.AssetReady += OnAssetReady;
			events.Content.AssetsInvalidated += OnAssetsInvalidated;
			DidRegisterEvents = true;
		}

		internal void RegisterContentPacks(IContentPackHelper helper, IGameContentHelper contentHelper)
		{
			ProjectFluent.Instance.Monitor.Log("Loading content packs...", LogLevel.Info);
			foreach (var pack in helper.GetOwned())
				RegisterContentPack(pack, contentHelper);
		}

		internal void RegisterContentPack(IContentPack pack, IGameContentHelper contentHelper)
		{
			ProjectFluent.Instance.Monitor.Log($"Loading content pack `{pack.Manifest.UniqueID}`", LogLevel.Info);

			(IContentPack pack, ContentPackContent content)? existingEntry = ContentPackContents.FirstOrNull(e => e.pack.Manifest.UniqueID == pack.Manifest.UniqueID);
			if (existingEntry is not null)
			{
				ContentPackContents.Remove(existingEntry.Value);
				if (existingEntry.Value.content.Fluent.Count != 0)
					contentHelper.InvalidateCache(FluentPathsAssetPath);
			}

			if (!pack.HasFile("content.json"))
				return;

			try
			{
				var content = pack.ReadJsonFile<ContentPackContent>("content.json");
				if (content is null)
					return;

				ContentPackContents.Add((pack: pack, content: content));
				if (content.Fluent.Count != 0)
					contentHelper.InvalidateCache(FluentPathsAssetPath);
			}
			catch (Exception ex)
			{
				ProjectFluent.Instance.Monitor.Log($"There was an error while reading `content.json` for the `{pack.Manifest.UniqueID}` content pack:\n{ex}", LogLevel.Error);
			}
		}

		internal IFluent<string> GetFluent(IGameLocale locale, IManifest mod, string? name = null)
		{
			var toRemove = Fluents.Where(r => !r.TryGetTarget(out _)).ToList();
			foreach (var reference in toRemove)
				Fluents.Remove(reference);

			foreach (var reference in Fluents)
			{
				if (!reference.TryGetTarget(out var cached))
					continue;
				if (cached.Locale.LanguageCode == locale.LanguageCode && cached.Mod.UniqueID == mod.UniqueID && cached.Name == name)
					return cached;
			}

			var fluent = new AssetOverrideFileResolvingFluent(locale, mod, name, ProjectFluent.Instance.GetFallbackFluent(mod));
			var asset = Game1.content.Load<Dictionary<string, List<string>>>(FluentPathsAssetPath);
			fluent.OnAssetChanged(asset);
			Fluents.Add(new WeakReference<AssetOverrideFileResolvingFluent>(fluent));
			return fluent;
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.Name.IsEquivalentTo(FluentPathsAssetPath))
			{
				e.LoadFrom(() =>
				{
					var asset = new Dictionary<string, List<string>>();
					foreach (var (pack, content) in ContentPackContents)
					{
						foreach (var (key, value) in content.Fluent)
						{
							if (!asset.TryGetValue(key, out var values))
							{
								values = new List<string>();
								asset[key] = values;
							}
							values.Add(value.Equals("<this>", StringComparison.InvariantCultureIgnoreCase) ? pack.Manifest.UniqueID : value);
						}
					}
					return asset;
				}, AssetLoadPriority.Medium);
			}
		}

		private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
		{
			if (e.Names.Any(n => n.IsEquivalentTo(FluentPathsAssetPath)))
			{
				foreach (var reference in Fluents)
				{
					if (!reference.TryGetTarget(out var cached))
						continue;
					cached.OnAssetChanged(null);
				}
			}
		}

		private void OnAssetReady(object? sender, AssetReadyEventArgs e)
		{
			if (e.Name.IsEquivalentTo(FluentPathsAssetPath))
			{
				var asset = Game1.content.Load<Dictionary<string, List<string>>>(FluentPathsAssetPath);
				foreach (var reference in Fluents)
				{
					if (!reference.TryGetTarget(out var cached))
						continue;
					cached.OnAssetChanged(asset);
				}
			}
		}
	}
}