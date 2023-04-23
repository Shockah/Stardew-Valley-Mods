using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class ResilienceAffix : BaseSeasonAffix
	{
		private static string ShortID => "Resilience";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description", new { Value = $"{Mod.Config.ResilienceValue:0.##}x" });
		public override TextureRectangle Icon => new(Game1.content.Load<Texture2D>("Characters\\Monsters\\Metal Head"), new(0, 0, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> Mod.Config.ResilienceValue < 1 ? 1 : 0;

		public override int GetNegativity(OrdinalSeason season)
			=> Mod.Config.ResilienceValue > 1 ? 1 : 0;

		public override double GetProbabilityWeight(OrdinalSeason season)
			=> season.Season == Season.Winter ? 0 : 1;

		public override void OnActivate()
		{
			Mod.Helper.Events.Content.AssetRequested += OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Data\\Monsters");
		}

		public override void OnDeactivate()
		{
			Mod.Helper.Events.Content.AssetRequested -= OnAssetRequested;
			Mod.Helper.GameContent.InvalidateCache("Data\\Monsters");
		}

		public override void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.negative.{ShortID}.config.value", () => Mod.Config.ResilienceValue, min: 0.25f, max: 4f, interval: 0.05f, value => $"{value:0.##}x");
		}

		private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
		{
			if (!e.Name.IsEquivalentTo("Data\\Monsters"))
				return;
			e.Edit(asset =>
			{
				var data = asset.AsDictionary<int, string>();
				foreach (var kvp in data.Data)
				{
					string[] split = kvp.Value.Split('/');
					split[0] = $"{(int)Math.Round(int.Parse(split[0]) * Mod.Config.ResilienceValue)}";
					data.Data[kvp.Key] = string.Join("/", split);
				}
			}, priority: AssetEditPriority.Late);
		}
	}
}