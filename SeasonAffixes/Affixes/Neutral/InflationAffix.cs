using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Neutral
{
	internal sealed class InflationAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Inflation";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description");
		public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(272, 528, 16, 16));

		public InflationAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetPositivity(Season season, int year)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetNegativity(Season season, int year)
			=> 1;

		// TODO: Inflation implementation
	}
}