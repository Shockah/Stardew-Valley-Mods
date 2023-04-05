using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class SkillAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }
		public ISkill Skill { get; init; }
		private double Weight { get; init; }

		private static string ShortID => "Skill";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}:{Skill.UniqueID}";
		public string LocalizedName => Skill.Name;
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description", new { Skill = Skill.Name });
		public TextureRectangle Icon => Skill.Icon!; // TODO: placeholder icon

		public SkillAffix(SeasonAffixes mod, ISkill skill, double weight)
		{
			this.Mod = mod;
			this.Skill = skill;
			this.Weight = weight;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetPositivity(Season season, int year)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetNegativity(Season season, int year)
			=> 0;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public double GetProbabilityWeight(Season season, int year)
			=> Weight;

		// TODO: Skill implementation
	}
}