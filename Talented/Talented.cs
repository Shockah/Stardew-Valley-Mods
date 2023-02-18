using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew.Skill;
using Shockah.Talented.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace Shockah.Talented
{
	public class Talented : BaseMod, ITalentedApi
	{
		internal static Talented Instance = null!;

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.Display.MenuChanged += OnMenuChanged;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			var harmony = new Harmony(ModManifest.UniqueID);
			GameMenuPatches.Apply(harmony);
			SkillsPagePatches.Apply(harmony);
		}

		private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
		{
			SkillsPagePatches.OnMenuChanged(sender, e);
		}

		internal bool HasTalentDefinitions(ISkill skill)
			=> skill is VanillaSkill vanilla && vanilla.SkillIndex is Farmer.fishingSkill or Farmer.foragingSkill;

		internal bool HasUnspentTalentPoints(ISkill skill)
			=> skill is VanillaSkill vanilla && vanilla.SkillIndex is Farmer.fishingSkill;

		public ITalentedApi.IRequirementFactories Factories => throw new NotImplementedException();

		public IReadOnlyList<ITalentTag> RootTalentTags
			=> new List<ITalentTag>()
			{
				new SkillTalentTag("StardewValley.Farming", VanillaSkill.Farming),
				new SkillTalentTag("StardewValley.Mining", VanillaSkill.Mining),
				new SkillTalentTag("StardewValley.Foraging", VanillaSkill.Foraging),
				new SkillTalentTag("StardewValley.Fishing", VanillaSkill.Fishing),
				new SkillTalentTag("StardewValley.Combat", VanillaSkill.Combat)
			};

		public IReadOnlyList<ITalentTag> GetChildTalentTags(ITalentTag parent)
		{
			throw new NotImplementedException();
		}

		public IReadOnlyList<ITalent> GetTalents(ITalentTag tag)
		{
			throw new NotImplementedException();
		}
	}
}