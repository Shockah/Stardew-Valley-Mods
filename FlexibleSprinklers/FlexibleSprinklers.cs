using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	public class FlexibleSprinklers: Mod, IFlexibleSprinklersApi
	{
		private const int PressureNozzleParentSheetIndex = 915;
		internal static readonly string LineSprinklersModID = "hootless.LineSprinklers";
		internal static readonly string BetterSprinklersModID = "Speeder.BetterSprinklers";
		internal static readonly string PrismaticToolsModID = "stokastic.PrismaticTools";

		public static FlexibleSprinklers Instance { get; private set; }

		internal ModConfig Config { get; private set; }
		internal ISprinklerBehavior SprinklerBehavior { get; private set; }
		private readonly List<System.Func<Object, int?>> sprinklerTierProviders = new();
		private readonly List<System.Func<Object, Vector2[]>> sprinklerCoverageProviders = new();

		internal ILineSprinklersApi LineSprinklersApi { get; private set; }
		internal IBetterSprinklersApi BetterSprinklersApi { get; private set; }
		internal IPrismaticToolsApi PrismaticToolsApi { get; private set; }

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			Config = helper.ReadConfig<ModConfig>();

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.World.ObjectListChanged += OnObjectListChanged;
			helper.Events.Input.ButtonPressed += OnButtonPressed;

			SetupSprinklerBehavior();
		}

		public override object GetApi()
		{
			return this;
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			var harmony = new Harmony(ModManifest.UniqueID);
			ObjectPatches.Apply(harmony);

			LineSprinklersApi = Helper.ModRegistry.GetApi<ILineSprinklersApi>(LineSprinklersModID);
			if (LineSprinklersApi != null)
				LineSprinklersPatches.Apply(harmony);

			BetterSprinklersApi = Helper.ModRegistry.GetApi<IBetterSprinklersApi>(BetterSprinklersModID);
			if (BetterSprinklersApi != null)
				BetterSplinklersPatches.Apply(harmony);

			PrismaticToolsApi = Helper.ModRegistry.GetApi<IPrismaticToolsApi>(BetterSprinklersModID);
			if (PrismaticToolsApi != null)
				PrismaticToolsPatches.Apply(harmony);

			SetupConfig();
		}

		private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
		{
			if (!Config.ActivateOnPlacement)
				return;
			foreach (var (_, sprinkler) in e.Added)
			{
				ActivateSprinkler(sprinkler, e.Location);
			}
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (!Config.ActivateOnAction)
				return;
			if (!Context.IsPlayerFree)
				return;
			if (!e.Button.IsActionButton())
				return;

			var tile = e.Cursor.GrabTile;
			var location = Game1.currentLocation;
			var @object = location.getObjectAtTile((int)tile.X, (int)tile.Y);
			if (@object == null || !@object.IsSprinkler())
				return;

			var heldItem = Game1.player.CurrentItem;
			if (heldItem?.ParentSheetIndex == PressureNozzleParentSheetIndex && @object.heldObject?.Value?.ParentSheetIndex != PressureNozzleParentSheetIndex)
				return;

			ActivateSprinkler(@object, location);
		}

		private void SetupConfig()
		{
			// TODO: add translation support
			var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

			configMenu.Register(
				ModManifest,
				reset: () => Config = new ModConfig(),
				save: () =>
				{
					Helper.WriteConfig(Config);
					SetupSprinklerBehavior();
				}
			);

			string ConfigNameForSprinklerBehavior(ModConfig.SprinklerBehaviorEnum behavior)
			{
				return behavior switch
				{
					ModConfig.SprinklerBehaviorEnum.Flexible => "Flexible (vanilla > flood fill)",
					ModConfig.SprinklerBehaviorEnum.FlexibleWithoutVanilla => "Flood fill",
					ModConfig.SprinklerBehaviorEnum.Vanilla => "Vanilla",
					_ => throw new System.ArgumentException(),
				};
			}

			string ConfigNameForTileWaterBalanceMode(FlexibleSprinklerBehavior.TileWaterBalanceMode tileWaterBalanceMode)
			{
				return tileWaterBalanceMode switch
				{
					FlexibleSprinklerBehavior.TileWaterBalanceMode.Relaxed => "Relaxed",
					FlexibleSprinklerBehavior.TileWaterBalanceMode.Exact => "Exact",
					FlexibleSprinklerBehavior.TileWaterBalanceMode.Restrictive => "Restrictive",
					_ => throw new System.ArgumentException(),
				};
			}

			configMenu.AddSectionTitle(
				mod: ModManifest,
				text: () => "Watering options"
			);

			configMenu.AddTextOption(
				mod: ModManifest,
				name: () => "Sprinkler behavior",
				tooltip: () => "> Flexible: Will water using vanilla behavior, and then flood fill for any tiles that failed.\n> Flood fill: Custom-made algorithm. Tries to flood fill from the sprinkler/watered tiles.\n   Will also change behavior if next to other sprinklers.\n> Vanilla: Does not change the sprinkler behavior.",
				getValue: () => ConfigNameForSprinklerBehavior(Config.SprinklerBehavior),
				setValue: value => Config.SprinklerBehavior = ((ModConfig.SprinklerBehaviorEnum[])System.Enum.GetValues(typeof(ModConfig.SprinklerBehaviorEnum))).First(e => ConfigNameForSprinklerBehavior(e) == value),
				allowedValues: ((ModConfig.SprinklerBehaviorEnum[])System.Enum.GetValues(typeof(ModConfig.SprinklerBehaviorEnum))).Select(e => ConfigNameForSprinklerBehavior(e)).ToArray()
			);

			configMenu.AddTextOption(
				mod: ModManifest,
				name: () => "Flood fill balance mode",
				tooltip: () => "Edge case handling for the flood fill behavior.\n\n> Relaxed: May water more tiles\n> Exact: Will water exactly as many tiles as it should, but those may be semi-random\n> Restrictive: May water less tiles",
				getValue: () => ConfigNameForTileWaterBalanceMode(Config.TileWaterBalanceMode),
				setValue: value => Config.TileWaterBalanceMode = ((FlexibleSprinklerBehavior.TileWaterBalanceMode[])System.Enum.GetValues(typeof(FlexibleSprinklerBehavior.TileWaterBalanceMode))).First(e => ConfigNameForTileWaterBalanceMode(e) == value),
				allowedValues: ((FlexibleSprinklerBehavior.TileWaterBalanceMode[])System.Enum.GetValues(typeof(FlexibleSprinklerBehavior.TileWaterBalanceMode))).Select(e => ConfigNameForTileWaterBalanceMode(e)).ToArray()
			);

			configMenu.AddSectionTitle(
				mod: ModManifest,
				text: () => "Activation options"
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Activate on placement",
				getValue: () => Config.ActivateOnPlacement,
				setValue: value => Config.ActivateOnPlacement = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Activate on action",
				getValue: () => Config.ActivateOnAction,
				setValue: value => Config.ActivateOnAction = value
			);

			configMenu.AddSectionTitle(
				mod: ModManifest,
				text: () => "Sprinkler power",
				tooltip: () => "The values below will only be used when watering via the flood fill method."
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Tier 1 (Basic)",
				tooltip: () => "How many tiles should tier 1 (Basic) sprinklers cover.",
				getValue: () => Config.Tier1Power,
				setValue: value => Config.Tier1Power = value,
				min: 0, interval: 1
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Tier 2 (Quality)",
				tooltip: () => "How many tiles should tier 2 (Quality / Basic + Pressure Nozzle) sprinklers cover.",
				getValue: () => Config.Tier2Power,
				setValue: value => Config.Tier2Power = value,
				min: 0, interval: 1
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Tier 3 (Iridium)",
				tooltip: () => "How many tiles should tier 3 (Iridium / Quality + Pressure Nozzle) sprinklers cover.",
				getValue: () => Config.Tier3Power,
				setValue: value => Config.Tier3Power = value,
				min: 0, interval: 1
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Tier 4 (Iridium + Nozzle)",
				tooltip: () => "How many tiles should tier 4 (Iridium + Pressure Nozzle) sprinklers cover.",
				getValue: () => Config.Tier4Power,
				setValue: value => Config.Tier4Power = value,
				min: 0, interval: 1
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Tier 5",
				tooltip: () => "How many tiles should tier 5 sprinklers (if you have mods with such a tier) cover.",
				getValue: () => Config.Tier5Power,
				setValue: value => Config.Tier5Power = value,
				min: 0, interval: 1
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Tier 6",
				tooltip: () => "How many tiles should tier 6 sprinklers (if you have mods with such a tier) cover.",
				getValue: () => Config.Tier6Power,
				setValue: value => Config.Tier6Power = value,
				min: 0, interval: 1
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Tier 7",
				tooltip: () => "How many tiles should tier 6 sprinklers (if you have mods with such a tier) cover.",
				getValue: () => Config.Tier7Power,
				setValue: value => Config.Tier7Power = value,
				min: 0, interval: 1
			);

			configMenu.AddNumberOption(
				mod: ModManifest,
				name: () => "Tier 8",
				tooltip: () => "How many tiles should tier 6 sprinklers (if you have mods with such a tier) cover.",
				getValue: () => Config.Tier8Power,
				setValue: value => Config.Tier8Power = value,
				min: 0, interval: 1
			);
		}

		private void SetupSprinklerBehavior()
		{
			SprinklerBehavior = Config.SprinklerBehavior switch
			{
				ModConfig.SprinklerBehaviorEnum.Flexible => new FlexibleSprinklerBehavior(Config.TileWaterBalanceMode, new VanillaSprinklerBehavior()),
				ModConfig.SprinklerBehaviorEnum.FlexibleWithoutVanilla => new FlexibleSprinklerBehavior(Config.TileWaterBalanceMode, null),
				ModConfig.SprinklerBehaviorEnum.Vanilla => new VanillaSprinklerBehavior(),
				_ => throw new System.ArgumentException(),
			};
		}

		public void ActivateSprinkler(Object sprinkler, GameLocation location)
		{
			if (Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER"))
				return;
			if (sprinkler == null || !sprinkler.IsSprinkler())
				return;

			foreach (var sprinklerTile in GetModifiedSprinklerCoverage(sprinkler, location))
			{
				sprinkler.ApplySprinkler(location, new Vector2(sprinklerTile.X, sprinklerTile.Y));
			}
			sprinkler.ApplySprinklerAnimation(location);
		}

		private int GetSprinklerPower(Object sprinkler, Vector2[] layout)
		{
			int? GetTier()
			{
				foreach (var sprinklerTierProvider in sprinklerTierProviders)
				{
					var tier = sprinklerTierProvider(sprinkler);
					if (tier != null)
						return tier;
				}

				if (LineSprinklersApi != null)
				{
					if (LineSprinklersApi.GetSprinklerCoverage().TryGetValue(sprinkler.ParentSheetIndex, out Vector2[] tilePositions))
					{
						switch (tilePositions.Length)
						{
							case 4:
								return 1;
							case 8:
								return 2;
							case 24:
								return 3;
						}
					}
				}

				if (PrismaticToolsApi != null && sprinkler.ParentSheetIndex == PrismaticToolsApi.SprinklerIndex && sprinkler.ParentSheetIndex == PrismaticToolsApi.SprinklerIndex)
				{
					return PrismaticToolsApi.SprinklerRange + 1;
				}

				var radius = sprinkler.GetModifiedRadiusForSprinkler();
				return radius == -1 ? null : radius + 1;
			}

			var tier = GetTier();

			if (tier == null)
			{
				return layout.Length;
			}
			else
			{
				var powers = new[] { Config.Tier1Power, Config.Tier2Power, Config.Tier3Power, Config.Tier4Power, Config.Tier5Power, Config.Tier6Power, Config.Tier7Power, Config.Tier8Power };
				return tier.Value < powers.Length ? powers[tier.Value - 1] : layout.Length;
			}
		}

		internal SprinklerInfo GetSprinklerInfo(Object sprinkler)
		{
			var layout = GetUnmodifiedSprinklerCoverage(sprinkler);
			var power = GetSprinklerPower(sprinkler, layout);
			return new SprinklerInfo(layout, power);
		}

		public Vector2[] GetModifiedSprinklerCoverage(Object sprinkler, GameLocation location)
		{
			var wasVanillaQueryInProgress = ObjectPatches.IsVanillaQueryInProgress;
			ObjectPatches.IsVanillaQueryInProgress = false;
			ObjectPatches.CurrentLocation = location;
			var layout = sprinkler.GetSprinklerTiles().ToArray();
			ObjectPatches.IsVanillaQueryInProgress = wasVanillaQueryInProgress;
			return layout;
		}

		public Vector2[] GetUnmodifiedSprinklerCoverage(Object sprinkler)
		{
			foreach (var sprinklerCoverageProvider in sprinklerCoverageProviders)
			{
				var coverage = sprinklerCoverageProvider(sprinkler);
				if (coverage != null)
					return coverage;
			}
			
			if (LineSprinklersApi != null)
			{
				if (LineSprinklersApi.GetSprinklerCoverage().TryGetValue(sprinkler.ParentSheetIndex, out Vector2[] tilePositions))
					return tilePositions.Where(t => t != Vector2.Zero).ToArray();
			}

			if (BetterSprinklersApi != null)
			{
				if (BetterSprinklersApi.GetSprinklerCoverage().TryGetValue(sprinkler.ParentSheetIndex, out Vector2[] tilePositions))
					return tilePositions.Where(t => t != Vector2.Zero).ToArray();
			}

			if (PrismaticToolsApi != null && sprinkler.ParentSheetIndex == PrismaticToolsApi.SprinklerIndex)
			{
				return PrismaticToolsApi.GetSprinklerCoverage(Vector2.Zero).Where(t => t != Vector2.Zero).ToArray();
			}

			var wasVanillaQueryInProgress = ObjectPatches.IsVanillaQueryInProgress;
			ObjectPatches.IsVanillaQueryInProgress = true;
			var layout = sprinkler.GetSprinklerTiles().Where(t => t != Vector2.Zero).ToArray();
			ObjectPatches.IsVanillaQueryInProgress = wasVanillaQueryInProgress;
			return layout;
		}

		public void RegisterSprinklerTierProvider(System.Func<Object, int?> provider)
		{
			sprinklerTierProviders.Add(provider);
		}

		public void RegisterSprinklerCoverageProvider(System.Func<Object, Vector2[]> provider)
		{
			sprinklerCoverageProviders.Add(provider);
		}
	}
}