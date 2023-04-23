using Microsoft.Xna.Framework.Graphics;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes.Affixes.Neutral
{
	internal sealed class TidesAffix : BaseSeasonAffix
	{
		private static string ShortID => "Tides";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.content.Load<Texture2D>("Minigames\\MineCart"), new(48, 256, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		public override IReadOnlySet<string> Tags
			=> new HashSet<string> { VanillaSkill.Fishing.UniqueID };

		public override void OnActivate()
		{
			Mod.Helper.Events.Content.AssetRequested += OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Data\\Fish");
		}

		public override void OnDeactivate()
		{
			Mod.Helper.Events.Content.AssetRequested -= OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Data\\Fish");
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (!e.Name.IsEquivalentTo("Data\\Fish"))
				return;
			e.Edit(asset =>
			{
				var data = asset.AsDictionary<int, string>();
				foreach (var kvp in data.Data)
				{
					string[] split = kvp.Value.Split('/');
					if (split[1] == "trap")
						continue;

					double spawnMultiplier = double.Parse(split[10]);
					double depthMultiplier = double.Parse(split[11]);
					double totalMultiplier = spawnMultiplier + depthMultiplier;

					double newMultiplier = (1.0 - Math.Pow(totalMultiplier, 0.75)) * Math.Sin(totalMultiplier * Math.PI) + Math.Pow(totalMultiplier, 4.0) / 8.0;
					split[10] = $"{spawnMultiplier * newMultiplier / totalMultiplier}";
					split[11] = $"{depthMultiplier * newMultiplier / totalMultiplier}";
					data.Data[kvp.Key] = string.Join("/", split);
				}
			}, priority: AssetEditPriority.Late);
		}
	}
}