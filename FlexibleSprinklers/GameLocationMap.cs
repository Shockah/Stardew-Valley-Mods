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
		private readonly GameLocation Location;

		internal GameLocationMap(GameLocation location)
		{
			this.Location = location;
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
				if (!Location.terrainFeatures.TryGetValue(tileVector, out TerrainFeature feature) || feature is not HoeDirt)
					return SoilType.NonSoil;
				if (Location.doesTileHaveProperty(point.X, point.Y, "NoSprinklers", "Back")?.StartsWith("T", StringComparison.InvariantCultureIgnoreCase) == true)
					return SoilType.NonWaterable;

				var soil = (HoeDirt)feature;
				return soil.state.Value == 0 ? SoilType.Dry : SoilType.Wet;
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