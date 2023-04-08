using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class SkillAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }
		public ISkill Skill { get; init; }

		private static string ShortID => "Skill";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}:{Skill.UniqueID}";
		public string LocalizedName => Skill.Name;
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description.{(VanillaSkill.GetAllSkills().Contains(Skill) ? "Vanilla" : "SpaceCore")}", new { Skill = Skill.Name });
		public TextureRectangle Icon => Skill.Icon!; // TODO: placeholder icon

		public SkillAffix(SeasonAffixes mod, ISkill skill)
		{
			this.Mod = mod;
			this.Skill = skill;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetPositivity(OrdinalSeason season)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetNegativity(OrdinalSeason season)
			=> 0;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public double GetProbabilityWeight(OrdinalSeason season)
			=> Math.Min(2.0 / SeasonAffixes.Instance.AllAffixes.Values.Count(affix => affix is SkillAffix), 1.0);

		// TODO: Skill implementation
	}
}