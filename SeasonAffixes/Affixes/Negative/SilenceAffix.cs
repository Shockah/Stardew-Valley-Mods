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
using System.Reflection.Emit;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class SilenceAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Silence";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description");
		public TextureRectangle Icon => new(Game1.emoteSpriteSheet, new(32, 144, 16, 16));

		public SilenceAffix() : base($"{Mod.ModManifest.UniqueID}.{ShortID}") { }

		public int GetPositivity(OrdinalSeason season)
			=> 0;

		public int GetNegativity(OrdinalSeason season)
			=> 1;

		public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { new SpaceCoreSkill("drbirbdev.Socializing").UniqueID };

		public void OnRegister()
			=> Apply(Mod.Harmony);

		public void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.negative.{ShortID}.config.friendshipGain", () => Mod.Config.SilenceFriendshipGain, min: 0, max: 250, interval: 10);
		}

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(SilenceAffix), nameof(NPC_checkAction_Transpiler)))
			);
		}

		private static IEnumerable<CodeInstruction> NPC_checkAction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
		{
			try
			{
				var newLabel = il.DefineLabel();

				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.AsAnchorable<CodeInstruction, Guid, Guid, SequencePointerMatcher<CodeInstruction>, SequenceBlockMatcher<CodeInstruction>>()
					.Find(
						ILMatches.Ldarg(1),
						ILMatches.Call("get_CanMove"),
						ILMatches.Brtrue.WithAutoAnchor(out Guid branchAnchor),
						ILMatches.LdcI4(0),
						ILMatches.Instruction(OpCodes.Ret)
					)
					.MoveToPointerAnchor(branchAnchor)
					.ExtractBranchTarget(out var branchTarget)
					.Replace(new CodeInstruction(OpCodes.Brtrue, newLabel))
					.Find(
						SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence,
						new ElementMatch<CodeInstruction>($"{{instruction with label {branchTarget}}}", i => i.labels.Contains(branchTarget))
					)
					.PointerMatcher(SequenceMatcherRelativeElement.First)
					.Insert(
						SequenceMatcherPastBoundsDirection.Before, true,

						new CodeInstruction(OpCodes.Ldarg_0).WithLabels(newLabel),
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SilenceAffix), nameof(NPC_checkAction_Transpiler_SilenceOrContinue))),
						new CodeInstruction(OpCodes.Brtrue, branchTarget),
						new CodeInstruction(OpCodes.Ldc_I4_1),
						new CodeInstruction(OpCodes.Ret)
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Mod.Monitor.Log($"Could not patch methods - {Mod.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		public static bool NPC_checkAction_Transpiler_SilenceOrContinue(NPC npc, Farmer player)
		{
			if (!Mod.ActiveAffixes.Any(a => a is SilenceAffix))
				return true;

			npc.grantConversationFriendship(player, Mod.Config.SilenceFriendshipGain);
			if (!player.isEmoting)
				player.doEmote(40);
			if (!npc.isEmoting)
				npc.doEmote(8);
			npc.shake(250);

			return false;
		}
	}
}