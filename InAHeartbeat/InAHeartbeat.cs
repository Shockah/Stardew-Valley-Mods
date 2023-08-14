using HarmonyLib;
using Microsoft.Xna.Framework;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SObject = StardewValley.Object;

namespace Shockah.InAHeartbeat;

public class InAHeartbeat : BaseMod<ModConfig>
{
	private const int EmeraldID = 60;
	private const int AquamarineID = 62;
	private const int RubyID = 64;
	private const int AmethystID = 66;
	private const int TopazID = 68;
	private const int JadeID = 70;
	private const int DiamondID = 72;
	private const int PrismaticShardID = 74;
	private const int PearlID = 797;

	private static InAHeartbeat Instance = null!;

	public override void OnEntry(IModHelper helper)
	{
		Instance = this;
		helper.Events.GameLoop.GameLaunched += OnGameLaunched;
	}

	private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
	{
		var api = Helper.ModRegistry.GetApi<IAdvancedSocialInteractionsApi>("spacechase0.AdvancedSocialInteractions");
		if (api is null)
		{
			Monitor.Log("Advanced Social Interactions is not installed. The mod will not work.", LogLevel.Error);
			return;
		}

		api.AdvancedInteractionStarted += OnAdvancedInteractionStarted;

		Harmony harmony = new(ModManifest.UniqueID);
		harmony.TryPatch(
			monitor: Monitor,
			original: () => AccessTools.Method(typeof(NPC), nameof(NPC.tryToReceiveActiveObject)),
			transpiler: new HarmonyMethod(GetType(), nameof(NPC_tryToReceiveActiveObject_Transpiler))
		);
		harmony.TryPatch(
			monitor: Monitor,
			original: () => AccessTools.Method(typeof(NPC), "engagementResponse"),
			prefix: new HarmonyMethod(GetType(), nameof(NPC_engagementResponse_Prefix))
		);
		harmony.TryPatch(
			monitor: Monitor,
			original: () => AccessTools.Method(typeof(NPC), nameof(NPC.checkAction)),
			transpiler: new HarmonyMethod(GetType(), nameof(NPC_checkAction_Transpiler))
		);
	}

	private void OnAdvancedInteractionStarted(object? sender, Action<string, Action> e)
	{
		if (sender is not NPC npc)
			return;

		if (npc.Name == "Caroline")
		{
			if (Config.IsBouquetCraftable && HasFriendshipWithAnyone(Game1.player, Config.Date.MinFriendship, DatingState.Datable))
				e(Helper.Translation.Get("action.arrangeABouquet"), () => OnArrangeABouquetAction(npc));
			if (Config.IsPendantCraftable && HasFriendshipWithAnyone(Game1.player, Config.Marry.MinFriendship, DatingState.Dating))
				e(Helper.Translation.Get("action.craftAPendant"), () => OnCraftAPendantAction(npc));
		}
	}

	private void OnArrangeABouquetAction(NPC npc)
	{
		var player = Game1.player;
		int? bestPossibleQuality = GetBestPossibleBouquetQuality(player);

		if (bestPossibleQuality is null)
		{
			Game1.drawDialogue(npc, Helper.Translation.Get("action.arrangeABouquet.notEnoughFlowers"));
			return;
		}

		// this should always succeed
		_ = ConsumeBouquetCraftingRequirements(player, bestPossibleQuality.Value);

		player.addItemByMenuIfNecessary(new SObject(458, 1, quality: bestPossibleQuality.Value));
		Game1.drawDialogue(npc, Helper.Translation.Get("action.arrangeABouquet.success"));
	}

	private void OnCraftAPendantAction(NPC npc)
	{
		var player = Game1.player;
		int? bestPossibleQuality = GetBestPossiblePendantQuality(player);

		if (bestPossibleQuality is null)
		{
			Game1.drawDialogue(npc, Helper.Translation.Get("action.craftAPendant.notEnoughMaterials"));
			return;
		}

		// this should always succeed
		_ = ConsumePendantCraftingRequirements(player, bestPossibleQuality.Value);

		player.addItemByMenuIfNecessary(new SObject(460, 1, quality: bestPossibleQuality.Value));
		Game1.drawDialogue(npc, Helper.Translation.Get("action.craftAPendant.success"));
	}

	private static bool HasFriendshipWithAnyone(Farmer player, int friendshipLevel, DatingState minimumDatingState)
	{
		foreach (NPC npc in Utility.getAllCharacters())
			if (player.getFriendshipLevelForNPC(npc.Name) >= friendshipLevel && (int)player.GetDatingState(npc) >= (int)minimumDatingState)
				return true;
		return false;
	}

	private static FlowerDescriptor? GetFlowerDescriptor(SObject item)
	{
		if (item.Category != SObject.flowersCategory)
			return null;

		if (item is ColoredObject colored)
			return new(item.ParentSheetIndex, colored.color.Value);
		else
			return new(item.ParentSheetIndex, Color.White);
	}

