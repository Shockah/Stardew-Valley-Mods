using GenericModConfigMenu;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
    public class FlexibleSprinklers: Mod
    {
        public static FlexibleSprinklers Instance { get; private set; } = null!;

        internal ModConfig Config { get; private set; }

        public bool SkipVanillaBehavior { get; private set; } = false;
        public ISprinklerBehavior SprinklerBehavior { get; private set; }

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            var harmony = new Harmony(ModManifest.UniqueID);
            ObjectPatches.Apply(harmony);

            SetupSprinklerBehavior();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            SetupConfig();
        }

        private void SetupConfig()
        {
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

            string ConfigNameForSprinklerBehavior(ModConfig.SprinklerBehavior behavior)
            {
                return behavior switch
                {
                    ModConfig.SprinklerBehavior.Flexible => "Flexible (vanilla > flood fill)",
                    ModConfig.SprinklerBehavior.FlexibleWithoutVanilla => "Flood fill",
                    ModConfig.SprinklerBehavior.Vanilla => "Vanilla",
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

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Sprinkler behavior",
                tooltip: () => "> Flexible: Will water using vanilla behavior, and then flood fill for any tiles that failed.\n> Flood fill: Custom-made algorithm. Tries to flood fill from the sprinkler/watered tiles.\n   Will also change behavior if next to other sprinklers.\n> Vanilla: Does not change the sprinkler behavior.",
                getValue: () => ConfigNameForSprinklerBehavior(Config.sprinklerBehavior),
                setValue: value => Config.sprinklerBehavior = ((ModConfig.SprinklerBehavior[])System.Enum.GetValues(typeof(ModConfig.SprinklerBehavior))).First(e => ConfigNameForSprinklerBehavior(e) == value),
                allowedValues: ((ModConfig.SprinklerBehavior[])System.Enum.GetValues(typeof(ModConfig.SprinklerBehavior))).Select(e => ConfigNameForSprinklerBehavior(e)).ToArray()
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Flood fill balance mode",
                tooltip: () => "Edge case handling for the flood fill behavior.\n\n> Relaxed: May water more tiles\n> Exact: Will water exactly as many tiles as it should, but those may be semi-random\n> Restrictive: May water less tiles",
                getValue: () => ConfigNameForTileWaterBalanceMode(Config.tileWaterBalanceMode),
                setValue: value => Config.tileWaterBalanceMode = ((FlexibleSprinklerBehavior.TileWaterBalanceMode[])System.Enum.GetValues(typeof(FlexibleSprinklerBehavior.TileWaterBalanceMode))).First(e => ConfigNameForTileWaterBalanceMode(e) == value),
                allowedValues: ((FlexibleSprinklerBehavior.TileWaterBalanceMode[])System.Enum.GetValues(typeof(FlexibleSprinklerBehavior.TileWaterBalanceMode))).Select(e => ConfigNameForTileWaterBalanceMode(e)).ToArray()
            );
        }

        private void SetupSprinklerBehavior()
        {
            SprinklerBehavior = Config.sprinklerBehavior switch
            {
                ModConfig.SprinklerBehavior.Flexible => new FlexibleSprinklerBehavior(Config.tileWaterBalanceMode, new VanillaSprinklerBehavior()),
                ModConfig.SprinklerBehavior.FlexibleWithoutVanilla => new FlexibleSprinklerBehavior(Config.tileWaterBalanceMode, null),
                ModConfig.SprinklerBehavior.Vanilla => new VanillaSprinklerBehavior(),
                _ => throw new System.ArgumentException(),
            };
        }

        public SprinklerInfo GetSprinklerInfo(Object sprinkler)
        {
            var wasVanillaQueryInProgress = ObjectPatches.IsVanillaQueryInProgress;
            ObjectPatches.IsVanillaQueryInProgress = true;
            var layout = sprinkler.GetSprinklerTiles().Select(t => new IntPoint((int)t.X, (int)t.Y)).ToHashSet();
            ObjectPatches.IsVanillaQueryInProgress = wasVanillaQueryInProgress;
            return new SprinklerInfo { Layout = layout };
        }
    }
}