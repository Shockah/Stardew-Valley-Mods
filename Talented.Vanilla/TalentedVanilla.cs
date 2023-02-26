using Shockah.Kokoro;
using Shockah.Kokoro.Stardew.Skill;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Shockah.Talented.Vanilla
{
	public class TalentedVanilla : BaseMod
	{
		#region Farming talent properties
		private ITalent? FarmingProficiency1Talent { get; set; }
		private ITalent? FarmingProficiency2Talent { get; set; }
		private ITalent? FarmingProficiency3Talent { get; set; }
		private ITalent? Resurgence1Talent { get; set; }
		private ITalent? Resurgence2Talent { get; set; }
		private ITalent? Resurgence3Talent { get; set; }
		#endregion

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

			api.RegisterTalent(FarmingProficiency1Talent = new Talent(
				$"{mainTag.UniqueID}.FarmingProficiency.I",
				() => VanillaSkill.Farming.Icon!,
				() => "Farming Proficiency I",
				() => "Reduced farming (hoe / watering can) energy usage by 10%.",
				ReplacedTalent: null,
				Tag: cropsTag,
				Requirements: null
			));

			api.RegisterTalent(FarmingProficiency2Talent = new Talent(
				$"{mainTag.UniqueID}.FarmingProficiency.II",
				() => VanillaSkill.Farming.Icon!,
				() => "Farming Proficiency II",
				() => "Reduced farming (hoe / watering can) energy usage by 25%.",
				ReplacedTalent: FarmingProficiency1Talent,
				Tag: cropsTag,
				Requirements: api.RequirementFactories.Tag(cropsTag, 2)
			));

			api.RegisterTalent(FarmingProficiency3Talent = new Talent(
				$"{mainTag.UniqueID}.FarmingProficiency.III",
				() => VanillaSkill.Farming.Icon!,
				() => "Farming Proficiency III",
				() => "Reduced farming (hoe / watering can) energy usage by 50%.",
				ReplacedTalent: FarmingProficiency2Talent,
				Tag: cropsTag,
				Requirements: api.RequirementFactories.Tag(cropsTag, 4)
			));

			api.RegisterTalent(Resurgence1Talent = new Talent(
				$"{mainTag.UniqueID}.Resurgence.I",
				() => new(Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 370, 16, 16)),
				() => "Resurgence I",
				() => "Automatically refill a charge of a watering can every 30 in-game minutes.",
				ReplacedTalent: null,
				Tag: cropsTag,
				Requirements: api.RequirementFactories.Tag(cropsTag, 1)
			));

			api.RegisterTalent(Resurgence2Talent = new Talent(
				$"{mainTag.UniqueID}.Resurgence.II",
				() => new(Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 370, 16, 16)),
				() => "Resurgence II",
				() => "Automatically refill a charge of a watering can every 20 in-game minutes.",
				ReplacedTalent: Resurgence1Talent,
				Tag: cropsTag,
				Requirements: api.RequirementFactories.Tag(cropsTag, 3)
			));

			api.RegisterTalent(Resurgence3Talent = new Talent(
				$"{mainTag.UniqueID}.Resurgence.III",
				() => new(Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 370, 16, 16)),
				() => "Resurgence III",
				() => "Automatically refill a charge of a watering can every 10 in-game minutes.",
				ReplacedTalent: Resurgence2Talent,
				Tag: cropsTag,
				Requirements: api.RequirementFactories.Tag(cropsTag, 5)
			));
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