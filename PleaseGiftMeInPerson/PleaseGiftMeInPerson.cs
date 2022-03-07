using HarmonyLib;
using Shockah.CommonModCode;
using Shockah.CommonModCode.GMCM;
using Shockah.CommonModCode.GMCM.Helper;
using Shockah.CommonModCode.IL;
using Shockah.CommonModCode.SMAPI;
using Shockah.CommonModCode.Stardew;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Xml.Serialization;
using SObject = StardewValley.Object;

namespace Shockah.PleaseGiftMeInPerson
{
	public class PleaseGiftMeInPerson: Mod
	{
		private static readonly string MailServicesMod_GiftShipmentController_QualifiedName = "MailServicesMod.GiftShipmentController, MailServicesMod";

		internal static readonly string OverrideAssetPath = "Data/PleaseGiftMeInPerson";
		private static readonly string GiftEntriesSaveDataKey = "GiftEntries";
		private static readonly string GiftEntriesMessageType = "GiftEntries";

		internal static PleaseGiftMeInPerson Instance { get; set; } = null!;
		internal ModConfig Config { get; private set; } = null!;
		private ModConfig.Entry LastDefaultConfigEntry = null!;

		private Farmer? CurrentGiftingPlayer;
		private GiftMethod? CurrentGiftMethod;
		private GiftTaste OriginalGiftTaste;
		private GiftTaste ModifiedGiftTaste;
		private int TicksUntilConfigSetup = 5;

		private readonly XmlSerializer itemSerializer = new(typeof(Item));
		private Lazy<IReadOnlyList<(string name, string displayName)>> Characters = null!;

		private IDictionary<long, IDictionary<string, IList<GiftEntry>>> GiftEntries = new Dictionary<long, IDictionary<string, IList<GiftEntry>>>();
		private readonly IDictionary<long, IList<Item>> ItemsToReturn = new Dictionary<long, IList<Item>>();

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<ModConfig>();
			LastDefaultConfigEntry = new(Config.Default);
			Helper.Content.AssetLoaders.Add(new OverrideAssetLoader());
			Helper.Content.AssetEditors.Add(new OverrideAssetEditor());

			Characters = new(() =>
			{
				var npcDispositions = Game1.content.Load<Dictionary<string, string>>("Data/NPCDispositions");
				var antiSocialNpcs = Helper.ModRegistry.IsLoaded("SuperAardvark.AntiSocial")
					? Game1.content.Load<Dictionary<string, string>>("Data/AntiSocialNPCs")
					: new();

				var characters = npcDispositions
					.Select(c => (name: c.Key, displayName: c.Value.Split('/')[11]))
					.Where(c => !antiSocialNpcs.ContainsKey(c.name))
					.OrderBy(c => c.displayName)
					.ToArray();
				return characters;
			});

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.GameLoop.Saving += OnSaving;
			helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
			helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				original: () => AccessTools.Method(AccessTools.TypeByName(MailServicesMod_GiftShipmentController_QualifiedName), "GiftToNpc"),
				Monitor,
				prefix: new HarmonyMethod(typeof(PleaseGiftMeInPerson), nameof(GiftShipmentController_GiftToNpc_Prefix)),
				postfix: new HarmonyMethod(typeof(PleaseGiftMeInPerson), nameof(GiftShipmentController_GiftToNpc_Postfix))
			);
			harmony.TryPatch(
				original: () => AccessTools.Method(AccessTools.TypeByName(MailServicesMod_GiftShipmentController_QualifiedName), "CreateResponsePage"),
				Monitor,
				transpiler: new HarmonyMethod(typeof(PleaseGiftMeInPerson), nameof(GiftShipmentController_CreateResponsePage_Transpiler))
			);
			harmony.TryPatchVirtual(
				original: () => AccessTools.Method(typeof(NPC), nameof(NPC.tryToReceiveActiveObject)),
				Monitor,
				prefix: new HarmonyMethod(typeof(PleaseGiftMeInPerson), nameof(NPC_tryToReceiveActiveObject_Prefix)),
				postfix: new HarmonyMethod(typeof(PleaseGiftMeInPerson), nameof(NPC_tryToReceiveActiveObject_Postfix))
			);
			harmony.TryPatch(
				original: () => AccessTools.Method(typeof(NPC), nameof(NPC.getGiftTasteForThisItem)),
				Monitor,
				postfix: new HarmonyMethod(typeof(PleaseGiftMeInPerson), nameof(NPC_getGiftTasteForThisItem_Postfix))
			);
			harmony.TryPatch(
				original: () => AccessTools.Method(typeof(NPC), nameof(NPC.receiveGift)),
				Monitor,
				postfix: new HarmonyMethod(typeof(PleaseGiftMeInPerson), nameof(NPC_receiveGift_Postfix))
			);
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			if (--TicksUntilConfigSetup > 0)
				return;

