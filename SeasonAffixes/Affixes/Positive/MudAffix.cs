using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using StardewValley.Locations;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes;

internal sealed class MudAffix : BaseSeasonAffix, ISeasonAffix
{
	private static bool IsHarmonySetup = false;

	private static string ShortID => "Mud";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description");
	public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(288, 208, 16, 16));

	public MudAffix() : base(ShortID, "positive") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> 0;

	public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.CropsAspect, VanillaSkill.FlowersAspect };

	public double GetProbabilityWeight(OrdinalSeason season)
		=> Game1.whichFarm != 6 && (Mod.Config.WinterCrops || season.Season != Season.Winter) ? 1 : 0;

	public void OnRegister()
		=> Apply(Mod.Harmony);

	private void Apply(Harmony harmony)
	{
		if (IsHarmonySetup)
			return;
		IsHarmonySetup = true;

		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(BuildableGameLocation), nameof(BuildableGameLocation.doesTileHaveProperty)),
			postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(BuildableGameLocation_doesTileHaveProperty_Postfix)))
		);

		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(BuildableGameLocation), nameof(BuildableGameLocation.doesTileHavePropertyNoNull)),
			postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(BuildableGameLocation_doesTileHavePropertyNoNull_Postfix)))
		);
	}

	private static void BuildableGameLocation_doesTileHaveProperty_Postfix(BuildableGameLocation __instance, string propertyName, ref string? __result)
	{
		if (!Mod.IsAffixActive(a => a is MudAffix))
			return;
		if (__instance is Farm && propertyName == "NoSprinklers")
			__result = null;
	}

	private static void BuildableGameLocation_doesTileHavePropertyNoNull_Postfix(BuildableGameLocation __instance, string propertyName, ref string __result)
	{
		if (!Mod.IsAffixActive(a => a is MudAffix))
			return;
		if (__instance is Farm && propertyName == "NoSprinklers")
			__result = "";
	}
}