using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;

namespace Shockah.SeasonAffixes;

partial class ModConfig
{
	[JsonProperty] public float WildGrowthAdvanceChance { get; internal set; } = 1f;
	[JsonProperty] public float WildGrowthNewSeedChance { get; internal set; } = 0.5f;
}

internal sealed class WildGrowthAffix : BaseSeasonAffix, ISeasonAffix
{
	private static string ShortID => "WildGrowth";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description");
	public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(336, 192, 16, 16));

	public WildGrowthAffix() : base(ShortID, "neutral") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> 1;

	public double GetProbabilityWeight(OrdinalSeason season)
		=> Mod.Config.ChoicePeriod == AffixSetChoicePeriod.Day ? 0 : 1;

	public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.WoodcuttingAspect };

	public void OnActivate(AffixActivationContext context)
	{
		Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;
	}

	public void OnDeactivate(AffixActivationContext context)
	{
		Mod.Helper.Events.GameLoop.DayStarted -= OnDayStarted;
	}

	public void SetupConfig(IManifest manifest)
	{
		var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
		GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
		helper.AddNumberOption($"{I18nPrefix}.config.advanceChance", () => Mod.Config.WildGrowthAdvanceChance, min: 0.05f, max: 1f, interval: 0.05f, value => $"{(int)(value * 100):0.##}%");
		helper.AddNumberOption($"{I18nPrefix}.config.newSeedChance", () => Mod.Config.WildGrowthNewSeedChance, min: 0.05f, max: 1f, interval: 0.05f, value => $"{(int)(value * 100):0.##}%");
	}

	private void OnDayStarted(object? sender, DayStartedEventArgs e)
	{
		if (!Context.IsMainPlayer)
			return;

		var farm = Game1.getFarm();
		foreach (var tree in farm.terrainFeatures.Values.OfType<Tree>().ToList())
		{
			if (tree.growthStage.Value < tree.GetMaxSizeHere(ignoreSeason: true) && Game1.random.NextDouble() < Mod.Config.WildGrowthAdvanceChance)
				tree.growthStage.Value++;
			if (tree.growthStage.Value >= Tree.treeStage && Game1.random.NextDouble() < Mod.Config.WildGrowthNewSeedChance)
			{
				int xCoord = Game1.random.Next(-3, 4) + (int)tree.Tile.X;
				int yCoord = Game1.random.Next(-3, 4) + (int)tree.Tile.Y;
				Vector2 tile = new(xCoord, yCoord);
				if (!farm.IsNoSpawnTile(tile, "Tree") && farm.isTileLocationOpen(new Location(xCoord, yCoord)) && !farm.IsTileOccupiedBy(tile) && !farm.isWaterTile(xCoord, yCoord) && farm.isTileOnMap(tile))
					farm.terrainFeatures.Add(tile, new Tree(tree.treeType.Value, 0));
			}
		}
	}
}