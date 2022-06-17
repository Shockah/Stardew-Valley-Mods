using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal class AssetFileResolvingFluent: IFluent<string>
	{
		internal IGameLocale Locale { get; private set; }
		internal IManifest Mod { get; private set; }
		internal string? Name { get; private set; }
		private IFluent<string> Fallback { get; set; }

		private IList<(string directoryPath, string? name)>? CachedPaths { get; set; }
		private IFluent<string>? CachedFluent { get; set; }

		private IFluent<string> CurrentFluent
		{
			get
			{
				if (CachedFluent is null)
					CachedFluent = new FileResolvingFluent(Locale, GetFilePathCandidates(), Fallback);
				return CachedFluent;
			}
		}

		public AssetFileResolvingFluent(
			IGameLocale locale,
			IManifest mod,
			string? name,
			IFluent<string> fallback
		)
		{
			this.Locale = locale;
			this.Mod = mod;
			this.Name = name;
			this.Fallback = fallback;
		}

		internal void OnAssetChanged(Dictionary<string, List<string>>? asset)
		{
			if (asset is null)
			{
				CachedPaths = null;
				CachedFluent = null;
				return;
			}

			var newDirectoryPaths = GetDirectoryPaths(asset).ToList();
			if (CachedPaths is null || !newDirectoryPaths.SequenceEqual(CachedPaths))
			{
				CachedPaths = newDirectoryPaths;
				CachedFluent = null;
			}
		}

		private IEnumerable<(string directoryPath, string? name)> GetDirectoryPaths(Dictionary<string, List<string>> asset)
		{
			string assetKey = Name is null ? Mod.UniqueID : $"{Mod.UniqueID}::{Name}";
			if (asset.TryGetValue(assetKey, out var entries))
			{
				foreach (var entry in entries)
				{
					var split = entry.Split("::");
					bool isDirectory = Directory.Exists(split[0]);
					string? name = split.Length >= 2 ? split[1] : null;

					if (isDirectory)
					{
						string path = split[0];
						yield return (path, name);
					}
					else
					{
						string uniqueModID = split[0];
						IManifest? mod = ProjectFluent.Instance.Helper.ModRegistry.Get(uniqueModID)?.Manifest;
						if (mod is not null)
						{
							var modPath = ProjectFluent.Instance.GetModDirectoryPath(mod);
							if (modPath is not null)
								yield return (Path.Combine(modPath, "i18n"), name);
						}
					}
				}
			}

			{
				var modPath = ProjectFluent.Instance.GetModDirectoryPath(Mod);
				if (modPath is not null)
					yield return (Path.Combine(modPath, "i18n"), Name);
			}
		}

		private IEnumerable<string> GetFilePathCandidates()
		{
			if (CachedPaths is null)
				yield break;
			foreach (var (directoryPath, name) in CachedPaths)
				foreach (var candidate in ProjectFluent.Instance.GetFilePathCandidates(directoryPath, name, Locale))
					yield return candidate;
		}

		public bool ContainsKey(string key)
		{
			return CurrentFluent.ContainsKey(key);
		}

		public string Get(string key, object? tokens)
		{
			return CurrentFluent.Get(key, tokens);
		}
	}
}