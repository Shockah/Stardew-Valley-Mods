﻿using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Shockah.SeasonAffixes.Affixes.Positive;

internal sealed class FortuneAffix : BaseSeasonAffix, ISeasonAffix
{
	private static string ShortID => "Fortune";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description");
	public TextureRectangle Icon => new(Game1.mouseCursors, new(381, 361, 10, 10));

	public FortuneAffix() : base(ShortID, "positive") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> 0;

	public void OnActivate()
	{
		Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;

		if (!Context.IsMainPlayer)
			return;
		Game1.player.team.sharedDailyLuck.Value += Mod.Config.FortuneValue;
	}

	public void OnDeactivate()
	{
		if (!Context.IsMainPlayer)
			return;
		Game1.player.team.sharedDailyLuck.Value -= Mod.Config.FortuneValue;
	}

	public void SetupConfig(IManifest manifest)
	{
		var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
		GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
		helper.AddNumberOption($"{I18nPrefix}.config.value", () => Mod.Config.FortuneValue, min: 0.001f, max: 0.5f, interval: 0.001f, value => $"{(int)(value * 100):0.##}%");
	}

	private void OnDayStarted(object? sender, DayStartedEventArgs e)
	{
		if (!Context.IsMainPlayer)
			return;
		Game1.player.team.sharedDailyLuck.Value += Mod.Config.FortuneValue;
	}
}