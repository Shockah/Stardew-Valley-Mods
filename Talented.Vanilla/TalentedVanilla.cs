using Shockah.Kokoro;
using Shockah.Kokoro.Stardew.Skill;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Shockah.Talented.Vanilla
{
	public class TalentedVanilla : BaseMod
	{
		public override void Entry(IModHelper helper)
		{
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			ITalentedApi? api = Helper.ModRegistry.GetApi<ITalentedApi>("Shockah.Talented");
			if (api is not null)
				SetupTalents(api);
		}

		private void SetupTalents(ITalentedApi api)
		{
			SetupFarmingTalents(api);
			SetupMiningTalents(api);
			SetupForagingTalents(api);
			SetupFishingTalents(api);
			SetupCombatTalents(api);
		}

		private void SetupFarmingTalents(ITalentedApi api)
		{
			var mainTag = new SkillTalentTag($"{ModManifest.UniqueID}.Farming", VanillaSkill.Farming);
			api.RegisterTalentTag(mainTag);

			var cropsTag = new BasicTalentTag($"{mainTag.UniqueID}.Crops", () => new(Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 472, 16, 16)), () => "Crops", mainTag);
			api.RegisterTalentTag(cropsTag);

			var animalsTag = new BasicTalentTag($"{mainTag.UniqueID}.Animals", () => new(Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 184, 16, 16)), () => "Animals", mainTag);
			api.RegisterTalentTag(animalsTag);
		}

		private void SetupMiningTalents(ITalentedApi api)
		{
			var mainTag = new SkillTalentTag($"{ModManifest.UniqueID}.Mining", VanillaSkill.Mining);
			api.RegisterTalentTag(mainTag);
		}

		private void SetupForagingTalents(ITalentedApi api)
		{
			var mainTag = new SkillTalentTag($"{ModManifest.UniqueID}.Foraging", VanillaSkill.Foraging);
			api.RegisterTalentTag(mainTag);
		}

		private void SetupFishingTalents(ITalentedApi api)
		{
			var mainTag = new SkillTalentTag($"{ModManifest.UniqueID}.Fishing", VanillaSkill.Fishing);
			api.RegisterTalentTag(mainTag);
		}

		private void SetupCombatTalents(ITalentedApi api)
		{
			var mainTag = new SkillTalentTag($"{ModManifest.UniqueID}.Combat", VanillaSkill.Combat);
			api.RegisterTalentTag(mainTag);
		}
	}
}