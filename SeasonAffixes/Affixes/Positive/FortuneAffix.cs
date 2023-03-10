using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class FortuneAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Fortune";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public TextureRectangle Icon => new(Game1.mouseCursors, new(381, 361, 10, 10));

		public FortuneAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetPositivity(Season season, int year)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetNegativity(Season season, int year)
			=> 0;

		// TODO: Fortune implementation
	}
}