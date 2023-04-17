using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using Shockah.Kokoro;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Network;
using StardewValley.TerrainFeatures;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using xTile.Dimensions;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Neutral
{
	internal sealed class WildGrowthAffix : BaseSeasonAffix
	{
		private static string ShortID => "WildGrowth";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(336, 192, 16, 16));

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		public override void OnActivate()
		{
			Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;
		}

		public override void OnDeactivate()
		{
			Mod.Helper.Events.GameLoop.DayStarted -= OnDayStarted;
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			if (!Context.IsMainPlayer)
				return;

			var farm = Game1.getFarm();
			foreach (var tree in farm.terrainFeatures.Values.OfType<Tree>().ToList())
			{
				bool wasFertilized = tree.fertilized.Value;
				tree.fertilized.Value = true;
				tree.dayUpdate(new FakeLocation(farm), tree.currentTileLocation);
				tree.fertilized.Value = wasFertilized;

				if (Game1.random.NextBool())
				{
					int xCoord = Game1.random.Next(-3, 4) + (int)tree.currentTileLocation.X;
					int yCoord = Game1.random.Next(-3, 4) + (int)tree.currentTileLocation.Y;
					Vector2 location = new(xCoord, yCoord);
					var noSpawn = farm.doesTileHaveProperty(xCoord, yCoord, "NoSpawn", "Back");
					if ((noSpawn is null || (!noSpawn.Equals("Tree") && !noSpawn.Equals("All") && !noSpawn.Equals("True"))) && farm.isTileLocationOpen(new Location(xCoord, yCoord)) && !farm.isTileOccupied(location) && farm.doesTileHaveProperty(xCoord, yCoord, "Water", "Back") is null && farm.isTileOnMap(location))
						farm.terrainFeatures.Add(location, new Tree(tree.treeType.Value, 0));
				}
			}
		}

		private sealed class FakeLocation : GameLocation
		{
			private static readonly Lazy<Action<GameLocation, NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>>>> TerrainFeaturesSetter = new(() => AccessTools.Field(typeof(GameLocation), "terrainFeatures").EmitInstanceSetter<GameLocation, NetVector2Dictionary<TerrainFeature, NetRef<TerrainFeature>>>());
			private static readonly Lazy<Action<GameLocation, OverlaidDictionary>> ObjectsSetter = new(() => AccessTools.Field(typeof(GameLocation), "objects").EmitInstanceSetter<GameLocation, OverlaidDictionary>());

			private readonly GameLocation Wrapped;

			public FakeLocation(GameLocation wrapped)
			{
				this.Wrapped = wrapped;
				TerrainFeaturesSetter.Value(this, wrapped.terrainFeatures);
				ObjectsSetter.Value(this, wrapped.objects);
			}

			public override SObject? getObjectAt(int x, int y)
				=> Wrapped.getObjectAt(x, y);

			public override string? doesTileHaveProperty(int xTile, int yTile, string propertyName, string layerName)
				=> Wrapped.doesTileHaveProperty(xTile, yTile, propertyName, layerName);

			public override bool CanPlantTreesHere(int sapling_index, int tile_x, int tile_y)
				=> Wrapped.CanPlantTreesHere(sapling_index, tile_x, tile_y);

			public override bool isTileOccupied(Vector2 tileLocation, string? characterToIgnore = "", bool ignoreAllCharacters = false)
				=> Wrapped.isTileOccupied(tileLocation, characterToIgnore, ignoreAllCharacters);
		}
	}
}