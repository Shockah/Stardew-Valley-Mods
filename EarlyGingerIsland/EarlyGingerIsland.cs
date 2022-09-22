using HarmonyLib;
using Shockah.CommonModCode;
using Shockah.CommonModCode.IL;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.EarlyGingerIsland
{
	public class EarlyGingerIsland : BaseMod<ModConfig>
	{
		public static EarlyGingerIsland Instance { get; private set; } = null!;

		public override void OnEntry(IModHelper helper)
		{
			Instance = this;

			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.Content.AssetRequested += OnAssetRequested;

			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(BoatTunnel), nameof(BoatTunnel.checkAction)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(EarlyGingerIsland), nameof(BoatTunnel_checkAction_Transpiler)))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(BoatTunnel), nameof(BoatTunnel.answerDialogue)),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(EarlyGingerIsland), nameof(BoatTunnel_answerDialogue_Transpiler)))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(BoatTunnel), nameof(BoatTunnel.GetTicketPrice)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(EarlyGingerIsland), nameof(BoatTunnel_GetTicketPrice_Postfix)))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(ParrotUpgradePerch), nameof(ParrotUpgradePerch.IsAvailable)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(EarlyGingerIsland), nameof(ParrotUpgradePerch_IsAvailable_Postfix))),
				transpiler: new HarmonyMethod(AccessTools.Method(typeof(EarlyGingerIsland), nameof(ParrotUpgradePerch_IsAvailable_Transpiler)))
			);
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			if (ShouldGingerIslandBeUnlocked() && !Game1.player.hasOrWillReceiveMail("willyBackRoomInvitation"))
				Game1.player.mailbox.Add("willyBackRoomInvitation");
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (e.Name.IsEquivalentTo("Strings/Locations"))
			{
				e.Edit(rawAsset =>
				{
					var asset = rawAsset.AsDictionary<string, string>();
					asset.Data["BoatTunnel_DonateBatteries"] = asset.Data["BoatTunnel_DonateBatteries"].Replace("5", $"{Config.BoatFixBatteryPacksRequired}");
					asset.Data["BoatTunnel_DonateHardwood"] = asset.Data["BoatTunnel_DonateHardwood"].Replace("200", $"{Config.BoatFixHardwoodRequired}");
					asset.Data["BoatTunnel_DonateIridium"] = asset.Data["BoatTunnel_DonateIridium"].Replace("5", $"{Config.BoatFixIridiumBarsRequired}");
					asset.Data["BoatTunnel_DonateBatteriesHint"] = asset.Data["BoatTunnel_DonateBatteriesHint"].Replace("5", $"{Config.BoatFixBatteryPacksRequired}");
					asset.Data["BoatTunnel_DonateHardwoodHint"] = asset.Data["BoatTunnel_DonateHardwoodHint"].Replace("200", $"{Config.BoatFixHardwoodRequired}");
					asset.Data["BoatTunnel_DonateIridiumHint"] = asset.Data["BoatTunnel_DonateIridiumHint"].Replace("5", $"{Config.BoatFixIridiumBarsRequired}");
				});
			}
		}

		private bool ShouldGingerIslandBeUnlocked()
		{
			foreach (var condition in Config.UnlockConditions)
			{
				if (Game1.Date.TotalDays < condition.Date.TotalDays)
					continue;
				foreach (var player in Game1.getAllFarmers())
					if (player.getFriendshipHeartLevelForNPC("Willy") < condition.HeartsWithWilly)
						continue;
				return true;
			}
			return ShouldGingerIslandBeUnlockedInVanilla();
		}

		private bool ShouldGingerIslandBeUnlockedInVanilla()
			=> Game1.MasterPlayer.eventsSeen.Contains(191393) || Game1.MasterPlayer.eventsSeen.Contains(502261) || Game1.MasterPlayer.hasCompletedCommunityCenter();

		private static IEnumerable<CodeInstruction> BoatTunnel_checkAction_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();

			// IL to find:
			// IL_0045: ldc.i4 787
			// IL_004a: ldc.i4.5
			// IL_004b: ldc.i4.0
			// IL_004c: callvirt instance bool StardewValley.Farmer::hasItemInInventory(int32, int32, int32)
			var worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdcI4(787),
				i => i.IsLdcI4(5),
				i => i.IsLdcI4(),
				i => i.Calls(AccessTools.Method(typeof(Farmer), nameof(Farmer.hasItemInInventory)))
			});
			if (worker is null)
				return instructions;

			worker[1] = new CodeInstruction(OpCodes.Ldc_I4, Instance.Config.BoatFixBatteryPacksRequired);

			// IL to find:
			// IL_0179: ldc.i4 709
			// IL_017e: ldc.i4 200
			// IL_0183: ldc.i4.0
			// IL_0184: callvirt instance bool StardewValley.Farmer::hasItemInInventory(int32, int32, int32)
			worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdcI4(709),
				i => i.IsLdcI4(200),
				i => i.IsLdcI4(),
				i => i.Calls(AccessTools.Method(typeof(Farmer), nameof(Farmer.hasItemInInventory)))
			});
			if (worker is null)
				return instructions;

			worker[1] = new CodeInstruction(OpCodes.Ldc_I4, Instance.Config.BoatFixHardwoodRequired);

			// IL to find:
			// IL_01e8: ldc.i4 337
			// IL_01ed: ldc.i4.5
			// IL_01ee: ldc.i4.0
			// IL_01ef: callvirt instance bool StardewValley.Farmer::hasItemInInventory(int32, int32, int32)
			worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdcI4(337),
				i => i.IsLdcI4(5),
				i => i.IsLdcI4(),
				i => i.Calls(AccessTools.Method(typeof(Farmer), nameof(Farmer.hasItemInInventory)))
			});
			if (worker is null)
				return instructions;

			worker[1] = new CodeInstruction(OpCodes.Ldc_I4, Instance.Config.BoatFixIridiumBarsRequired);

			return instructions;
		}

		private static IEnumerable<CodeInstruction> BoatTunnel_answerDialogue_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();

			// IL to find:
			// IL_00e9: call class StardewValley.Farmer StardewValley.Game1::get_player()
			// IL_00ee: ldc.i4 787
			// IL_00f3: ldc.i4.5
			// IL_00f4: callvirt instance bool StardewValley.Farmer::removeItemsFromInventory(int32, int32)
			var worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.Calls(AccessTools.PropertyGetter(typeof(Game1), nameof(Game1.player))),
				i => i.IsLdcI4(787),
				i => i.IsLdcI4(5),
				i => i.Calls(AccessTools.Method(typeof(Farmer), nameof(Farmer.removeItemsFromInventory)))
			});
			if (worker is null)
				return instructions;

			worker[2] = new CodeInstruction(OpCodes.Ldc_I4, Instance.Config.BoatFixBatteryPacksRequired);

			// IL to find:
			// IL_013c: callvirt instance void StardewValley.Multiplayer::globalChatInfoMessage(string, string[])
			// IL_0141: call class StardewValley.Farmer StardewValley.Game1::get_player()
			// IL_0146: ldc.i4 709
			// IL_014b: ldc.i4 200
			// IL_0150: callvirt instance bool StardewValley.Farmer::removeItemsFromInventory(int32, int32)
			worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.Calls(AccessTools.PropertyGetter(typeof(Game1), nameof(Game1.player))),
				i => i.IsLdcI4(709),
				i => i.IsLdcI4(200),
				i => i.Calls(AccessTools.Method(typeof(Farmer), nameof(Farmer.removeItemsFromInventory)))
			});
			if (worker is null)
				return instructions;

			worker[2] = new CodeInstruction(OpCodes.Ldc_I4, Instance.Config.BoatFixHardwoodRequired);

			// IL to find:
			// IL_019d: call class StardewValley.Farmer StardewValley.Game1::get_player()
			// IL_01a2: ldc.i4 337
			// IL_01a7: ldc.i4.5
			// IL_01a8: callvirt instance bool StardewValley.Farmer::removeItemsFromInventory(int32, int32)
			worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.Calls(AccessTools.PropertyGetter(typeof(Game1), nameof(Game1.player))),
				i => i.IsLdcI4(337),
				i => i.IsLdcI4(5),
				i => i.Calls(AccessTools.Method(typeof(Farmer), nameof(Farmer.removeItemsFromInventory)))
			});
			if (worker is null)
				return instructions;

			worker[2] = new CodeInstruction(OpCodes.Ldc_I4, Instance.Config.BoatFixIridiumBarsRequired);

			return instructions;
		}

		private static void BoatTunnel_GetTicketPrice_Postfix(ref int __result)
		{
			__result = Instance.Config.BoatTicketPrice;
		}

		private static void ParrotUpgradePerch_IsAvailable_Postfix(ParrotUpgradePerch __instance, ref bool __result)
		{
			if (__instance.upgradeName.Value == "Turtle" && !Instance.Config.AllowIslandFarmBeforeCC && !Instance.ShouldGingerIslandBeUnlockedInVanilla())
				__result = false;
		}

		private static IEnumerable<CodeInstruction> ParrotUpgradePerch_IsAvailable_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();

			// IL to find:
			// IL_0035: ldarg.0
			// IL_0036: ldfld class Netcode.NetString StardewValley.BellsAndWhistles.ParrotUpgradePerch::requiredMail
			// IL_003b: callvirt instance!0 class Netcode.NetFieldBase`2<string, class Netcode.NetString>::get_Value()
			// IL_0040: ldc.i4.s 44
			// IL_0042: ldc.i4.0
			// IL_0043: callvirt instance string[][System.Runtime] System.String::Split(char, valuetype[System.Runtime] System.StringSplitOptions)
			// IL_0048: stloc.0
			var worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdarg(0),
				i => i.LoadsField(AccessTools.Field(typeof(ParrotUpgradePerch), nameof(ParrotUpgradePerch.requiredMail))),
				i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "get_Value",
				i => i.IsLdcI4(44),
				i => i.IsLdcI4(),
				i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "Split",
				i => i.IsStloc()
			});
			if (worker is null)
				return instructions;

			worker.Postfix(new[]
			{
				worker[6].ToLoadLocal()!,
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(EarlyGingerIsland), nameof(ParrotUpgradePerch_IsAvailable_Transpiler_ModifyRequiredMails))),
				worker[6].ToStoreLocal()!
			});

			return instructions;
		}

		public static string[] ParrotUpgradePerch_IsAvailable_Transpiler_ModifyRequiredMails(string[] requiredMails)
		{
			if (!Instance.Config.AllowIslandFarmBeforeCC)
			{
				for (int i = 0; i < requiredMails.Length; i++)
					if (requiredMails[i] is "Island_Turtle" or "Island_W_Obelisk" or "Island_UpgradeHouse_Mailbox" or "Island_UpgradeHouse" or "Island_UpgradeParrotPlatform")
						requiredMails[i] = "Island_FirstParrot";
			}
			return requiredMails;
		}
	}
}