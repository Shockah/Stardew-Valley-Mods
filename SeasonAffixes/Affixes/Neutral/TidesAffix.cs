using Microsoft.Xna.Framework.Graphics;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Neutral
{
	internal sealed class TidesAffix : BaseSeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Tides";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.content.Load<Texture2D>("Minigames\\MineCart"), new(48, 256, 16, 16));

		public TidesAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		// TODO: Tides implementation
	}
}