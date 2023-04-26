using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.SeasonAffixes.Affixes.Neutral
{
	internal sealed class ThunderAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Thunder";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description", new { Chance = $"{Mod.Config.ThunderChance:0.##}x" });
		public override TextureRectangle Icon => new(Game1.mouseCursors, new(413, 346, 13, 13));

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
			=> Mod.Helper.ModRegistry.IsLoaded("Shockah.SafeLightning") ? 0 : 1;

		public override IReadOnlySet<string> Tags
			=> new HashSet<string> { VanillaSkill.CropsAspect };

		public override double GetProbabilityWeight(OrdinalSeason season)
			=> season.Season switch
			{
				Season.Spring or Season.Fall => 0.5,
				Season.Summer => 1,
				Season.Winter => 0,
				_ => throw new ArgumentException($"{nameof(Season)} has an invalid value."),
			};

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		public override void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.neutral.{ShortID}.config.chance", () => Mod.Config.ThunderChance, min: 0.25f, max: 4f, interval: 0.05f, value => $"{value:0.##}x");
		}

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			var game1Type = typeof(Game1);
			var newDayAfterFadeEnumeratorType = game1Type.GetNestedTypes(BindingFlags.NonPublic)
				.FirstOrDefault(t => t.Name.StartsWith("<_newDayAfterFade>") && AccessTools.Method(t, "MoveNext") is not null);

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(newDayAfterFadeEnumeratorType, "MoveNext"),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(ThunderAffix), nameof(Game1_newDayAfterFade_MoveNext_Transpiler)))
			);
		}

		private static IEnumerable<CodeInstruction> Game1_newDayAfterFade_MoveNext_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{
			try
			{
				var newLabel = il.DefineLabel();

				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.Find(
						ILMatches.Instruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Game1), nameof(Game1.random))),
						ILMatches.Call("NextDouble"),
						ILMatches.Instruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Game1), nameof(Game1.chanceToRainTomorrow))),
						ILMatches.BgeUn
					)
					.PointerMatcher(SequenceMatcherRelativeElement.First)
					.ExtractLabels(out var labels)
					.Insert(
						SequenceMatcherPastBoundsDirection.Before, true,
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThunderAffix), nameof(Game1_newDayAfterFade_MoveNext_Transpiler_ModifyChanceToRainTomorrow))).WithLabels(labels)
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Mod.Monitor.Log($"Could not patch methods - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		public static void Game1_newDayAfterFade_MoveNext_Transpiler_ModifyChanceToRainTomorrow()
		{
			if (!Mod.ActiveAffixes.Any(a => a is ThunderAffix))
				return;
			Game1.chanceToRainTomorrow *= Mod.Config.ThunderChance;
		}
	}
}