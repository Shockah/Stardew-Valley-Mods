using HarmonyLib;
using Microsoft.Xna.Framework;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.SeasonAffixes;

internal sealed class MigrationAffix : BaseSeasonAffix, ISeasonAffix // TODO: test in 1.6
{
	private static bool IsHarmonySetup = false;

	private static string ShortID => "Migration";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description");
	public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(32, 464, 16, 16));

	public MigrationAffix() : base(ShortID, "neutral") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> 1;

	public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.FishingAspect };
	
	public void OnRegister()
		=> Apply(Mod.Harmony);

	private void Apply(Harmony harmony)
	{
		if (IsHarmonySetup)
			return;
		IsHarmonySetup = true;

		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(GameLocation), nameof(GameLocation.GetFishFromLocationData), new Type[] { typeof(string), typeof(Vector2), typeof(int), typeof(Farmer), typeof(bool), typeof(bool), typeof(GameLocation), typeof(ItemQueryContext) }),
			transpiler: new HarmonyMethod(GetType(), nameof(GameLocation_GetFishFromLocationData_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> GameLocation_GetFishFromLocationData_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)

				.Find(
					ILMatches.Stloc<HashSet<string>>(originalMethod.GetMethodBody()!.LocalVariables)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.CreateLdlocInstruction(out var ldlocIgnoreQueryKeys)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.JustInsertion,

					ldlocIgnoreQueryKeys,
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MigrationAffix), nameof(GameLocation_GetFishFromLocationData_Transpiler_ModifyIgnoreQueryKeys)))
				)

				.Find(
					ILMatches.Ldloca<Season?>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Call("GetValueOrDefault"),
					ILMatches.Ldloc<Season>(originalMethod.GetMethodBody()!.LocalVariables),
					ILMatches.Instruction(OpCodes.Ceq)
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.JustInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MigrationAffix), nameof(GameLocation_GetFishFromLocationData_Transpiler_DoSeasonsMatch)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			Mod.Monitor.Log($"Could not patch method {originalMethod} - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
			return instructions;
		}
	}

	private static void GameLocation_GetFishFromLocationData_Transpiler_ModifyIgnoreQueryKeys(HashSet<string> ignoreQueryKeys)
	{
		foreach (var key in GameStateQuery.SeasonQueryKeys)
			ignoreQueryKeys.Add(key);
	}

	private static bool GameLocation_GetFishFromLocationData_Transpiler_DoSeasonsMatch(bool doSeasonsMatch)
		=> doSeasonsMatch || Mod.IsAffixActive(a => a is MigrationAffix);
}