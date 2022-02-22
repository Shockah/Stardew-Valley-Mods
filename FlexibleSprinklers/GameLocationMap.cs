using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace Shockah.FlexibleSprinklers
{
	internal class GameLocationMap: IMap
	{
		private readonly GameLocation Location;
		private readonly IEnumerable<Func<GameLocation, Vector2, bool?>> CustomWaterableTileProviders;

		internal GameLocationMap(GameLocation location, IEnumerable<Func<GameLocation, Vector2, bool?>> customWaterableTileProviders)
		{
			this.Location = location;
			this.CustomWaterableTileProviders = customWaterableTileProviders;
		}

		public override bool Equals(object? obj)
			=> obj is IMap other && Equals(other);

		public override int GetHashCode()
			=> Location.GetHashCode();

		public bool Equals(IMap? other)
			=> other is GameLocationMap map && (ReferenceEquals(Location, map.Location) || Location == map.Location);

		public SoilType this[IntPoint point]
		{
			get
			{
				var tileVector = new Vector2(point.X, point.Y);
				if (Location.Objects.TryGetValue(tileVector, out SObject @object) && @object.IsSprinkler())
					return SoilType.Sprinkler;

				foreach (var provider in CustomWaterableTileProviders)
				{
					bool? result = provider(Location, tileVector);
					if (result.HasValue)
						return result.Value ? SoilType.Waterable : SoilType.NonWaterable;
				}

				if (!Location.terrainFeatures.TryGetValue(tileVector, out TerrainFeature feature) || feature is not HoeDirt)
					return SoilType.NonWaterable;
				if (Location.doesTileHaveProperty(point.X, point.Y, "NoSprinklers", "Back")?.StartsWith("T", StringComparison.InvariantCultureIgnoreCase) == true)
					return SoilType.NonWaterable;
				return SoilType.Waterable;
			}
		}

		public void WaterTile(IntPoint point)
		{
			var can = new WateringCan();
			var tileVector = new Vector2(point.X, point.Y);

			if (Location.terrainFeatures.TryGetValue(tileVector, out TerrainFeature feature))
				feature.performToolAction(can, 0, tileVector, Location);
			if (Location.Objects.TryGetValue(tileVector, out SObject @object))
				@object.performToolAction(can, Location);

			// TODO: add animation, if needed
		}
	}
}