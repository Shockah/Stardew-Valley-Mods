using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew.Skill;
using Shockah.Talented.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Shockah.Talented
{
	public class Talented : BaseMod
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
	}
}