	private static IEnumerable<(SObject Item, FlowerDescriptor FlowerDescriptor)> GetAllHeldFlowers(Farmer player)
	{
		foreach (var item in player.Items)
		{
			if (item is not SObject @object)
				continue;
			var descriptor = GetFlowerDescriptor(@object);
			if (descriptor is null)
				continue;
			yield return (Item: @object, FlowerDescriptor: descriptor.Value);
		}
	}

	private static IEnumerable<SObject> GetAllHeldGems(Farmer player)
	{
		foreach (var item in player.Items)
		{
			if (item is not SObject @object)
				continue;
			if (@object.bigCraftable.Value)
				continue;
			if (item.ParentSheetIndex is EmeraldID or AquamarineID or RubyID or AmethystID or TopazID or JadeID or DiamondID or PrismaticShardID)
				yield return @object;
		}
	}

	private int? GetBestPossibleBouquetQuality(Farmer player)
	{
		if (HasBouquetCraftingRequirements(player, SObject.bestQuality))
			return SObject.bestQuality;
		else if (HasBouquetCraftingRequirements(player, SObject.highQuality))
			return SObject.highQuality;
		else if (HasBouquetCraftingRequirements(player, SObject.medQuality))
			return SObject.medQuality;
		else if (HasBouquetCraftingRequirements(player, SObject.lowQuality))
			return SObject.lowQuality;
		else
			return null;
	}

	private int? GetBestPossiblePendantQuality(Farmer player)
	{
		if (!player.Items.Any(item => item is SObject @object && !@object.bigCraftable.Value && @object.ParentSheetIndex == PearlID))
			return null;
		var types = GetAllHeldGems(player).Select(gem => gem.ParentSheetIndex).ToHashSet();

		if (types.Count >= Config.PendantGemsRequiredForIridium)
			return SObject.bestQuality;
		else if (types.Count >= Config.PendantGemsRequiredForGold)
			return SObject.highQuality;
		else if (types.Count >= Config.PendantGemsRequiredForSilver)
			return SObject.medQuality;
		else if (types.Count >= Config.PendantGemsRequiredForRegular)
			return SObject.lowQuality;
		else
			return null;
	}

	private bool HasBouquetCraftingRequirements(Farmer player, int minimumItemQuality)
	{
		HashSet<FlowerDescriptor> uniqueFlowerTypes = new();
		int flowersLeft = Config.BouquetFlowersRequired;

		foreach (var flower in GetAllHeldFlowers(player))
		{
			if (flower.Item.Quality < minimumItemQuality)
				continue;

			uniqueFlowerTypes.Add(flower.FlowerDescriptor);
			flowersLeft -= flower.Item.Stack;

			if (flowersLeft <= 0 && uniqueFlowerTypes.Count >= Config.BouquetFlowerTypesRequired)
				return true;
		}
		return false;
	}

	private bool ConsumeBouquetCraftingRequirements(Farmer player, int itemQuality)
	{
		var itemsLeft = GetAllHeldFlowers(player)
			.Where(e => e.Item.Quality >= itemQuality)
			.OrderBy(e => e.Item.Quality)
			.ThenBy(e => e.Item.salePrice())
			.Select(e => (Item: e.Item, FlowerDescriptor: e.FlowerDescriptor, Amount: e.Item.Stack))
			.ToList();

		List<Item> itemsToConsume = new();
		HashSet<FlowerDescriptor> uniqueFlowerTypes = new();
		int flowersLeft = Config.BouquetFlowersRequired;

		void ActuallyConsume()
		{
			foreach (var item in itemsToConsume)
				player.ConsumeItem(item);
		}

		while (true)
		{
			if (itemsLeft.Count == 0)
				return false;

			for (int i = 0; i < itemsLeft.Count; i++)
			{
				var entry = itemsLeft[i];
				if (uniqueFlowerTypes.Count < Config.BouquetFlowerTypesRequired && uniqueFlowerTypes.Contains(entry.FlowerDescriptor))
					continue;

				uniqueFlowerTypes.Add(entry.FlowerDescriptor);
				flowersLeft--;
				itemsToConsume.Add(entry.Item.getOne());

				if (entry.Amount <= 1)
					itemsLeft.RemoveAt(i--);
				else
					itemsLeft[i] = (Item: entry.Item, FlowerDescriptor: entry.FlowerDescriptor, Amount: entry.Amount - 1);

				if (flowersLeft <= 0 && uniqueFlowerTypes.Count >= Config.BouquetFlowerTypesRequired)
				{
					ActuallyConsume();
					return true;
				}
			}

			if (uniqueFlowerTypes.Count < Config.BouquetFlowerTypesRequired)
				return false;
		}
	}

