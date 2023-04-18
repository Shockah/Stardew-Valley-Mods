using Shockah.Kokoro.UI;
using StardewValley;

namespace Shockah.SeasonAffixes.Affixes.Neutral
{
	internal sealed class InflationAffix : BaseSeasonAffix
	{
		private static string ShortID => "Inflation";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(272, 528, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		// TODO: Inflation implementation
	}
}