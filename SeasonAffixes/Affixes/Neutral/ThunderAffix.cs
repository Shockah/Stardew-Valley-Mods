using Newtonsoft.Json;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.LocationContexts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes;

partial class ModConfig
{
	[JsonProperty] public float ThunderChance { get; internal set; } = 2f;
}

internal sealed class ThunderAffix : BaseSeasonAffix, ISeasonAffix
{
	private static string ShortID => "Thunder";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description", new { Chance = $"{Mod.Config.ThunderChance:0.##}x" });
	public TextureRectangle Icon => new(Game1.mouseCursors, new(413, 346, 13, 13));

	public ThunderAffix() : base(ShortID, "neutral") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> Mod.Helper.ModRegistry.IsLoaded("Shockah.SafeLightning") ? 0 : 1;

	public double GetProbabilityWeight(OrdinalSeason season)
	{
		if (Mod.Config.ChoicePeriod == AffixSetChoicePeriod.Day)
			return 0;
		return season.Season switch
		{
			Season.Spring or Season.Fall => 0.5,
			Season.Summer => 1,
			Season.Winter => 0,
			_ => throw new ArgumentException($"{nameof(Season)} has an invalid value."),
		};
	}

	public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.CropsAspect, VanillaSkill.FlowersAspect, VanillaSkill.FishingAspect };

	public void OnActivate(AffixActivationContext context)
	{
		Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;
		Mod.Helper.Events.Content.AssetRequested += OnAssetRequested;
		Mod.Helper.GameContent.InvalidateCache("Data\\LocationContexts");
	}

	public void OnDeactivate(AffixActivationContext context)
	{
		Mod.Helper.Events.GameLoop.DayStarted -= OnDayStarted;
		Mod.Helper.Events.Content.AssetRequested -= OnAssetRequested;
		Mod.Helper.GameContent.InvalidateCache("Data\\LocationContexts");
	}

	public void SetupConfig(IManifest manifest)
	{
		var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
		GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
		helper.AddNumberOption($"{I18nPrefix}.config.chance", () => Mod.Config.ThunderChance, min: 0.25f, max: 4f, interval: 0.05f, value => $"{value:0.##}x");
	}

	private void OnDayStarted(object? sender, DayStartedEventArgs e)
		=> Mod.Helper.GameContent.InvalidateCache("Data\\LocationContexts");

	private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
	{
		if (!e.Name.IsEquivalentTo("Data\\LocationContexts"))
			return;
		e.Edit(asset =>
		{
			var data = asset.AsDictionary<string, LocationContextData>();
			foreach (var kvp in data.Data)
			{
				foreach (var entry in kvp.Value.WeatherConditions)
				{
					if (string.IsNullOrEmpty(entry.Condition))
						continue;
					var isRainy = entry.Weather.Equals("Rain", StringComparison.InvariantCultureIgnoreCase) || entry.Weather.Equals("Storm", StringComparison.InvariantCultureIgnoreCase);
					var queries = GameStateQuery.Parse(entry.Condition);
					var newQueries = ModifyRandomChance(queries, (originalChance, isNegated) => 1.0 - Math.Pow(1.0 - originalChance, (isRainy ^ isNegated) ? Mod.Config.ThunderChance : 1.0 / Mod.Config.ThunderChance));
					entry.Condition = Serialize(newQueries);
				}
			}
		}, priority: AssetEditPriority.Late);
	}

	private GameStateQuery.ParsedGameStateQuery[] ModifyRandomChance(GameStateQuery.ParsedGameStateQuery[] query, RandomChanceMutator mutator, bool externallyMutated = false)
		=> query.Select(q => ModifyRandomChance(q, mutator, externallyMutated)).ToArray();

	private GameStateQuery.ParsedGameStateQuery ModifyRandomChance(GameStateQuery.ParsedGameStateQuery query, RandomChanceMutator mutator, bool externallyMutated = false)
	{
		if (!string.IsNullOrEmpty(query.Error))
			return query;
		if (query.Query.Length == 0)
			return query;

		GameStateQuery.ParsedGameStateQuery? RebuildQuery(string[] newValues)
			=> GameStateQuery.Parse(Serialize(query, query: newValues)).FirstOrNull();

		switch (query.Query[0].ToUpper())
		{
			case "RANDOM":
				{
					if (query.Query.Length < 2)
						return query;
					if (!double.TryParse(query.Query[1], out var chance))
						return query;

					string[] newArgs = query.Query.ToArray();
					newArgs[1] = $"{mutator(chance, query.Negated ^ externallyMutated)}";
					return RebuildQuery(newArgs) ?? query;
				}
			case "SYNCED_RANDOM":
				{
					if (query.Query.Length < 4)
						return query;
					if (!double.TryParse(query.Query[3], out var chance))
						return query;

					string[] newArgs = query.Query.ToArray();
					newArgs[3] = $"{mutator(chance, query.Negated ^ externallyMutated)}";
					return RebuildQuery(newArgs) ?? query;
				}
			case "SYNCED_SUMMER_RAIN_RANDOM":
				{
					if (query.Query.Length < 3)
						return query;
					if (!double.TryParse(query.Query[1], out var baseChance))
						return query;
					if (!double.TryParse(query.Query[2], out var dayMultiplier))
						return query;

					var daysInSeason = Game1.Date.Season.GetDays(Context.IsWorldReady ? Game1.Date.Year : 1);
					var minChance = baseChance + dayMultiplier;
					var maxChance = baseChance + dayMultiplier * daysInSeason;

					minChance = mutator(minChance, query.Negated ^ externallyMutated);
					maxChance = mutator(maxChance, query.Negated ^ externallyMutated);

					dayMultiplier = (maxChance - minChance) / (daysInSeason - 1);
					baseChance = maxChance - dayMultiplier * daysInSeason;

					string[] newArgs = query.Query.ToArray();
					newArgs[1] = $"{baseChance}";
					newArgs[2] = $"{dayMultiplier}";
					return RebuildQuery(newArgs) ?? query;
				}
			case "ANY":
				{
					string[] newArgs = query.Query.ToArray();
					for (int i = 1; i < newArgs.Length; i++)
					{
						var innerQueries = GameStateQuery.Parse(newArgs[i]);
						var newInnerQueries = ModifyRandomChance(innerQueries, mutator, externallyMutated: query.Negated ^ externallyMutated);
						newArgs[i] = Serialize(newInnerQueries);
					}
					return RebuildQuery(newArgs) ?? query;
				}
			default:
				return query;
		}
	}

	private static string Serialize(GameStateQuery.ParsedGameStateQuery parsed, string[]? query = null, bool? negated = null)
		=> (negated ?? parsed.Negated ? "!" : "") + string.Join(
			" ",
			(query ?? parsed.Query)
				.Select(p => p.Replace("\\", "\\\\").Replace("\"", "\\\""))
				.Select(p => p.Contains(' ') ? "\"\"" : p)
		);

	private static string Serialize(GameStateQuery.ParsedGameStateQuery[] parsed)
		=> string.Join(", ", parsed.Select(q => Serialize(q)));

	private delegate double RandomChanceMutator(double originalChance, bool isNegated);
}