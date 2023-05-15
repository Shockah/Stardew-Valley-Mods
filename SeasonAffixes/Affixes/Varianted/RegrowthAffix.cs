using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes.Affixes.Varianted
{
	internal sealed class RegrowthAffix : BaseVariantedSeasonAffix, ISeasonAffix
	{
		private static string ShortPositiveID => "Regrowth";
		private static string ShortNegativeID => "PoorYields";
		public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description");

		public TextureRectangle Icon
			=> Variant == AffixVariant.Positive
			? new(Game1.objectSpriteSheet, new(16, 528, 16, 16))
			: new(Game1.objectSpriteSheet, new(0, 0, 16, 16));

		public RegrowthAffix(AffixVariant variant) : base(variant == AffixVariant.Positive ? ShortPositiveID : ShortNegativeID, variant)
		{
			Tags = Variant == AffixVariant.Positive
				? new HashSet<string> { VanillaSkill.CropsAspect, VanillaSkill.FlowersAspect }
				: new HashSet<string> { VanillaSkill.CropsAspect };
		}

		public int GetPositivity(OrdinalSeason season)
			=> Variant == AffixVariant.Positive ? 1 : 0;

		public int GetNegativity(OrdinalSeason season)
			=> Variant == AffixVariant.Negative ? 1 : 0;

		public IReadOnlySet<string> Tags { get; init; }

		public double GetProbabilityWeight(OrdinalSeason season)
			=> Mod.Config.WinterCrops || season.Season != Season.Winter ? 1 : 0;

		public void OnActivate()
		{
			Mod.Helper.Events.Content.AssetRequested += OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Data\\Crops");
			UpdateExistingCrops();
		}

		public void OnDeactivate()
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
					if (Variant == AffixVariant.Positive)
					{
						if (split[4] == "-1")
						{
							int totalGrowthDays = split[0].Split(" ").Select(growthStage => int.Parse(growthStage)).Sum();
							split[4] = $"{(int)Math.Ceiling(totalGrowthDays / 3.0)}";
							data.Data[kvp.Key] = string.Join("/", split);
						}
					}
					else
					{
						split[4] = "-1";
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