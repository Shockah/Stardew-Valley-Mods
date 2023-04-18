using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.UI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Linq;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class HurricaneAffix : BaseSeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Hurricane";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(368, 224, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 0;

		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		public override void OnActivate()
		{
			Mod.Helper.Events.Content.AssetRequested += OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Data\\Locations");
		}

		public override void OnDeactivate()
		{
			Mod.Helper.Events.Content.AssetRequested -= OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Data\\Locations");
		}

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			if (!Mod.Helper.ModRegistry.IsLoaded("Esca.FarmTypeManager"))
				return;

			Type ftmModEntryType = AccessTools.TypeByName("FarmTypeManager.ModEntry, FarmTypeManager");
			Type ftmGenerationType = AccessTools.Inner(ftmModEntryType, "Generation");

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(ftmGenerationType, "ForageGeneration"),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(HurricaneAffix), nameof(FarmTypeManager_ModEntry_Generation_ForageGeneration_Prefix)))
			);
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (!e.Name.IsEquivalentTo("Data\\Locations"))
				return;
			e.Edit(asset =>
			{
				var data = asset.AsDictionary<string, string>();
				foreach (var kvp in data.Data)
				{
					string[] split = kvp.Value.Split('/');
					for (int i = 0; i < 4; i++)
						split[i] = "-1";
					data.Data[kvp.Key] = string.Join("/", split);
				}
			}, priority: AssetEditPriority.Late);
		}

		private static bool FarmTypeManager_ModEntry_Generation_ForageGeneration_Prefix()
		{
			return !Mod.ActiveAffixes.Any(a => a is HurricaneAffix);
		}
	}
}