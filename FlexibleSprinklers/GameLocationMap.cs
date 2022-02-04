using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using SObject = StardewValley.Object;

namespace Shockah.FlexibleSprinklers
{
	internal class GameLocationMap: IMap
	{
		private readonly GameLocation location;

		internal GameLocationMap(GameLocation location)
		{
			this.location = location;
		}

		public override bool Equals(object obj)
		{
			return obj is GameLocationMap map && location == map.location;
		}

		public override int GetHashCode()
		{
			return location.GetHashCode();
		}

		public bool Equals(IMap other)
		{
			return Equals((object)other);
		}

		public SoilType this[IntPoint point]
		{
			get
			{
				var tileVector = new Vector2(point.X, point.Y);
				if (location.Objects.TryGetValue(tileVector, out SObject @object) && @object.IsSprinkler())
					return SoilType.Sprinkler;
				if (!location.terrainFeatures.TryGetValue(tileVector, out TerrainFeature feature) || feature is not HoeDirt)
					return SoilType.NonSoil;
				if (location.doesTileHaveProperty(point.X, point.Y, "NoSprinklers", "Back")?.StartsWith("T", StringComparison.InvariantCultureIgnoreCase) == true)
					return SoilType.NonWaterable;

				var soil = (HoeDirt)feature;
				return soil.needsWatering() ? SoilType.Dry : SoilType.Wet;
			}
		}

		public void WaterTile(IntPoint point)
		{
			var can = new WateringCan();
			var tileVector = new Vector2(point.X, point.Y);

			if (location.terrainFeatures.TryGetValue(tileVector, out TerrainFeature feature))
				feature.performToolAction(can, 0, tileVector, location);
			if (location.Objects.TryGetValue(tileVector, out SObject @object))
				@object.performToolAction(can, location);

			// TODO: add animation, if needed
		}
	}
}