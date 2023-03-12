using Microsoft.Xna.Framework.Graphics;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.Stardew.Skill;
using Shockah.Kokoro.UI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class CrowsAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Crows";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description");
		public TextureRectangle Icon => new(Game1.content.Load<Texture2D>(Critter.critterTexture), new(134, 46, 21, 17));

		public CrowsAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetPositivity(Season season, int year)
			=> 0;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetNegativity(Season season, int year)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public double GetProbabilityWeight(Season season, int year)
			=> season == Season.Winter ? 0 : 1;

		bool ISeasonAffix.ShouldConflict(ISeasonAffix affix)
			=> affix.UniqueID == $"{Mod.ModManifest.UniqueID}.Skill:{VanillaSkill.Farming.UniqueID}";

		// TODO: Crows implementation
	}
}