			PopulateConfig(Config);
			SetupConfig();
			Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
		}

		private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
		{
			if (GameExt.GetMultiplayerMode() != MultiplayerMode.Client)
			{
				GiftEntries = Helper.Data.ReadSaveData<IDictionary<long, IDictionary<string, IList<GiftEntry>>>>(GiftEntriesSaveDataKey)
					?? new Dictionary<long, IDictionary<string, IList<GiftEntry>>>();
			}
		}

		private void OnSaving(object? sender, SavingEventArgs e)
		{
			if (GameExt.GetMultiplayerMode() == MultiplayerMode.Client)
				return;

			CleanUpGiftEntries();
			Helper.Data.WriteSaveData(GiftEntriesSaveDataKey, GiftEntries);
		}

		private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
		{
			if (GameExt.GetMultiplayerMode() != MultiplayerMode.Server)
				return;
			if (e.Peer.GetMod(ModManifest.UniqueID) is null)
				return;

			Helper.Multiplayer.SendMessage(
				GiftEntries,
				GiftEntriesMessageType,
				new[] { ModManifest.UniqueID },
				new[] { e.Peer.PlayerID }
			);
		}

		private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
		{
			if (e.FromModID != ModManifest.UniqueID)
				return;

			if (e.Type == GiftEntriesMessageType)
			{
				var message = e.ReadAs<Dictionary<long, IDictionary<string, IList<GiftEntry>>>>();
				GiftEntries = message;
			}
			else if (e.Type == typeof(NetMessage.RecordGift).FullName)
			{
				var message = e.ReadAs<NetMessage.RecordGift>();
				var player = Game1.getAllFarmers().First(p => p.UniqueMultiplayerID == message.PlayerID);
				RecordGiftEntryForNPC(player, message.NpcName, message.GiftEntry);
			}
			else
			{
				Monitor.Log($"Received unknown message of type {e.Type}.", LogLevel.Warn);
			}
		}

		private void PopulateConfig(ModConfig config)
		{
			foreach (var (name, _) in Characters.Value)
				if (!config.PerNPC.ContainsKey(name))
					config.PerNPC[name] = new(Config.Default);
		}

		private void SetupConfig()
		{
			var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (api is null)
				return;
			GMCMI18nHelper helper = new(api, ModManifest, Helper.Translation);

			api.Register(
				ModManifest,
				reset: () =>
				{
					Config = new();
					PopulateConfig(Config);
					LastDefaultConfigEntry = new(Config.Default);
				},
				save: () =>
				{
					if (Config.Default != LastDefaultConfigEntry)
					{
						foreach (var (_, entry) in Config.PerNPC)
							if (entry == LastDefaultConfigEntry)
								entry.CopyFrom(Config.Default);
					}

					ModConfig copy = new(Config);
					var toRemove = new List<string>();
					foreach (var (npcName, entry) in copy.PerNPC)
						if (entry == copy.Default || entry == LastDefaultConfigEntry)
							toRemove.Add(npcName);

					foreach (var npcName in toRemove)
						copy.PerNPC.Remove(npcName);
					Helper.WriteConfig(copy);
					LastDefaultConfigEntry = new(Config.Default);
				}
			);

			helper.AddSectionTitle("config.section.behavior");
			helper.AddBoolOption("config.enableNpcOverrides", () => Config.Default.EnableModOverrides);

			helper.AddSectionTitle("config.section.appearance");
			helper.AddBoolOption("config.showCurrentMailModifier", () => Config.ShowCurrentMailModifier);

			void SetupConfigEntryMenu(Func<ModConfig.Entry> entry)
			{
				helper.AddSectionTitle("config.section.npcPreferences");
				helper.AddEnumOption(
					keyPrefix: "config.inPersonPreference",
					valuePrefix: "config.giftPreference",
					getValue: () => entry().InPersonPreference,
					setValue: v => entry().InPersonPreference = v
				);
				helper.AddEnumOption(
					keyPrefix: "config.byMailPreference",
					valuePrefix: "config.giftPreference",
					getValue: () => entry().ByMailPreference,
					setValue: v => entry().ByMailPreference = v
				);
				helper.AddNumberOption("config.infrequentGiftPercent", () => entry().InfrequentGiftPercent, v => entry().InfrequentGiftPercent = v, min: 0f, max: 1f, interval: 0.01f);
				helper.AddNumberOption("config.frequentGiftPercent", () => entry().FrequentGiftPercent, v => entry().FrequentGiftPercent = v, min: 0f, max: 1f, interval: 0.01f);

				helper.AddSectionTitle("config.section.npc");
				helper.AddBoolOption("config.enableModOverrides", () => entry().EnableModOverrides, v => entry().EnableModOverrides = v);
				helper.AddNumberOption("config.giftsToRemember", () => entry().GiftsToRemember, v => entry().GiftsToRemember = v, min: 0);
				helper.AddNumberOption("config.daysToRemember", () => entry().DaysToRemember, v => entry().DaysToRemember = v, min: 0);
			}

			SetupConfigEntryMenu(() => Config.Default);

			helper.AddMultiPageLinkOption(
				keyPrefix: "config.npcOverrides",
				columns: _ => 3,
				pageID: character => $"character_{character.name}",
				pageName: character => character.displayName,
				pageValues: Characters.Value.ToArray()
			);

			foreach (var (name, displayName) in Characters.Value)
			{
				api.AddPage(ModManifest, $"character_{name}", () => displayName);
				SetupConfigEntryMenu(() => Config.PerNPC[name]);
			}
		}

		private void CleanUpGiftEntries()
		{
			WorldDate newDate = new(Game1.Date);
			foreach (var (playerID, allGiftEntries) in GiftEntries)
			{
				foreach (var (npcName, giftEntries) in allGiftEntries)
				{
					var configEntry = Config.GetForNPC(npcName);
					var toRemove = new HashSet<GiftEntry>();
					toRemove.UnionWith(giftEntries.Where(e => newDate.TotalDays - e.Date.TotalDays > configEntry.DaysToRemember));
					toRemove.UnionWith(giftEntries.Take(Math.Max(giftEntries.Count - configEntry.GiftsToRemember, 0)));
					foreach (var entry in toRemove)
						giftEntries.Remove(entry);
				}
			}

			if (GameExt.GetMultiplayerMode() == MultiplayerMode.Server)
			{
				Helper.Multiplayer.SendMessage(
				GiftEntries,
				GiftEntriesMessageType,
				new[] { ModManifest.UniqueID },
				Game1.getOnlineFarmers()
					.Where(p => p.UniqueMultiplayerID != GameExt.GetHostPlayer().UniqueMultiplayerID)
					.Select(p => p.UniqueMultiplayerID)
					.ToArray()
				);
			}
		}

		private IEnumerable<GiftEntry> GetGiftEntriesForNPC(Farmer player, string npcName)
		{
			if (GiftEntries.TryGetValue(player.UniqueMultiplayerID, out var allGiftEntries))
				if (allGiftEntries.TryGetValue(npcName, out var giftEntries))
					return giftEntries;
			return Enumerable.Empty<GiftEntry>();
		}

		private void RecordGiftEntryForNPC(Farmer player, string npcName, GiftEntry giftEntry)
		{
			Monitor.Log($"{GameExt.GetMultiplayerMode()} {player.Name} gifted {giftEntry.GiftTaste} {giftEntry.GiftMethod} to {npcName}", LogLevel.Trace);
			if (!GiftEntries.TryGetValue(player.UniqueMultiplayerID, out var allGiftEntries))
			{
				allGiftEntries = new Dictionary<string, IList<GiftEntry>>();
				GiftEntries[player.UniqueMultiplayerID] = allGiftEntries;
			}
			if (!allGiftEntries.TryGetValue(npcName, out var giftEntries))
			{
				giftEntries = new List<GiftEntry>();
				allGiftEntries[npcName] = giftEntries;
			}
			giftEntries.Add(giftEntry);

			if (GameExt.GetMultiplayerMode() != MultiplayerMode.SinglePlayer)
			{
				long[] playerIDsToSendTo;
				if (GameExt.GetMultiplayerMode() == MultiplayerMode.Server)
					playerIDsToSendTo = Game1.getOnlineFarmers()
						.Where(p => p != player && p.UniqueMultiplayerID != GameExt.GetHostPlayer().UniqueMultiplayerID)
						.Select(p => p.UniqueMultiplayerID)
						.ToArray();
				else
					playerIDsToSendTo = GameExt.GetHostPlayer() == player ? Array.Empty<long>() : new[] { GameExt.GetHostPlayer().UniqueMultiplayerID };

				if (playerIDsToSendTo.Length != 0)
				{
					Instance.Helper.Multiplayer.SendMessage(
						new NetMessage.RecordGift(player.UniqueMultiplayerID, npcName, giftEntry),
						new[] { Instance.ModManifest.UniqueID },
						playerIDsToSendTo
					);
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0018:Inline variable declaration", Justification = "Better semi-repeated code")]
		private GiftTaste GetGiftTasteModifier(Farmer player, string npcName, GiftMethod method)
		{
			var giftEntries = GetGiftEntriesForNPC(player, npcName);
			var viaMail = giftEntries.Count(e => e.GiftMethod == GiftMethod.ByMail);
			var configEntry = Config.GetForNPC(npcName);
			if (configEntry.EnableModOverrides && configEntry.HasSameValues(LastDefaultConfigEntry))
			{
				var asset = Game1.content.Load<Dictionary<string, string>>(OverrideAssetPath);
				if (asset.TryGetValue(npcName, out var line))
				{
					var split = line.Split('/');
					configEntry = new(configEntry);
					GiftPreference parsedGiftPreference;
					float parsedFloat;

					if (split.Length > 0 && Enum.TryParse(split[0].Trim(), true, out parsedGiftPreference))
						configEntry.InPersonPreference = parsedGiftPreference;
					if (split.Length > 1 && Enum.TryParse(split[1].Trim(), true, out parsedGiftPreference))
						configEntry.ByMailPreference = parsedGiftPreference;
					if (split.Length > 2 && float.TryParse(split[2].Trim(), out parsedFloat))
						configEntry.InfrequentGiftPercent = parsedFloat;
					if (split.Length > 3 && float.TryParse(split[3].Trim(), out parsedFloat))
						configEntry.FrequentGiftPercent = parsedFloat;
				}
			}

			float sameMethodPercent = 1f * giftEntries.Count(e => e.GiftMethod == method) / configEntry.GiftsToRemember;
			var preference = method switch
			{
				GiftMethod.InPerson => configEntry.InPersonPreference,
				GiftMethod.ByMail => configEntry.ByMailPreference,
				_ => throw new ArgumentException($"{nameof(GiftMethod)} has an invalid value."),
			};

			switch (preference)
			{
				case GiftPreference.Hates:
					return GiftTaste.Hate;
				case GiftPreference.HatesFrequent:
					if (sameMethodPercent >= configEntry.FrequentGiftPercent)
						return GiftTaste.Hate;
					else if (sameMethodPercent >= configEntry.InfrequentGiftPercent)
						return GiftTaste.Dislike;
					else
						return GiftTaste.Neutral;
				case GiftPreference.DislikesAndHatesFrequent:
					if (sameMethodPercent >= configEntry.FrequentGiftPercent)
						return GiftTaste.Hate;
					else
						return GiftTaste.Dislike;
				case GiftPreference.Dislikes:
					return GiftTaste.Dislike;
				case GiftPreference.DislikesFrequent:
					if (sameMethodPercent >= configEntry.FrequentGiftPercent)
						return GiftTaste.Dislike;
					else
						return GiftTaste.Neutral;
				case GiftPreference.Neutral:
					return GiftTaste.Neutral;
				case GiftPreference.LikesInfrequentButDislikesFrequent:
					if (sameMethodPercent >= configEntry.FrequentGiftPercent)
						return GiftTaste.Dislike;
					else if (sameMethodPercent >= configEntry.InfrequentGiftPercent)
						return GiftTaste.Neutral;
					else
						return GiftTaste.Like;
				case GiftPreference.LikesInfrequent:
					if (sameMethodPercent < configEntry.InfrequentGiftPercent)
						return GiftTaste.Like;
					else
						return GiftTaste.Neutral;
				case GiftPreference.Likes:
					return GiftTaste.Like;
				case GiftPreference.LovesInfrequent:
					if (sameMethodPercent < configEntry.InfrequentGiftPercent)
						return GiftTaste.Love;
					else if (sameMethodPercent < configEntry.FrequentGiftPercent)
						return GiftTaste.Like;
					else
						return GiftTaste.Neutral;
				case GiftPreference.LikesAndLovesInfrequent:
					if (sameMethodPercent < configEntry.InfrequentGiftPercent)
						return GiftTaste.Love;
					else
						return GiftTaste.Like;
				case GiftPreference.Loves:
					return GiftTaste.Love;
				default:
					throw new ArgumentException($"{nameof(GiftPreference)} has an invalid value.");
			}
		}

		private void ReturnItemIfNeeded(SObject item, string originalAddresseeNpcName, GiftTaste originalGiftTaste, GiftTaste modifiedGiftTaste)
		{
			if ((int)Instance.ModifiedGiftTaste > (int)GiftTaste.Dislike)
				return;

			var returnItem = Instance.Config.ReturnUnlikedItems switch
			{
				ModConfig.ReturningBehavior.Never => false,
				ModConfig.ReturningBehavior.NormallyLiked => (int)originalGiftTaste >= (int)GiftTaste.Neutral,
				ModConfig.ReturningBehavior.Always => true,
				_ => throw new ArgumentException($"{nameof(ModConfig.ReturningBehavior)} has an invalid value."),
			};
			if (!returnItem)
				return;

			// TODO: actually send a mail
		}

		private static void GiftShipmentController_GiftToNpc_Prefix()
		{
			Instance.CurrentGiftingPlayer = Game1.player;
			Instance.CurrentGiftMethod = GiftMethod.ByMail;
		}

		private static void GiftShipmentController_GiftToNpc_Postfix()
		{
			Instance.CurrentGiftingPlayer = null;
			Instance.CurrentGiftMethod = null;
		}

		private static IEnumerable<CodeInstruction> GiftShipmentController_CreateResponsePage_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();

			// IL to find:
			// IL_0117: ldloc.0
			// IL_0118: ldloc.3
			// IL_0119: callvirt instance string['Stardew Valley'] StardewValley.Character::get_Name()
			// IL_011e: ldloc.3
			// IL_011f: callvirt instance string['Stardew Valley'] StardewValley.Character::get_displayName()
			var worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdloc(),
				i => i.IsLdloc(),
				i => i.Calls(AccessTools.PropertyGetter(typeof(Character), nameof(Character.Name))),
				i => i.IsLdloc(),
				i => i.Calls(AccessTools.PropertyGetter(typeof(Character), nameof(Character.displayName)))
			});
			if (worker is null)
			{
				Instance.Monitor.Log($"Could not patch methods - Please Gift Me In Person probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
				return instructions;
			}

			worker.Postfix(new[]
			{
				new CodeInstruction(worker[1]), // ldloc.3
				new CodeInstruction(worker[2]), // callvirt instance string['Stardew Valley'] StardewValley.Character::get_Name()
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PleaseGiftMeInPerson), nameof(GiftShipmentController_CreateResponsePage_Transpiler_ModifyDisplayName)))
			});

			return instructions;
		}

		public static string GiftShipmentController_CreateResponsePage_Transpiler_ModifyDisplayName(string displayName, string npcName)
		{
			if (!Instance.Config.ShowCurrentMailModifier)
				return displayName;
			
			var mailGiftTasteModifier = Instance.GetGiftTasteModifier(Game1.player, npcName, GiftMethod.ByMail);
			var translationKey = mailGiftTasteModifier switch
			{
				GiftTaste.Hate => "mailGift.hateTier",
				GiftTaste.Dislike => "mailGift.dislikeTier",
				GiftTaste.Neutral => "mailGift.neutralTier",
				GiftTaste.Like => "mailGift.likeTier",
				GiftTaste.Love => "mailGift.loveTier",
				_ => throw new ArgumentException($"Invalid mail gift taste modifier {mailGiftTasteModifier}."),
			};
			return Instance.Helper.Translation.Get(translationKey, new { Name = displayName });
		}

		private static void NPC_tryToReceiveActiveObject_Prefix(NPC __instance, Farmer __0 /* who */)
		{
			Instance.CurrentGiftingPlayer = __0;
			Instance.CurrentGiftMethod = GiftMethod.InPerson;
		}

		private static void NPC_tryToReceiveActiveObject_Postfix(NPC __instance)
		{
			Instance.CurrentGiftingPlayer = null;
			Instance.CurrentGiftMethod = null;
		}

		private static void NPC_getGiftTasteForThisItem_Postfix(NPC __instance, ref int __result)
		{
			if (Instance.CurrentGiftingPlayer is null || Instance.CurrentGiftMethod is null)
				return;
			
			Instance.OriginalGiftTaste = GiftTasteExt.From(__result);
			__result = Instance.OriginalGiftTaste
				.GetModified((int)Instance.GetGiftTasteModifier(Instance.CurrentGiftingPlayer, __instance.Name, Instance.CurrentGiftMethod.Value))
				.GetStardewValue();
			Instance.ModifiedGiftTaste = GiftTasteExt.From(__result);
		}

		private static void NPC_receiveGift_Postfix(NPC __instance, SObject o, Farmer giver)
		{
			if (Instance.CurrentGiftMethod is null)
				return;
			
			Instance.RecordGiftEntryForNPC(
				giver,
				__instance.Name,
				new(
					new WorldDate(Game1.Date),
					Instance.OriginalGiftTaste,
					Instance.CurrentGiftMethod.Value
				)
			);

			if (Instance.CurrentGiftingPlayer == giver)
				Instance.ReturnItemIfNeeded(o, __instance.Name, Instance.OriginalGiftTaste, Instance.ModifiedGiftTaste);
		}
	}
}
