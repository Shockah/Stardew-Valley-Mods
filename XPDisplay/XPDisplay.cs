using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Shockah.XPView
{
	public class XPDisplay: Mod
	{
		private static readonly Rectangle SmallObtainedLevelCursorsRectangle = new(137, 338, 7, 9);
		private static readonly Rectangle BigObtainedLevelCursorsRectangle = new(159, 338, 13, 9);
		private static readonly int[] OrderedSkillIndexes = new[] { 0, 3, 2, 1, 4, 5 };
		private static readonly string LuckSkillModQualifiedName = "LuckSkill.Mod, LuckSkill";
		private static readonly string SpaceCoreNewSkillsPageQualifiedName = "SpaceCore.Interface.NewSkillsPage, SpaceCore";

		private static XPDisplay Instance = null!;
		internal ModConfig Config { get; private set; } = null!;
		private bool IsWalkOfLifeInstalled = false;
		private int[] XPValues = null!;

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<ModConfig>();
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			SetupConfig();
			
			var harmony = new Harmony(ModManifest.UniqueID);
			try
			{
				harmony.Patch(
					original: AccessTools.Method(typeof(Farmer), nameof(Farmer.checkForLevelGain)),
					transpiler: new HarmonyMethod(typeof(XPDisplay), nameof(Farmer_checkForLevelGain_Transpiler))
				);

				harmony.Patch(
					original: AccessTools.Method(typeof(SkillsPage), nameof(SkillsPage.draw), new Type[] { typeof(SpriteBatch) }),
					transpiler: new HarmonyMethod(typeof(XPDisplay), nameof(SkillsPage_draw_Transpiler))
				);

				if (Helper.ModRegistry.IsLoaded("spacechase0.LuckSkill"))
				{
					harmony.Patch(
						original: AccessTools.Method(AccessTools.TypeByName(LuckSkillModQualifiedName), "DrawLuckSkill", new Type[] { typeof(SkillsPage) }),
						transpiler: new HarmonyMethod(typeof(XPDisplay), nameof(LuckSkill_DrawLuckSkill_Transpiler))
					);
				}

				if (Helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
				{
					harmony.Patch(
						original: AccessTools.Method(AccessTools.TypeByName(SpaceCoreNewSkillsPageQualifiedName), "draw", new Type[] { typeof(SpriteBatch) }),
						transpiler: new HarmonyMethod(typeof(XPDisplay), nameof(SpaceCore_NewSkillsPage_draw_Transpiler))
					);
				}

				IsWalkOfLifeInstalled = Helper.ModRegistry.IsLoaded("DaLion.AwesomeProfessions");
			}
			catch (Exception ex)
			{
				Monitor.Log($"Could not patch methods - XP View probably won't work.\nReason: {ex}", LogLevel.Error);
			}
		}

		private void SetupConfig()
		{
			var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

			string Translate(ModConfig.Orientation orientation)
			{
				return orientation switch
				{
					ModConfig.Orientation.Horizontal => Helper.Translation.Get("config.orientation.horizontal"),
					ModConfig.Orientation.Vertical => Helper.Translation.Get("config.orientation.vertical"),
					_ => throw new ArgumentException(),
				};
			}

			configMenu?.Register(
				ModManifest,
				reset: () => Config = new ModConfig(),
				save: () => Helper.WriteConfig(Config)
			);

			configMenu?.AddSectionTitle(
				mod: ModManifest,
				text: () => Helper.Translation.Get("config.orientation.section.text"),
				tooltip: () => Helper.Translation.Get("config.orientation.section.tooltip")
			);

			configMenu?.AddTextOption(
				mod: ModManifest,
				name: () => Helper.Translation.Get("config.orientation.smallBars.name"),
				tooltip: () => Helper.Translation.Get("config.orientation.smallBars.tooltip"),
				getValue: () => Translate(Config.SmallBarOrientation),
				setValue: value => Config.SmallBarOrientation = I18nEnum.GetFromTranslation<ModConfig.Orientation>(value, Translate),
				allowedValues: I18nEnum.GetTranslations<ModConfig.Orientation>(Translate).ToArray()
			);

			configMenu?.AddTextOption(
				mod: ModManifest,
				name: () => Helper.Translation.Get("config.orientation.bigBars.name"),
				tooltip: () => Helper.Translation.Get("config.orientation.bigBars.tooltip"),
				getValue: () => Translate(Config.BigBarOrientation),
				setValue: value => Config.BigBarOrientation = I18nEnum.GetFromTranslation<ModConfig.Orientation>(value, Translate),
				allowedValues: I18nEnum.GetTranslations<ModConfig.Orientation>(Translate).ToArray()
			);

			configMenu?.AddSectionTitle(
				mod: ModManifest,
				text: () => Helper.Translation.Get("config.appearance.section.text")
			);

			configMenu?.AddNumberOption(
				mod: ModManifest,
				name: () => Helper.Translation.Get("config.appearance.alpha.name"),
				tooltip: () => Helper.Translation.Get("config.appearance.alpha.tooltip"),
				getValue: () => Config.Alpha,
				setValue: value => Config.Alpha = value,
				min: 0, max: 1, interval: 0.05f
			);
		}

		private static IEnumerable<CodeInstruction> Farmer_checkForLevelGain_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();
			var xpValues = new List<int>();
			int currentInstructionIndex = 0;

			while (true)
			{
				// IL to find:
				// ldarg.0
				// <any ldc.i4> <any value>
				var worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
				{
					i => i.IsLdarg(0),
					i => i.IsLdcI4()
				}, startIndex: currentInstructionIndex);
				if (worker is null)
					break;

				xpValues.Add(worker[1].GetLdcI4Value()!.Value);
				currentInstructionIndex = worker.EndIndex;
			}

			Instance.XPValues = xpValues.OrderBy(v => v).ToArray();
			return instructions;
		}

		private static IEnumerable<CodeInstruction> SkillsPage_draw_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();

			// IL to find:
			// IL_07bf: ldloc.3
			// IL_07c0: ldc.i4.s 9
			// IL_07c2: bne.un IL_0881
			var worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdloc(),
				i => i.IsLdcI4(9),
				i => i.IsBneUn()
			});
			if (worker is null)
			{
				Instance.Monitor.Log($"Could not patch methods - XP View probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
				return instructions;
			}

			worker.Insert(1, new[]
			{
				new CodeInstruction(OpCodes.Ldarg_1), // `SpriteBatch`

				new CodeInstruction(OpCodes.Ldloc_0), // this *should* be the `x` local
				new CodeInstruction(OpCodes.Ldloc_2), // this *should* be the `addedX` local
				new CodeInstruction(OpCodes.Add),

				new CodeInstruction(OpCodes.Ldloc_1), // this *should* be the `y` local
				new CodeInstruction(OpCodes.Ldloc_3), // this *should* be the `i` local - the currently drawn level index (0-9)
				new CodeInstruction(OpCodes.Ldloc, 4), // this *should* be the `j` local - the skill index
				new CodeInstruction(OpCodes.Ldnull), // no skill name, it's a built-in one
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XPDisplay), nameof(SkillsPage_draw_Addition)))
			});

			return instructions;
		}

		private static IEnumerable<CodeInstruction> LuckSkill_DrawLuckSkill_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();

			// IL to find:
			// IL_0381: ldloc.s 7
			// IL_0383: ldc.i4.s 9
			// IL_0385: bne.un IL_044a
			var worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdloc(),
				i => i.IsLdcI4(9),
				i => i.IsBneUn()
			});
			if (worker is null)
			{
				Instance.Monitor.Log($"Could not patch Luck Skill" +
					$" methods - XP View probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
				return instructions;
			}

			worker.Insert(1, new[]
			{
				new CodeInstruction(OpCodes.Ldloc_0), // this *should* be the `sb` local (SpriteBatch)

				new CodeInstruction(OpCodes.Ldloc, 4), // this *should* be the `num` local
				new CodeInstruction(OpCodes.Ldloc, 6), // this *should* be the `num3` local
				new CodeInstruction(OpCodes.Add),

				new CodeInstruction(OpCodes.Ldloc, 5), // this *should* be the `y` local
				new CodeInstruction(OpCodes.Ldloc, 7), // this *should* be the `i` local - the currently drawn level index (0-9)
				new CodeInstruction(OpCodes.Ldc_I4, 5), // the skill index of the luck skill
				new CodeInstruction(OpCodes.Ldnull), // no skill name, it's a "built-in" one
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XPDisplay), nameof(SkillsPage_draw_Addition)))
			});

			return instructions;
		}

		private static IEnumerable<CodeInstruction> SpaceCore_NewSkillsPage_draw_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();

			// IL to find:
			// IL_08b1: ldloc.s 8
			// IL_08b3: ldc.i4.s 9
			// IL_08b5: bne.un IL_0976
			var worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdloc(),
				i => i.IsLdcI4(9),
				i => i.IsBneUn()
			});
			if (worker is null)
			{
				Instance.Monitor.Log($"Could not patch SpaceCore methods - XP View probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
			}
			else
			{
				// TODO: some kind of local finder to stop hardcoding the indexes
				worker.Insert(1, new[]
				{
					new CodeInstruction(OpCodes.Ldarg_1), // `SpriteBatch`

					new CodeInstruction(OpCodes.Ldloc_0), // this *should* be the `x` local
					new CodeInstruction(OpCodes.Ldloc_3), // this *should* be the `xOffset` local
					new CodeInstruction(OpCodes.Add),

					new CodeInstruction(OpCodes.Ldloc_1), // this *should* be the `y` local
					new CodeInstruction(OpCodes.Ldloc, 8), // this *should* be the `levelIndex` local
					new CodeInstruction(OpCodes.Ldloc, 9), // this *should* be the `skillIndex` local
					new CodeInstruction(OpCodes.Ldnull), // no skill name, it's a built-in one
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XPDisplay), nameof(SkillsPage_draw_Addition)))
				});
			}

			// IL to find:
			// IL_0cc1: ldloc.s 19
			// IL_0cc3: ldc.i4.s 9
			// IL_0cc5: bne.un IL_0d84
			worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdloc(),
				i => i.IsLdcI4(9),
				i => i.IsBneUn()
			}, startIndex: worker?.EndIndex ?? 0);
			if (worker is null)
			{
				Instance.Monitor.Log($"Could not patch SpaceCore methods - XP View probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
			}
			else
			{
				// TODO: some kind of local finder to stop hardcoding the indexes
				worker.Insert(1, new[]
				{
					new CodeInstruction(OpCodes.Ldarg_1), // `SpriteBatch`

					new CodeInstruction(OpCodes.Ldloc_0), // this *should* be the `x` local
					new CodeInstruction(OpCodes.Ldloc_3), // this *should* be the `xOffset` local
					new CodeInstruction(OpCodes.Add),

					new CodeInstruction(OpCodes.Ldloc_1), // this *should* be the `y` local
					new CodeInstruction(OpCodes.Ldloc, 19), // this *should* be the `levelIndex` local
					new CodeInstruction(OpCodes.Ldloc_2), // this *should* be the `indexWithLuckSkill` local
					new CodeInstruction(OpCodes.Ldloc, 17), // this *should* be the `skillName` local
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(XPDisplay), nameof(SkillsPage_draw_Addition)))
				});
			}

			return instructions;
		}

		public static void SkillsPage_draw_Addition(SpriteBatch b, int x, int y, int levelIndex, int uiSkillIndex, string? spaceCoreSkillName)
		{
			int currentLevel = GetUnmodifiedSkillLevel(uiSkillIndex, spaceCoreSkillName);
			if (currentLevel % 10 != levelIndex)
				return;
			int nextLevelXP = GetLevelXP(currentLevel, spaceCoreSkillName);
			int currentLevelXP = currentLevel == 0 ? 0 : GetLevelXP(currentLevel - 1, spaceCoreSkillName);
			int currentXP = GetCurrentXP(uiSkillIndex, spaceCoreSkillName);
			float nextLevelProgress = Math.Clamp(1f * (currentXP - currentLevelXP) / (nextLevelXP - currentLevelXP), 0f, 1f);

			float scale = 4f;
			bool isBigLevel = (levelIndex + 1) % 5 == 0;
			Texture2D barTexture = Game1.mouseCursors;
			Rectangle barTextureRectangle = isBigLevel ? BigObtainedLevelCursorsRectangle : SmallObtainedLevelCursorsRectangle;
			ModConfig.Orientation orientation = isBigLevel ? Instance.Config.BigBarOrientation : Instance.Config.SmallBarOrientation;

			if (currentLevel >= 10 && Instance.IsWalkOfLifeInstalled && WalkOfLifeBridge.IsPrestigeEnabled())
				(barTexture, barTextureRectangle) = isBigLevel ? WalkOfLifeBridge.GetExtendedBigBar() : WalkOfLifeBridge.GetExtendedSmallBar();

			switch (orientation)
			{
				case ModConfig.Orientation.Horizontal:
					int rectangleWidthPixels = (int)(barTextureRectangle.Height * nextLevelProgress);
					b.Draw(
						barTexture,
						new Vector2(x + levelIndex * 36, y - 4 + uiSkillIndex * 56),
						new Rectangle(
							barTextureRectangle.Left,
							barTextureRectangle.Top,
							rectangleWidthPixels,
							barTextureRectangle.Height
						),
						Color.White * Instance.Config.Alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.87f
					);
					break;
				case ModConfig.Orientation.Vertical:
					int rectangleHeightPixels = (int)(barTextureRectangle.Height * nextLevelProgress);
					b.Draw(
						barTexture,
						new Vector2(x + levelIndex * 36, y - 4 + uiSkillIndex * 56 + (barTextureRectangle.Height - rectangleHeightPixels) * scale),
						new Rectangle(
							barTextureRectangle.Left,
							barTextureRectangle.Top + barTextureRectangle.Height - rectangleHeightPixels,
							barTextureRectangle.Width,
							rectangleHeightPixels
						),
						Color.White * Instance.Config.Alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.87f
					);
					break;
			}
		}

		private static int GetUnmodifiedSkillLevel(int uiSkillIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is null)
				return Game1.player.GetUnmodifiedSkillLevel(OrderedSkillIndexes[uiSkillIndex]);
			else
				return SpaceCoreBridge.GetUnmodifiedSkillLevel(spaceCoreSkillName);
		}

		private static int GetLevelXP(int levelIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is null)
			{
				if (levelIndex >= 10)
				{
					if (Instance.IsWalkOfLifeInstalled && WalkOfLifeBridge.IsPrestigeEnabled())
					{
						int levelXP = Instance.XPValues.Last();
						int requiredXPPerExtendedLevel = WalkOfLifeBridge.GetRequiredXPPerExtendedLevel();
						for (int i = 10; i <= levelIndex; i++)
							levelXP += requiredXPPerExtendedLevel;
						return levelXP;
					}
					else
					{
						levelIndex %= 10;
					}
				}
				return Instance.XPValues[levelIndex];
			}
			else
			{
				return SpaceCoreBridge.GetLevelXP(levelIndex, spaceCoreSkillName);
			}
		}

		private static int GetCurrentXP(int uiSkillIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is null)
				return Game1.player.experiencePoints[OrderedSkillIndexes[uiSkillIndex]];
			else
				return SpaceCoreBridge.GetCurrentXP(spaceCoreSkillName);
		}
	}
}
