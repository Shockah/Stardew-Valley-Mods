using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class LoveAffix : BaseSeasonAffix
	{
		private static string ShortID => "Love";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description", new { Chance = $"{Mod.Config.LoveValue:0.##}x" });
		public override TextureRectangle Icon => new(Game1.mouseCursors, new(626, 1892, 9, 8));

		private readonly Dictionary<string, int> OldFriendship = new();

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		public override void OnActivate()
		{
			UpdateDispositions();
			Mod.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			Mod.Helper.Events.Content.AssetsInvalidated += OnAssetsInvalidated;
		}

		public override void OnDeactivate()
		{
			Mod.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
			Mod.Helper.Events.Content.AssetsInvalidated -= OnAssetsInvalidated;
		}

		public override void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.positive.{ShortID}.config.value", () => Mod.Config.LoveValue, min: 0.25f, max: 4f, interval: 0.05f, value => $"{value:0.##}x");
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			UpdateFriendship();
		}

		private void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
		{
			if (e.NamesWithoutLocale.Any(a => a.IsEquivalentTo("Data\\NPCDispositions")))
			{
				UpdateFriendship();
				UpdateDispositions();
			}
		}

		private void UpdateDispositions()
		{
			OldFriendship.Clear();
			Dictionary<string, string> dispositions = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");
			foreach (var (npcName, data) in dispositions)
			{
				string[] split = data.Split('/');
				if (!split[5].Equals("datable", StringComparison.InvariantCultureIgnoreCase))
					continue;
				OldFriendship[npcName] = Game1.player.getFriendshipLevelForNPC(npcName);
			}
		}

		private void UpdateFriendship()
		{
			foreach (var (npcName, oldFriendship) in OldFriendship)
			{
				int newFriendship = Game1.player.getFriendshipLevelForNPC(npcName);
				int extraFriendship = newFriendship - oldFriendship;
				OldFriendship[npcName] += extraFriendship;
				if (extraFriendship > 0)
				{
					int bonusFriendship = (int)Math.Round(-extraFriendship + extraFriendship * Mod.Config.LoveValue);
					Game1.player.friendshipData[npcName].Points += bonusFriendship;
					OldFriendship[npcName] += bonusFriendship;
				}
			}
		}
	}
}