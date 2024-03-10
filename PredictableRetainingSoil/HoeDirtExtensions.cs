using StardewValley.TerrainFeatures;

namespace Shockah.PredictableRetainingSoil;

public static class HoeDirtExtensions
{
	private static readonly string Key = $"{typeof(HoeDirtExtensions).Namespace!}::RetainingSoilDaysLeft";

	public static int GetRetainingSoilDaysLeft(this HoeDirt instance)
		=> instance.modData.TryGetValue(Key, out var stringData) && int.TryParse(stringData, out var value) ? value : 0;

	public static void SetRetainingSoilDaysLeft(this HoeDirt instance, int value)
		=> instance.modData[Key] = value.ToString();
}