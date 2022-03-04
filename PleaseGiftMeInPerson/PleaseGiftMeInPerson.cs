using HarmonyLib;
using Shockah.CommonModCode;
using Shockah.CommonModCode.GMCM;
using Shockah.CommonModCode.GMCM.Helper;
using Shockah.CommonModCode.SMAPI;
using Shockah.CommonModCode.Stardew;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.PleaseGiftMeInPerson
{
	public class PleaseGiftMeInPerson: Mod
	{
		private static readonly string MailServicesMod_GiftShipmentController_QualifiedName = "MailServicesMod.GiftShipmentController, MailServicesMod";
		private static readonly string GiftEntriesKey = "GiftEntries";
		private static readonly string GiftEntriesMessageType = "GiftEntries";

		internal static PleaseGiftMeInPerson Instance { get; set; } = null!;
		internal ModConfig Config { get; private set; } = null!;
		private ModConfig.Entry LastDefaultConfigEntry = null!;

		private Farmer? CurrentPlayerGiftingViaMail;
		private int OriginalGiftTaste;
		private int ModifiedGiftTaste;
		private int TicksUntilConfigSetup = 5;

		private Lazy<IReadOnlyList<(string name, string displayName)>> Characters = null!;

		private IDictionary<long, IDictionary<string, IList<GiftEntry>>> GiftEntries = new Dictionary<long, IDictionary<string, IList<GiftEntry>>>();

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<ModConfig>();
			LastDefaultConfigEntry = new(Config.Default);

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
			if (GameExt.GetMultiplayerMode() == MultiplayerMode.Client)
				return;
			GiftEntries = Helper.Data.ReadSaveData<IDictionary<long, IDictionary<string, IList<GiftEntry>>>>(GiftEntriesKey)
				?? new Dictionary<long, IDictionary<string, IList<GiftEntry>>>();
		}

		private void OnSaving(object? sender, SavingEventArgs e)
		{
			if (GameExt.GetMultiplayerMode() == MultiplayerMode.Client)
				return;

			CleanUpGiftEntries();
			Helper.Data.WriteSaveData(GiftEntriesKey, GiftEntries);
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

			void SetupConfigEntryMenu(Func<ModConfig.Entry> entry)
			{
				helper.AddNumberOption("config.giftsToRemember", () => entry().GiftsToRemember, v => entry().GiftsToRemember = v, min: 0);
				helper.AddNumberOption("config.daysToRemember", () => entry().DaysToRemember, v => entry().DaysToRemember = v, min: 0);
				helper.AddNumberOption("config.mailsUntilDislike", () => entry().MailsUntilDislike, v => entry().MailsUntilDislike = v, min: -1);
				helper.AddNumberOption("config.mailsUntilHate", () => entry().MailsUntilHate, v => entry().MailsUntilHate = v, min: -1);
				helper.AddNumberOption("config.mailsUntilLike", () => entry().MailsUntilLike, v => entry().MailsUntilLike = v, min: -1);
				helper.AddNumberOption("config.mailsUntilLove", () => entry().MailsUntilLove, v => entry().MailsUntilLove = v, min: -1);
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
				Game1.getAllFarmers()
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
			Monitor.Log($"{GameExt.GetMultiplayerMode()} {player.Name} gifted {giftEntry.GiftTaste} {giftEntry.GiftMethod} to {npcName}", LogLevel.Debug);
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
					playerIDsToSendTo = Game1.getAllFarmers()
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

		private int GetMailGiftTasteModifier(Farmer player, string npcName)
		{
			var giftEntries = GetGiftEntriesForNPC(player, npcName);
			var viaMail = giftEntries.Count(e => e.GiftMethod == GiftMethod.ByMail);
			var configEntry = Config.GetForNPC(npcName);
			(GiftTaste taste, int mails)[] sorted = new[]
			{
				(taste: GiftTaste.Hate, mails: configEntry.MailsUntilHate),
				(taste: GiftTaste.Dislike, mails: configEntry.MailsUntilDislike),
				(taste: GiftTaste.Like, mails: configEntry.MailsUntilLike),
				(taste: GiftTaste.Love, mails: configEntry.MailsUntilLove)
			}.Where(e => e.mails >= 0).OrderBy(e => e.mails).ToArray();
			var taste = sorted.LastOrNull(e => e.mails <= viaMail)?.taste ?? GiftTaste.Neutral;
			return (int)taste;
		}

		private static void GiftShipmentController_GiftToNpc_Prefix()
		{
			Instance.CurrentPlayerGiftingViaMail = Game1.player;
		}

		private static void GiftShipmentController_GiftToNpc_Postfix()
		{
			Instance.CurrentPlayerGiftingViaMail = null;
		}

		private static void NPC_getGiftTasteForThisItem_Postfix(NPC __instance, ref int __result)
		{
			Instance.OriginalGiftTaste = __result;
			Farmer? player = Instance.CurrentPlayerGiftingViaMail;
			if (player is not null)
				__result = GiftTasteExt.From(__result)
					.GetModified(Instance.GetMailGiftTasteModifier(player, __instance.Name))
					.GetStardewValue();
			Instance.ModifiedGiftTaste = __result;
		}

		private static void NPC_receiveGift_Postfix(NPC __instance, Farmer giver)
		{
			Instance.RecordGiftEntryForNPC(
				giver,
				__instance.Name,
				new(
					new WorldDate(Game1.Date),
					GiftTasteExt.From(Instance.OriginalGiftTaste),
					Instance.CurrentPlayerGiftingViaMail == giver ? GiftMethod.ByMail : GiftMethod.InPerson
				)
			);
		}
	}
}
