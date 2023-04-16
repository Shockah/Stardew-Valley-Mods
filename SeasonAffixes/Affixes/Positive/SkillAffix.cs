using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class SkillAffix : BaseSeasonAffix, ISeasonAffix
	{
		public ISkill Skill { get; init; }

		private static string ShortID => "Skill";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}:{Skill.UniqueID}";
		public override string LocalizedName => Skill.Name;
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description.{(Skill is VanillaSkill ? "Vanilla" : "SpaceCore")}", new { Skill = Skill.Name });
		public override TextureRectangle Icon => Skill.Icon!; // TODO: placeholder icon

		private int? LastXP = null;

		public SkillAffix(ISkill skill)
		{
			this.Skill = skill;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public double GetProbabilityWeight(OrdinalSeason season)
			=> Math.Min(2.0 / Mod.AllAffixes.Values.Count(affix => affix is SkillAffix), 1.0);

		public override void OnActivate()
		{
			UpdateXP();
			Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;
			Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

			if (Skill is VanillaSkill skill)
				ModifySkillLevel(Game1.player, skill, 3);
		}

		public override void OnDeactivate()
		{
			if (Skill is VanillaSkill skill)
				ModifySkillLevel(Game1.player, skill, -3);

			Mod.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
			LastXP = null;
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			if (Skill is not VanillaSkill skill)
				return;
			ModifySkillLevel(Game1.player, skill, 3);
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			UpdateXP();
		}

		private static void ModifySkillLevel(Farmer player, VanillaSkill skill, int levels)
		{
			switch (skill.SkillIndex)
			{
				case Farmer.farmingSkill:
					player.addedFarmingLevel.Value += levels;
					break;
				case Farmer.miningSkill:
					player.addedMiningLevel.Value += levels;
					break;
				case Farmer.foragingSkill:
					player.addedForagingLevel.Value += levels;
					break;
				case Farmer.fishingSkill:
					player.addedFishingLevel.Value += levels;
					break;
				case Farmer.combatSkill:
					player.addedCombatLevel.Value += levels;
					break;
				case Farmer.luckSkill:
					player.addedLuckLevel.Value += levels;
					break;
				default:
					throw new ArgumentException($"{nameof(skill.SkillIndex)} has an invalid value.");
			}
		}

		private void UpdateXP()
		{
			// TODO: Skill Rings compatibility - i'm assuming right now they will trigger off each other

			int newXP = Skill.GetXP(Game1.player);
			if (LastXP is null)
			{
				LastXP = newXP;
				return;
			}

			int extraXP = newXP - LastXP.Value;
			if (extraXP > 0)
			{
				int bonusXP = (int)Math.Ceiling(extraXP * (Skill is VanillaSkill ? 0.2 : 0.25));
				Skill.GrantXP(Game1.player, bonusXP);
				LastXP += extraXP;
			}
		}
	}
}