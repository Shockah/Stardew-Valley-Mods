using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class RegrowthAffix : BaseSeasonAffix
	{
		private static string ShortID => "Regrowth";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(16, 528, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		public override IReadOnlySet<string> Tags
			=> new HashSet<string> { VanillaSkill.CropsAspect };

		public override double GetProbabilityWeight(OrdinalSeason season)
			=> season.Season == Season.Winter ? 0 : 1;

		public override void OnActivate()
		{
			Mod.Helper.Events.Content.AssetRequested += OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Data\\Crops");
			UpdateExistingCrops();
		}

		public override void OnDeactivate()
		{
			Mod.Helper.Events.Content.AssetRequested -= OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Data\\Crops");
			UpdateExistingCrops();
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (!e.Name.IsEquivalentTo("Data\\Crops"))
				return;
			e.Edit(asset =>
			{
				var data = asset.AsDictionary<int, string>();
				foreach (var kvp in data.Data)
				{
					string[] split = kvp.Value.Split('/');
					if (split[4] == "-1")
					{
						int totalGrowthDays = split[0].Split(" ").Select(growthStage => int.Parse(growthStage)).Sum();
						split[4] = $"{(int)Math.Ceiling(totalGrowthDays / 3.0)}";
						data.Data[kvp.Key] = string.Join("/", split);
					}
				}
			}, priority: AssetEditPriority.Late);
		}

		private static void UpdateExistingCrops()
		{
			foreach (var location in GameExt.GetAllLocations())
			{
				foreach (var terrainFeature in location.terrainFeatures.Values)
					if (terrainFeature is HoeDirt dirt)
						if (dirt.crop is not null)
							UpdateCrop(dirt.crop);
				foreach (var @object in location.Objects.Values)
					if (@object is IndoorPot pot)
						if (pot.hoeDirt.Value?.crop is not null)
							UpdateCrop(pot.hoeDirt.Value.crop);
			}
		}

		private static void UpdateCrop(Crop crop)
		{
			Dictionary<int, string> allCropData = Game1.content.Load<Dictionary<int, string>>("Data\\Crops");
			if (!allCropData.TryGetValue(crop.netSeedIndex.Value, out var cropData))
				return;
			string[] split = cropData.Split('/');
			crop.regrowAfterHarvest.Value = Convert.ToInt32(split[4]);
		}
	}
}