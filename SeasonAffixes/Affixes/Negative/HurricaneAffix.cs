using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class HurricaneAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Hurricane";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description");
		public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(368, 224, 16, 16));

		public HurricaneAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetPositivity(Season season, int year)
			=> 0;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetNegativity(Season season, int year)
			=> 1;

		// TODO: Hurricane implementation
	}
}