	private bool ConsumePendantCraftingRequirements(Farmer player, int itemQuality)
	{
		int gemsRequired = itemQuality switch
		{
			SObject.bestQuality => Config.PendantGemsRequiredForIridium,
			SObject.highQuality => Config.PendantGemsRequiredForGold,
			SObject.medQuality => Config.PendantGemsRequiredForSilver,
			_ => Config.PendantGemsRequiredForRegular
		};

		var gems = GetAllHeldGems(player).OrderBy(gem => gem.salePrice());
		HashSet<int> uniqueGemTypes = new();

		foreach (var gem in gems)
		{
			if (uniqueGemTypes.Contains(gem.ParentSheetIndex))
				continue;
			uniqueGemTypes.Add(gem.ParentSheetIndex);
			if (uniqueGemTypes.Count >= gemsRequired)
				break;
		}

		if (uniqueGemTypes.Count < gemsRequired)
			return false;

		if (!player.ConsumeItem(new SObject(PearlID, 1), exactQuality: false))
			return false;

		foreach (var gemType in uniqueGemTypes)
			player.ConsumeItem(new SObject(gemType, 1), exactQuality: false);
		return true;
	}

	private static int GetFriendshipRequirement(ActionConfig config, int itemQuality)
	{
		return itemQuality switch
		{
			SObject.medQuality => config.SilverFriendship,
			SObject.highQuality => config.GoldFriendship,
			SObject.bestQuality => config.IridiumFriendship,
			_ => config.RegularFriendship
		};
	}

	private static IEnumerable<CodeInstruction> NPC_tryToReceiveActiveObject_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AsGuidAnchorable()

				.Find(
					ILMatches.Call("get_Points"),
					ILMatches.LdcI4(1000).WithAutoAnchor(out Guid requirementAnchor),
					ILMatches.AnyBranch
				)
				.PointerMatcher(requirementAnchor)
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InAHeartbeat), nameof(GetDateFriendshipRequirement))),
					new CodeInstruction(OpCodes.Ldc_I4_2),
					new CodeInstruction(OpCodes.Div)
				)

				.Find(
					ILMatches.Call("get_Points"),
					ILMatches.LdcI4(2000).WithAutoAnchor(out requirementAnchor),
					ILMatches.AnyBranch
				)
				.PointerMatcher(requirementAnchor)
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InAHeartbeat), nameof(GetDateFriendshipRequirement)))
				)

				.Find(
					ILMatches.Call("get_Points"),
					ILMatches.LdcI4(2500).WithAutoAnchor(out requirementAnchor),
					ILMatches.AnyBranch
				)
				.PointerMatcher(requirementAnchor)
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InAHeartbeat), nameof(GetMarryFriendshipRequirement)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Monitor.Log($"Could not patch method {originalMethod} - {Instance.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
			return instructions;
		}
	}

	private static void NPC_engagementResponse_Prefix(NPC __instance, Farmer who, bool asRoommate)
		=> SetMarriedFriendshipRequirement(__instance, asRoommate ? Instance.Config.Marry.RegularFriendship : GetMarryFriendshipRequirement(who));

	private static IEnumerable<CodeInstruction> NPC_checkAction_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.AsGuidAnchorable()
				.Find(
					ILMatches.Ldarg(1).WithAutoAnchor(out Guid findHeadAnchor),
					ILMatches.Ldarg(0),
					ILMatches.Call("get_Name"),
					ILMatches.Call("getFriendshipHeartLevelForNPC"),
					ILMatches.LdcI4(9),
					ILMatches.Ble.WithAutoAnchor(out Guid kissDenyAnchor)
				)
				.PointerMatcher(kissDenyAnchor)
				.ExtractBranchTarget(out Label kissDenyBranchLabel)
				.EncompassUntil(findHeadAnchor)
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(NPC), nameof(NPC.Name))),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Farmer), nameof(Farmer.getFriendshipLevelForNPC))),
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InAHeartbeat), nameof(GetMarriedFriendshipRequirement))),
					new CodeInstruction(OpCodes.Blt, kissDenyBranchLabel)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Monitor.Log($"Could not patch method {originalMethod} - {Instance.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
			return instructions;
		}
	}

	public static int GetDateFriendshipRequirement(Farmer player)
		=> GetFriendshipRequirement(Instance.Config.Date, player.ActiveObject.Quality);

	public static int GetMarryFriendshipRequirement(Farmer player)
		=> GetFriendshipRequirement(Instance.Config.Marry, player.ActiveObject.Quality);

	public static int GetMarriedFriendshipRequirement(NPC npc)
	{
		int marriedFriendshipRequirement = Instance.Config.Marry.RegularFriendship;
		if (npc.modData.TryGetValue($"{Instance.ModManifest.UniqueID}/MarriedFriendshipRequirement", out var marriedFriendshipRequirementString))
			if (int.TryParse(marriedFriendshipRequirementString, out var parsedMarriedFriendshipRequirement))
				marriedFriendshipRequirement = parsedMarriedFriendshipRequirement;
		return marriedFriendshipRequirement;
	}

	public static void SetMarriedFriendshipRequirement(NPC npc, int value)
		=> npc.modData[$"{Instance.ModManifest.UniqueID}/MarriedFriendshipRequirement"] = $"{value}";
}