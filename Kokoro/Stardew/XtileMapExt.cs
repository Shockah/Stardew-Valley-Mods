using StardewValley;

namespace Shockah.Kokoro.Stardew
{
	public static class XtileMapExt
	{
		public static xTile.Dimensions.Size GetSize(this xTile.Map map)
			=> new(map.DisplayWidth / Game1.tileSize, map.DisplayHeight / Game1.tileSize);
	}
}