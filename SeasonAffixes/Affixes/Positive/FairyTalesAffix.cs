﻿using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Events;
using System;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes;

partial class ModConfig
{
	[JsonProperty] public float FairyTalesChance { get; internal set; } = 0.15f;
}

internal sealed class FairyTalesAffix : BaseSeasonAffix, ISeasonAffix
{
	private static bool IsHarmonySetup = false;

	private static string ShortID => "FairyTales";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description", new { Chance = $"{(int)(Mod.Config.FairyTalesChance * 100):0.##}%" });
	public TextureRectangle Icon => new(Game1.content.Load<Texture2D>("LooseSprites\\temporary_sprites_1"), new(2, 129, 18, 16));

	public FairyTalesAffix() : base(ShortID, "positive") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> 0;

	public double GetProbabilityWeight(OrdinalSeason season)
	{
		if (Mod.Config.ChoicePeriod == AffixSetChoicePeriod.Day)
			return 0;
		if (!Mod.Config.WinterCrops && season.Season == Season.Winter)
			return 0.5;
		return 1;
	}

	public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.CropsAspect, VanillaSkill.FlowersAspect, VanillaSkill.AnimalsAspect };

	public void OnRegister()
		=> Apply(Mod.Harmony);

	public void SetupConfig(IManifest manifest)
	{
		var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
		GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
		helper.AddNumberOption($"{I18nPrefix}.config.chance", () => Mod.Config.FairyTalesChance, min: 0.01f, max: 1f, interval: 0.01f, value => $"{(int)(value * 100):0.##}%");
	}

	private void Apply(Harmony harmony)
	{
		if (IsHarmonySetup)
			return;
		IsHarmonySetup = true;

		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(Utility), nameof(Utility.pickFarmEvent)),
			postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(Utility_pickFarmEvent_Postfix)))
		);
	}

	private static void Utility_pickFarmEvent_Postfix(ref FarmEvent? __result)
	{
		if (!Mod.IsAffixActive(a => a is FairyTalesAffix))
			return;
		if (__result is not null)
			return;

		Random random = new((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2);
		if (random.NextDouble() > Mod.Config.FairyTalesChance)
			return;
		__result = random.NextBool() ? new FairyEvent() : new WitchEvent();
	}
}