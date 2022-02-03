using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace Shockah.FlexibleSprinklers
{
	internal class GameLocationMap: IMap
	{
		private readonly GameLocation location;

		internal GameLocationMap(GameLocation location)
		{
			this.location = location;
		}

		public SoilType this[IntPoint point]
		{
			get
			{
				var tileVector = new Vector2(point.X, point.Y);
				if (location.Objects.TryGetValue(tileVector, out Object @object) && @object.IsSprinkler())
					return SoilType.Sprinkler;
				if (!location.terrainFeatures.TryGetValue(tileVector, out TerrainFeature feature) || feature is not HoeDirt)
					return SoilType.NonSoil;
				if (location.doesTileHaveProperty(point.X, point.Y, "NoSprinklers", "Back")?.StartsWith("T", System.StringComparison.InvariantCultureIgnoreCase) == true)
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
			if (location.Objects.TryGetValue(tileVector, out Object @object))
				@object.performToolAction(can, location);

			// TODO: add animation, if needed
		}
	}
}