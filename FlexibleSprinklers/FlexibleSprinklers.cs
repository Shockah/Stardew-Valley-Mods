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
        private const int PressureNozzleParentSheetIndex = 915;
        
        public static FlexibleSprinklers Instance { get; private set; }

        internal ModConfig Config { get; private set; }
        public ISprinklerBehavior SprinklerBehavior { get; private set; }

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.World.ObjectListChanged += OnObjectListChanged;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            var harmony = new Harmony(ModManifest.UniqueID);
            ObjectPatches.Apply(harmony);

            SetupSprinklerBehavior();
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            SetupConfig();
        }

        private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            if (!Config.activateOnPlacement)
                return;
            foreach (var (_, sprinkler) in e.Added)
            {
                ActivateSprinkler(sprinkler, e.Location);
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Config.activateOnAction)
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

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Watering options"
            );

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

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Activation options"
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Activate on placement",
                getValue: () => Config.activateOnPlacement,
                setValue: value => Config.activateOnPlacement = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Activate on action",
                getValue: () => Config.activateOnAction,
                setValue: value => Config.activateOnAction = value
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

        public void ActivateSprinkler(Object sprinkler, GameLocation location)
        {
            if (Game1.player.team.SpecialOrderRuleActive("NO_SPRINKLER"))
                return;
            if (sprinkler == null || !sprinkler.IsSprinkler())
                return;

            ObjectPatches.IsVanillaQueryInProgress = false;
            ObjectPatches.CurrentLocation = location;
            foreach (var sprinklerTile in sprinkler.GetSprinklerTiles())
            {
                sprinkler.ApplySprinkler(location, sprinklerTile);
            }
            sprinkler.ApplySprinklerAnimation(location);
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