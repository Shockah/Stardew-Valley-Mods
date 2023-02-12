using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.CommonModCode;
using Shockah.CommonModCode.GMCM;
using Shockah.CommonModCode.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Shockah.XPDisplay
{
	public class XPDisplay : BaseMod<ModConfig>
	{
		private static readonly Rectangle SmallUnobtainedLevelCursorsRectangle = new(129, 338, 7, 9);
		private static readonly Rectangle SmallObtainedLevelCursorsRectangle = new(137, 338, 7, 9);
		private static readonly Rectangle BigObtainedLevelCursorsRectangle = new(159, 338, 13, 9);
		private static readonly Rectangle BigUnobtainedLevelCursorsRectangle = new(145, 338, 13, 9);
		private const float IconToBarSpacing = 3;
		private const float LevelNumberToBarSpacing = 2;
		private const float BarSegmentSpacing = 2;

		private const int FPS = 60;
		private static readonly int[] OrderedSkillIndexes = new[] { 0, 3, 2, 1, 4, 5 };
		private static readonly string SpaceCoreNewSkillsPageQualifiedName = "SpaceCore.Interface.NewSkillsPage, SpaceCore";
		private static readonly string SpaceCoreSkillsQualifiedName = "SpaceCore.Skills, SpaceCore";

		internal static XPDisplay Instance = null!;
		private bool IsWalkOfLifeInstalled = false;
		private bool IsMargoInstalled = false;
		private int[]? XPValues;

		private static readonly IDictionary<(int uiSkillIndex, string? spaceCoreSkillName), (Vector2?, Vector2?)> SkillBarCorners = new Dictionary<(int uiSkillIndex, string? spaceCoreSkillName), (Vector2?, Vector2?)>();
		private static readonly IList<(Vector2, Vector2)> SkillBarHoverExclusions = new List<(Vector2, Vector2)>();
		private static readonly IList<Action> SkillsPageDrawQueuedDelegates = new List<Action>();

		private static readonly Lazy<Func<Toolbar, List<ClickableComponent>>> ToolbarButtonsGetter = new(() => AccessTools.DeclaredField(typeof(Toolbar), "buttons").EmitInstanceGetter<Toolbar, List<ClickableComponent>>());

		private readonly List<Func<Item, (int? SkillIndex, string? SpaceCoreSkillName)>> ToolSkillMatchers = new()
		{
			o => o is Hoe or WateringCan or MilkPail or Shears ? (Farmer.farmingSkill, null) : (null, null),
			o => o is Pickaxe ? (Farmer.miningSkill, null) : (null, null),
			o => o is Axe ? (Farmer.foragingSkill, null) : (null, null),
			o => o is FishingRod ? (Farmer.fishingSkill, null) : (null, null),
			o => o is Sword or Slingshot || (o is MeleeWeapon && !o.Name.Contains("Scythe")) ? (Farmer.combatSkill, null) : (null, null),
		};

		private readonly PerScreen<(int? SkillIndex, string? SpaceCoreSkillName)> ToolbarCurrentSkill = new(() => (null, null));
		private readonly PerScreen<float> ToolbarActiveDuration = new(() => 0f);
		private readonly PerScreen<float> ToolbarAlpha = new(() => 0f);
		private readonly PerScreen<Item?> LastCurrentItem = new(() => null);
		private readonly PerScreen<string?> ToolbarTooltip = new(() => null);

		public override void OnEntry(IModHelper helper)
		{
			Instance = this;
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.Display.MenuChanged += OnMenuChanged;
			helper.Events.Display.RenderingHud += OnRenderingHud;
			helper.Events.Display.RenderedHud += OnRenderedHud;
		}

		public override void MigrateConfig(ISemanticVersion? configVersion, ISemanticVersion modVersion)
		{
			// do nothing, for now
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			SetupConfig();

			IsWalkOfLifeInstalled = Helper.ModRegistry.IsLoaded("DaLion.ImmersiveProfessions");
			IsMargoInstalled = Helper.ModRegistry.IsLoaded("DaLion.Overhaul");
			var harmony = new Harmony(ModManifest.UniqueID);

			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(SkillsPage), nameof(SkillsPage.draw), new Type[] { typeof(SpriteBatch) }),
				postfix: new HarmonyMethod(typeof(XPDisplay), nameof(SkillsPage_draw_Postfix)),
				transpiler: new HarmonyMethod(typeof(XPDisplay), nameof(SkillsPage_draw_Transpiler))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Farmer), nameof(Farmer.gainExperience)),
				prefix: new HarmonyMethod(typeof(XPDisplay), nameof(Farmer_gainExperience_Prefix)),
				postfix: new HarmonyMethod(typeof(XPDisplay), nameof(Farmer_gainExperience_Postfix))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Farmer), "performFireTool"),
				prefix: new HarmonyMethod(typeof(XPDisplay), nameof(Farmer_performFireTool_Prefix))
			);

			if (Helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
			{
				harmony.TryPatch(
					monitor: Monitor,
					original: () => AccessTools.Method(AccessTools.TypeByName(SpaceCoreNewSkillsPageQualifiedName), "draw", new Type[] { typeof(SpriteBatch) }),
					postfix: new HarmonyMethod(typeof(XPDisplay), nameof(SpaceCore_NewSkillsPage_draw_Postfix)),
					transpiler: new HarmonyMethod(typeof(XPDisplay), nameof(SpaceCore_NewSkillsPage_draw_Transpiler))
				);
				harmony.TryPatch(
					monitor: Monitor,
					original: () => AccessTools.Method(AccessTools.TypeByName(SpaceCoreSkillsQualifiedName), "AddExperience"),
					prefix: new HarmonyMethod(typeof(XPDisplay), nameof(SpaceCore_Skills_AddExperience_Prefix)),
					postfix: new HarmonyMethod(typeof(XPDisplay), nameof(SpaceCore_Skills_AddExperience_Postfix))
				);
			}
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			if (Config.ToolbarSkillBar.IsEnabled)
			{
				if (ToolbarActiveDuration.Value > 0f)
				{
					if (!Config.ToolbarSkillBar.AlwaysShowCurrentTool)
						ToolbarActiveDuration.Value = Math.Max(ToolbarActiveDuration.Value - 1f / FPS, 0f);
					if (ToolbarActiveDuration.Value <= 0f && Config.ToolbarSkillBar.AlwaysShowCurrentTool)
						ToolbarCurrentSkill.Value = GetSkillForItem(Game1.player.CurrentItem);
				}

				var targetAlpha = ToolbarActiveDuration.Value > 0f ? 1f : 0f;
				ToolbarAlpha.Value += (targetAlpha - ToolbarAlpha.Value) * 0.15f;
				if (ToolbarAlpha.Value <= 0.01f)
					ToolbarAlpha.Value = 0f;
				else if (ToolbarAlpha.Value >= 0.99f)
					ToolbarAlpha.Value = 1f;
			}

			if (!ReferenceEquals(Game1.player.CurrentItem, LastCurrentItem.Value))
			{
				if (Config.ToolbarSkillBar.IsEnabled && (Config.ToolbarSkillBar.AlwaysShowCurrentTool || Config.ToolbarSkillBar.ToolSwitchDurationInSeconds > 0f))
				{
					var skill = GetSkillForItem(Game1.player.CurrentItem);
					if (skill.SkillIndex is not null || skill.SpaceCoreSkillName is not null)
					{
						ToolbarCurrentSkill.Value = skill;
						ToolbarActiveDuration.Value = Config.ToolbarSkillBar.ToolSwitchDurationInSeconds;
					}
					else
					{
						ToolbarActiveDuration.Value = 0f;
					}
				}
				LastCurrentItem.Value = Game1.player.CurrentItem;
			}
		}

		private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
		{
			if (e.NewMenu is GameMenu)
				UpdateXPValues();
		}

		private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
		{
			ToolbarTooltip.Value = null;
			if (!Config.ToolbarSkillBar.IsEnabled)
				return;
			if (ToolbarAlpha.Value <= 0f)
				return;

			var (skillIndex, spaceCoreSkillName) = ToolbarCurrentSkill.Value;
			if (skillIndex is null && spaceCoreSkillName is null)
				return;

			if (Instance.XPValues is null)
				UpdateXPValues();

			var toolbar = GetToolbar();
			if (toolbar is null)
				return;

			var buttons = ToolbarButtonsGetter.Value(toolbar);
			int toolbarMinX = buttons.Select(b => b.bounds.X).Min();
			int toolbarMaxX = buttons.Select(b => b.bounds.X).Max();
			int toolbarMinY = buttons.Select(b => b.bounds.Y).Min();
			Rectangle toolbarBounds = new(toolbarMinX, toolbarMinY, toolbarMaxX - toolbarMinX + 64, 64);

			var viewportBounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			bool drawBarAboveToolbar = toolbarBounds.Center.Y >= viewportBounds.Center.Y;
			Vector2 barPosition = new(
				toolbarBounds.Center.X,
				drawBarAboveToolbar ? toolbarBounds.Top - Config.ToolbarSkillBar.SpacingFromToolbar : toolbarBounds.Bottom + Config.ToolbarSkillBar.SpacingFromToolbar
			);
			DrawSkillBar(skillIndex, spaceCoreSkillName, e.SpriteBatch, drawBarAboveToolbar ? UIAnchorSide.Bottom : UIAnchorSide.Top, barPosition, Config.ToolbarSkillBar.Scale, ToolbarAlpha.Value);
		}

		private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
		{
			if (ToolbarTooltip.Value is not null)
			{
				IClickableMenu.drawToolTip(e.SpriteBatch, ToolbarTooltip.Value, null, null);
				ToolbarTooltip.Value = null;
			}
		}

		private void SetupConfig()
		{
			var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (api is null)
				return;
			var helper = new GMCMI18nHelper(api, ModManifest, Helper.Translation);

			api.Register(
				ModManifest,
				reset: () => Config = new ModConfig(),
				save: () =>
				{
					WriteConfig();
					LogConfig();
				}
			);

			helper.AddSectionTitle("config.orientation.section");
			helper.AddEnumOption("config.orientation.smallBars", valuePrefix: "config.orientation", property: () => Config.SmallBarOrientation);
			helper.AddEnumOption("config.orientation.bigBars", valuePrefix: "config.orientation", property: () => Config.BigBarOrientation);

			helper.AddSectionTitle("config.appearance.section");
			helper.AddNumberOption("config.appearance.alpha", () => Config.Alpha, min: 0f, max: 1f, interval: 0.05f);
		}

		private void UpdateXPValues()
		{
			int maxLevel = Farmer.checkForLevelGain(0, int.MaxValue);
			if (maxLevel <= 0)
			{
				XPValues = Array.Empty<int>();
				return;
			}

			int maxValue = int.MaxValue;
			int minValue = 0;
			int[] values = new int[maxLevel];
			for (int level = maxLevel; level > 0; level--)
			{
				while (maxValue - minValue > 1)
				{
					int midValue = minValue + (maxValue - minValue) / 2;
					int stepLevel = Farmer.checkForLevelGain(0, midValue);
					if (stepLevel >= level)
						maxValue = midValue;
					else
						minValue = midValue;
				}

				values[level - 1] = maxValue;
				minValue = 0;
			}

			XPValues = values;
		}

		private (int? SkillIndex, string? SpaceCoreSkillName) GetSkillForItem(Item? item)
		{
			if (item is null)
				return (null, null);

			foreach (var matcher in ToolSkillMatchers)
			{
				var skill = matcher(item);
				if (skill.SkillIndex is not null || skill.SpaceCoreSkillName is not null)
					return skill;
			}

			return (null, null);
		}

		private static Toolbar? GetToolbar()
			=> Game1.onScreenMenus.OfType<Toolbar>().FirstOrDefault();

		private void DrawSkillBar(int? skillIndex, string? spaceCoreSkillName, SpriteBatch b, UIAnchorSide anchorSide, Vector2 position, float scale, float alpha)
		{
			int currentLevel = GetUnmodifiedSkillLevel(skillIndex, spaceCoreSkillName);
			int nextLevelXP = GetLevelXP(currentLevel, spaceCoreSkillName);
			int currentLevelXP = currentLevel == 0 ? 0 : GetLevelXP(currentLevel - 1, spaceCoreSkillName);
			int currentXP = GetCurrentXP(skillIndex, spaceCoreSkillName);
			float nextLevelProgress = Math.Clamp(1f * (currentXP - currentLevelXP) / (nextLevelXP - currentLevelXP), 0f, 1f);

			var icon = Config.ToolbarSkillBar.ShowIcon ? GetSkillIcon(skillIndex, spaceCoreSkillName) : null;

			var barSize = new Vector2(
				SmallObtainedLevelCursorsRectangle.Size.X * 8 + BigObtainedLevelCursorsRectangle.Size.X * 2 + BarSegmentSpacing * 9
					+ (icon is null ? 0f : icon.Value.Rectangle.Width + IconToBarSpacing)
					+ (Config.ToolbarSkillBar.ShowLevelNumber ? NumberSprite.getWidth(99) - 2 + LevelNumberToBarSpacing : 0f),
				BigObtainedLevelCursorsRectangle.Size.Y
			) * scale;
			var wholeToolbarTopLeft = position - anchorSide.GetAnchorOffset(barSize);

			float xOffset = 0;
			if (icon is not null)
			{
				Vector2 iconSize = new(icon.Value.Rectangle.Width, icon.Value.Rectangle.Height);
				var iconPosition = wholeToolbarTopLeft + UIAnchorSide.Center.GetAnchorOffset(iconSize) * scale;
				b.Draw(icon.Value.Texture, iconPosition + new Vector2(-1, 1) * scale, icon.Value.Rectangle, Color.Black * alpha * 0.3f, 0f, iconSize / 2f, scale, SpriteEffects.None, 0f);
				b.Draw(icon.Value.Texture, iconPosition, icon.Value.Rectangle, Color.White * alpha, 0f, iconSize / 2f, scale, SpriteEffects.None, 0f);
				xOffset += icon.Value.Rectangle.Width + IconToBarSpacing;
			}

			for (int levelIndex = 0; levelIndex < 10; levelIndex++)
			{
				if (levelIndex != 0)
					xOffset += BarSegmentSpacing;

				bool isBigLevel = (levelIndex + 1) % 5 == 0;
				Orientation orientation = isBigLevel ? Instance.Config.BigBarOrientation : Instance.Config.SmallBarOrientation;
				Texture2D barTexture = Game1.mouseCursors;
				Rectangle barTextureRectangle = isBigLevel
					? (currentLevel > levelIndex) ? BigObtainedLevelCursorsRectangle : BigUnobtainedLevelCursorsRectangle
					: (currentLevel > levelIndex) ? SmallObtainedLevelCursorsRectangle : SmallUnobtainedLevelCursorsRectangle;

				var topLeft = wholeToolbarTopLeft + new Vector2(xOffset * scale, 0);
				b.Draw(barTexture, topLeft + new Vector2(-1, 1) * scale, barTextureRectangle, Color.Black * alpha * 0.3f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
				b.Draw(barTexture, topLeft, barTextureRectangle, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

				if (currentLevel % 10 != levelIndex)
				{
					xOffset += barTextureRectangle.Width;
					continue;
				}

				barTextureRectangle = isBigLevel ? BigObtainedLevelCursorsRectangle : SmallObtainedLevelCursorsRectangle;

				if (currentLevel >= 10 && currentLevel > levelIndex + 10)
				{
					if (Instance.IsWalkOfLifeInstalled && WalkOfLifeBridge.IsPrestigeEnabled())
						(barTexture, barTextureRectangle) = isBigLevel ? WalkOfLifeBridge.GetExtendedBigBar()!.Value : WalkOfLifeBridge.GetExtendedSmallBar()!.Value;
					else if (Instance.IsMargoInstalled && MargoBridge.IsPrestigeEnabled())
						(barTexture, barTextureRectangle) = isBigLevel ? MargoBridge.GetExtendedBigBar()!.Value : MargoBridge.GetExtendedSmallBar()!.Value;
				}

				Vector2 barPosition;
				switch (orientation)
				{
					case Orientation.Horizontal:
						int rectangleWidthPixels = (int)(barTextureRectangle.Width * nextLevelProgress);
						barPosition = topLeft;
						barTextureRectangle = new(
							barTextureRectangle.Left,
							barTextureRectangle.Top,
							rectangleWidthPixels,
							barTextureRectangle.Height
						);
						break;
					case Orientation.Vertical:
						int rectangleHeightPixels = (int)(barTextureRectangle.Height * nextLevelProgress);
						barPosition = topLeft + new Vector2(0f, (barTextureRectangle.Height - rectangleHeightPixels) * scale);
						barTextureRectangle = new(
							barTextureRectangle.Left,
							barTextureRectangle.Top + barTextureRectangle.Height - rectangleHeightPixels,
							barTextureRectangle.Width,
							rectangleHeightPixels
						);
						break;
					default:
						throw new ArgumentException($"{nameof(Orientation)} has an invalid value.");
				}

				b.Draw(barTexture, barPosition, barTextureRectangle, Color.White * alpha * Instance.Config.Alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
				xOffset += barTextureRectangle.Width;
			}

			if (Config.ToolbarSkillBar.ShowLevelNumber)
			{
				xOffset += LevelNumberToBarSpacing;
				bool isModifiedSkill = GetModifiedSkillLevel(skillIndex, spaceCoreSkillName) != currentLevel;
				int modifiedLevel = GetModifiedSkillLevel(skillIndex, spaceCoreSkillName);

				Vector2 levelNumberPosition = wholeToolbarTopLeft + new Vector2(xOffset + 2f + NumberSprite.getWidth(modifiedLevel) / 2f, NumberSprite.getHeight() / 2f) * scale;
				NumberSprite.draw(modifiedLevel, b, levelNumberPosition + new Vector2(-1, 1) * scale, Color.Black * alpha * 0.35f, 1f, 0f, 1f, 0);
				NumberSprite.draw(modifiedLevel, b, levelNumberPosition, (isModifiedSkill ? Color.LightGreen : Color.SandyBrown) * (modifiedLevel == 0 ? 0.75f : 1f) * alpha, 1f, 0f, 1f, 0);
			}

			if (nextLevelXP != int.MaxValue)
			{
				Vector2 mouse = new(Game1.getMouseX(), Game1.getMouseY());
				if (mouse.X >= wholeToolbarTopLeft.X && mouse.Y >= wholeToolbarTopLeft.Y && mouse.X < wholeToolbarTopLeft.X + barSize.X && mouse.Y < wholeToolbarTopLeft.Y + barSize.Y)
				{
					ToolbarTooltip.Value = GetSkillTooltip(skillIndex, spaceCoreSkillName);
					ToolbarActiveDuration.Value = Math.Max(ToolbarActiveDuration.Value, 1f);
				}
			}
		}

		private static int GetUnmodifiedSkillLevel(int? skillIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is not null)
				return SpaceCoreBridge.GetUnmodifiedSkillLevel(spaceCoreSkillName);
			else if (skillIndex is not null)
				return Game1.player.GetUnmodifiedSkillLevel(skillIndex.Value);
			else
				throw new ArgumentException($"Missing both {nameof(skillIndex)} and {spaceCoreSkillName} parameters.");
		}

		private static int GetModifiedSkillLevel(int? skillIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is not null)
				return SpaceCoreBridge.GetUnmodifiedSkillLevel(spaceCoreSkillName);
			else if (skillIndex is not null)
				return Game1.player.GetSkillLevel(skillIndex.Value);
			else
				throw new ArgumentException($"Missing both {nameof(skillIndex)} and {spaceCoreSkillName} parameters.");
		}

		private static int GetLevelXP(int levelIndex, string? spaceCoreSkillName)
		{
			if (Instance.XPValues is null)
				throw new InvalidOperationException("`XPValues` should be set by now, but it's not.");

			if (spaceCoreSkillName is null)
				return Instance.XPValues.Length > levelIndex ? Instance.XPValues[levelIndex] : int.MaxValue;
			else
				return SpaceCoreBridge.GetLevelXP(levelIndex, spaceCoreSkillName);
		}

		private static int GetCurrentXP(int? skillIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is not null)
				return SpaceCoreBridge.GetCurrentXP(spaceCoreSkillName);
			else if (skillIndex is not null)
				return Game1.player.experiencePoints[skillIndex.Value];
			else
				throw new ArgumentException($"Missing both {nameof(skillIndex)} and {spaceCoreSkillName} parameters.");
		}

		private static (Texture2D Texture, Rectangle Rectangle)? GetSkillIcon(int? skillIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is not null)
			{
				var icon = SpaceCoreBridge.GetSkillIcon(spaceCoreSkillName);
				if (icon is null)
					return null;
				return (icon, new(0, 0, icon.Width, icon.Height));
			}

			if (skillIndex is Farmer.farmingSkill)
				return (Game1.mouseCursors, new(10, 428, 10, 10));
			else if (skillIndex is Farmer.miningSkill)
				return (Game1.mouseCursors, new(30, 428, 10, 10));
			else if (skillIndex is Farmer.foragingSkill)
				return (Game1.mouseCursors, new(60, 428, 10, 10));
			else if (skillIndex is Farmer.fishingSkill)
				return (Game1.mouseCursors, new(20, 428, 10, 10));
			else if (skillIndex is Farmer.combatSkill)
				return (Game1.mouseCursors, new(120, 428, 10, 10));
			else if (skillIndex is Farmer.luckSkill)
				return (Game1.mouseCursors, new(50, 428, 10, 10));
			else if (skillIndex is not null)
				throw new ArgumentException($"Unknown skill index {skillIndex}.");
			else
				throw new ArgumentException($"Missing both {nameof(skillIndex)} and {spaceCoreSkillName} parameters.");
		}

		private string GetSkillTooltip(int? skillIndex, string? spaceCoreSkillName)
		{
			int currentLevel = GetUnmodifiedSkillLevel(skillIndex, spaceCoreSkillName);
			int nextLevelXP = GetLevelXP(currentLevel, spaceCoreSkillName);
			int currentXP = GetCurrentXP(skillIndex, spaceCoreSkillName);
			int currentLevelXP = currentLevel == 0 ? 0 : GetLevelXP(currentLevel - 1, spaceCoreSkillName);
			float nextLevelProgress = Math.Clamp(1f * (currentXP - currentLevelXP) / (nextLevelXP - currentLevelXP), 0f, 1f);

			return Helper.Translation.Get(
				"tooltip.text",
				new
				{
					CurrentXP = currentXP - currentLevelXP,
					NextLevelXP = nextLevelXP - currentLevelXP,
					LevelPercent = (int)(nextLevelProgress * 100f)
				}
			);

		}

		private void DrawSkillTooltip(SpriteBatch b, int? skillIndex, string? spaceCoreSkillName)
			=> IClickableMenu.drawToolTip(b, GetSkillTooltip(skillIndex, spaceCoreSkillName), null, null);

		private static void Farmer_gainExperience_Prefix(Farmer __instance, int which, ref int __state)
		{
			if (__instance != Game1.player)
				return;
			__state = GetUnmodifiedSkillLevel(which, null);
		}

		private static void Farmer_gainExperience_Postfix(Farmer __instance, int which, ref int __state)
		{
			if (__instance != Game1.player)
				return;

			float xpChangedDuration = Instance.Config.ToolbarSkillBar.XPChangedDurationInSeconds;
			float levelChangedDuration = Instance.Config.ToolbarSkillBar.LevelChangedDurationInSeconds;
			if (__state == GetUnmodifiedSkillLevel(which, null))
				levelChangedDuration = 0f;
			else if (!string.IsNullOrEmpty(Instance.Config.LevelUpSoundName))
				Game1.playSound(Instance.Config.LevelUpSoundName);

			if (!Instance.Config.ToolbarSkillBar.IsEnabled)
				return;

			var maxDuration = Math.Max(xpChangedDuration, levelChangedDuration);
			if (maxDuration > 0f)
			{
				Instance.ToolbarCurrentSkill.Value = (which, null);
				Instance.ToolbarActiveDuration.Value = maxDuration;
			}
		}

		private static void Farmer_performFireTool_Prefix(Farmer __instance)
		{
			if (__instance != Game1.player)
				return;
			if (!Instance.Config.ToolbarSkillBar.IsEnabled)
				return;

			if (Instance.Config.ToolbarSkillBar.ToolUseDurationInSeconds > 0f)
			{
				var skill = Instance.GetSkillForItem(Game1.player.CurrentItem);
				if (skill.SkillIndex is not null || skill.SpaceCoreSkillName is not null)
				{
					Instance.ToolbarCurrentSkill.Value = skill;
					Instance.ToolbarActiveDuration.Value = Instance.Config.ToolbarSkillBar.ToolUseDurationInSeconds;
				}
				else
				{
					Instance.ToolbarActiveDuration.Value = 0f;
				}
			}
		}

		private static void SpaceCore_Skills_AddExperience_Prefix(Farmer farmer, string skillName, ref int __state)
		{
			if (farmer != Game1.player)
				return;
			if (!Instance.Config.ToolbarSkillBar.IsEnabled)
				return;
			__state = GetUnmodifiedSkillLevel(null, skillName);
		}

		private static void SpaceCore_Skills_AddExperience_Postfix(Farmer farmer, string skillName, ref int __state)
		{
			if (farmer != Game1.player)
				return;

			float xpChangedDuration = Instance.Config.ToolbarSkillBar.XPChangedDurationInSeconds;
			float levelChangedDuration = Instance.Config.ToolbarSkillBar.LevelChangedDurationInSeconds;
			if (__state == GetUnmodifiedSkillLevel(null, skillName))
				levelChangedDuration = 0f;
			else if (!string.IsNullOrEmpty(Instance.Config.LevelUpSoundName))
				Game1.playSound(Instance.Config.LevelUpSoundName);

			if (!Instance.Config.ToolbarSkillBar.IsEnabled)
				return;

			var maxDuration = Math.Max(xpChangedDuration, levelChangedDuration);
			if (maxDuration > 0f)
			{
				Instance.ToolbarCurrentSkill.Value = (null, skillName);
				Instance.ToolbarActiveDuration.Value = maxDuration;
			}
		}

		private static void SkillsPage_draw_Postfix(SpriteBatch b)
		{
			DrawSkillsPageExperienceTooltip(b);
		}

		private static IEnumerable<CodeInstruction> SkillsPage_draw_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.Do(matcher =>
					{
						return matcher
							.Find(
								ILMatches.AnyLdloc,
								ILMatches.LdcI4(9),
								ILMatches.BneUn
							)
							.PointerMatcher(SequenceMatcherRelativeElement.First)
							.Advance()
							.Insert(
								SequenceMatcherPastBoundsDirection.Before, true,

								new CodeInstruction(OpCodes.Ldarg_1), // `SpriteBatch`

								new CodeInstruction(OpCodes.Ldloc_0), // this *should* be the `x` local
								new CodeInstruction(OpCodes.Ldloc_2), // this *should* be the `addedX` local
								new CodeInstruction(OpCodes.Add),

								new CodeInstruction(OpCodes.Ldloc_1), // this *should* be the `y` local
								new CodeInstruction(OpCodes.Ldloc_3), // this *should* be the `i` local - the currently drawn level index (0-9)
								new CodeInstruction(OpCodes.Ldloc, 4), // this *should* be the `j` local - the skill index
								new CodeInstruction(OpCodes.Ldnull), // no skill name, it's a built-in one
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XPDisplay), nameof(SkillsPage_draw_QueueDelegate)))
							);
					})
					.Do(matcher =>
					{
						var skillsPageSkillBarsField = AccessTools.Field(typeof(SkillsPage), nameof(SkillsPage.skillBars));
						return matcher
							.Repeat(2, matcher =>
							{
								return matcher
									.Find(
										ILMatches.Ldarg(0),
										ILMatches.Ldfld(skillsPageSkillBarsField),
										ILMatches.Call(AccessTools.Method(skillsPageSkillBarsField.FieldType, "GetEnumerator"))
									);
							})
							.PointerMatcher(SequenceMatcherRelativeElement.First)
							.Advance()
							.Insert(
								SequenceMatcherPastBoundsDirection.Before, true,
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XPDisplay), nameof(SkillsPage_draw_CallQueuedDelegates)))
							);
					})
					.AllElements();
			}
			catch (Exception ex)
			{
				Instance.Monitor.Log($"Could not patch methods - {Instance.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static void SpaceCore_NewSkillsPage_draw_Postfix(SpriteBatch b)
		{
			DrawSkillsPageExperienceTooltip(b);
		}

		private static IEnumerable<CodeInstruction> SpaceCore_NewSkillsPage_draw_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.Do(matcher =>
					{
						return matcher
							.Find(
								ILMatches.AnyLdloc,
								ILMatches.LdcI4(9),
								ILMatches.BneUn
							)
							.Do(matcher =>
							{
								return matcher
									.PointerMatcher(SequenceMatcherRelativeElement.First)
									.Advance()
									.Insert(
										SequenceMatcherPastBoundsDirection.Before, true,

										new CodeInstruction(OpCodes.Ldarg_1), // `SpriteBatch`

										new CodeInstruction(OpCodes.Ldloc_0), // this *should* be the `x` local
										new CodeInstruction(OpCodes.Ldloc_3), // this *should* be the `xOffset` local
										new CodeInstruction(OpCodes.Add),

										new CodeInstruction(OpCodes.Ldloc_1), // this *should* be the `y` local
										new CodeInstruction(OpCodes.Ldloc, 8), // this *should* be the `levelIndex` local
										new CodeInstruction(OpCodes.Ldloc, 9), // this *should* be the `skillIndex` local
										new CodeInstruction(OpCodes.Ldnull), // no skill name, it's a built-in one
										new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XPDisplay), nameof(SkillsPage_draw_QueueDelegate)))
									);
							})
							.Find(
								ILMatches.AnyLdloc,
								ILMatches.LdcI4(9),
								ILMatches.BneUn
							)
							.Do(matcher =>
							{
								return matcher
									.PointerMatcher(SequenceMatcherRelativeElement.First)
									.Advance()
									.Insert(
										SequenceMatcherPastBoundsDirection.Before, true,

										new CodeInstruction(OpCodes.Ldarg_1), // `SpriteBatch`

										new CodeInstruction(OpCodes.Ldloc_0), // this *should* be the `x` local
										new CodeInstruction(OpCodes.Ldloc_3), // this *should* be the `xOffset` local
										new CodeInstruction(OpCodes.Add),

										new CodeInstruction(OpCodes.Ldloc_1), // this *should* be the `y` local
										new CodeInstruction(OpCodes.Ldloc, 19), // this *should* be the `levelIndex` local
										new CodeInstruction(OpCodes.Ldloc_2), // this *should* be the `indexWithLuckSkill` local
										new CodeInstruction(OpCodes.Ldloc, 17), // this *should* be the `skillName` local
										new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XPDisplay), nameof(SkillsPage_draw_QueueDelegate)))
									);
							});
					})
					.Do(matcher =>
					{
						var skillsPageSkillBarsField = AccessTools.Field(AccessTools.TypeByName(SpaceCoreNewSkillsPageQualifiedName), "skillBars");
						return matcher
							.Repeat(2, matcher =>
							{
								return matcher
									.Find(
										ILMatches.Ldarg(0),
										ILMatches.Ldfld(skillsPageSkillBarsField),
										ILMatches.Call(AccessTools.Method(skillsPageSkillBarsField.FieldType, "GetEnumerator"))
									);
							})
							.PointerMatcher(SequenceMatcherRelativeElement.First)
							.Advance()
							.Insert(
								SequenceMatcherPastBoundsDirection.Before, true,
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XPDisplay), nameof(SkillsPage_draw_CallQueuedDelegates)))
							);
					})
					.AllElements();
			}
			catch (Exception ex)
			{
				Instance.Monitor.Log($"Could not patch SpaceCore methods - {Instance.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		public static void SkillsPage_draw_QueueDelegate(SpriteBatch b, int x, int y, int levelIndex, int uiSkillIndex, string? spaceCoreSkillName)
		{
			int skillIndex = OrderedSkillIndexes.Length > uiSkillIndex ? OrderedSkillIndexes[uiSkillIndex] : uiSkillIndex;
			if (Instance.XPValues is null)
				return;

			bool isBigLevel = (levelIndex + 1) % 5 == 0;
			Texture2D barTexture = Game1.mouseCursors;
			Rectangle barTextureRectangle = isBigLevel ? BigObtainedLevelCursorsRectangle : SmallObtainedLevelCursorsRectangle;
			float scale = 4f;

			Vector2 topLeft = new(x + levelIndex * 36, y - 4 + uiSkillIndex * 56);
			Vector2 bottomRight = topLeft + new Vector2(barTextureRectangle.Width, barTextureRectangle.Height) * scale;

			int currentLevel = GetUnmodifiedSkillLevel(skillIndex, spaceCoreSkillName);
			int nextLevelXP = GetLevelXP(currentLevel, spaceCoreSkillName);
			if (levelIndex is 4 or 9 && currentLevel >= levelIndex)
				SkillBarHoverExclusions.Add((topLeft, bottomRight));

			if (nextLevelXP != int.MaxValue && levelIndex is 0 or 9)
			{
				var key = (uiSkillIndex, spaceCoreSkillName);
				if (!SkillBarCorners.ContainsKey(key))
					SkillBarCorners[key] = (null, null);
				if (levelIndex == 0)
					SkillBarCorners[key] = (topLeft, SkillBarCorners[key].Item2);
				else if (levelIndex == 9)
					SkillBarCorners[key] = (SkillBarCorners[key].Item1, bottomRight);
			}

			if (currentLevel % 10 != levelIndex)
				return;
			int currentLevelXP = currentLevel == 0 ? 0 : GetLevelXP(currentLevel - 1, spaceCoreSkillName);
			int currentXP = GetCurrentXP(skillIndex, spaceCoreSkillName);
			float nextLevelProgress = Math.Clamp(1f * (currentXP - currentLevelXP) / (nextLevelXP - currentLevelXP), 0f, 1f);

			Orientation orientation = isBigLevel ? Instance.Config.BigBarOrientation : Instance.Config.SmallBarOrientation;

			if (currentLevel >= 10)
			{
				if (Instance.IsWalkOfLifeInstalled && WalkOfLifeBridge.IsPrestigeEnabled())
					(barTexture, barTextureRectangle) = isBigLevel ? WalkOfLifeBridge.GetExtendedBigBar()!.Value : WalkOfLifeBridge.GetExtendedSmallBar()!.Value;
				else if (Instance.IsMargoInstalled && MargoBridge.IsPrestigeEnabled())
					(barTexture, barTextureRectangle) = isBigLevel ? MargoBridge.GetExtendedBigBar()!.Value : MargoBridge.GetExtendedSmallBar()!.Value;
			}

			Vector2 barPosition;
			switch (orientation)
			{
				case Orientation.Horizontal:
					int rectangleWidthPixels = (int)(barTextureRectangle.Width * nextLevelProgress);
					barPosition = topLeft;
					barTextureRectangle = new(
						barTextureRectangle.Left,
						barTextureRectangle.Top,
						rectangleWidthPixels,
						barTextureRectangle.Height
					);
					break;
				case Orientation.Vertical:
					int rectangleHeightPixels = (int)(barTextureRectangle.Height * nextLevelProgress);
					barPosition = topLeft + new Vector2(0f, (barTextureRectangle.Height - rectangleHeightPixels) * scale);
					barTextureRectangle = new(
						barTextureRectangle.Left,
						barTextureRectangle.Top + barTextureRectangle.Height - rectangleHeightPixels,
						barTextureRectangle.Width,
						rectangleHeightPixels
					);
					break;
				default:
					throw new ArgumentException($"{nameof(Orientation)} has an invalid value.");
			}
			SkillsPageDrawQueuedDelegates.Add(() => b.Draw(barTexture, barPosition, barTextureRectangle, Color.White * Instance.Config.Alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.87f));
		}

		public static void SkillsPage_draw_CallQueuedDelegates()
		{
			foreach (var @delegate in SkillsPageDrawQueuedDelegates)
				@delegate();
			SkillsPageDrawQueuedDelegates.Clear();
		}

		private static void DrawSkillsPageExperienceTooltip(SpriteBatch b)
		{
			if (Instance.XPValues is null)
				return;

			int mouseX = Game1.getMouseX();
			int mouseY = Game1.getMouseY();
			bool isHoverExcluded = SkillBarHoverExclusions.Any(e => mouseX >= e.Item1.X && mouseY >= e.Item1.Y && mouseX < e.Item2.X && mouseY < e.Item2.Y);
			if (!isHoverExcluded)
			{
				(int uiSkillIndex, string? spaceCoreSkillName)? hoveredUiSkill = SkillBarCorners
					.Where(kv => kv.Value.Item1 is not null && kv.Value.Item2 is not null)
					.Where(kv => mouseX >= kv.Value.Item1!.Value.X && mouseY >= kv.Value.Item1!.Value.Y && mouseX < kv.Value.Item2!.Value.X && mouseY < kv.Value.Item2!.Value.Y)
					.FirstOrNull()
					?.Key;
				if (hoveredUiSkill is not null)
				{
					var (uiSkillIndex, spaceCoreSkillName) = hoveredUiSkill.Value;
					int skillIndex = OrderedSkillIndexes.Length > uiSkillIndex ? OrderedSkillIndexes[uiSkillIndex] : uiSkillIndex;
					int currentLevel = GetUnmodifiedSkillLevel(skillIndex, spaceCoreSkillName);
					int nextLevelXP = GetLevelXP(currentLevel, spaceCoreSkillName);
					if (nextLevelXP != int.MaxValue)
						Instance.DrawSkillTooltip(b, skillIndex, spaceCoreSkillName);
				}
			}
			SkillBarCorners.Clear();
			SkillBarHoverExclusions.Clear();
		}
	}
}