using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode;
using SpaceCore;
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
		private static readonly Rectangle SmallObtainedLevelCursorsRectangle = new(137, 338, 8, 9);
		private static readonly Rectangle BigObtainedLevelCursorsRectangle = new(159, 338, 14, 9);
		private static readonly int[] OrderedSkillIndexes = new[] { 0, 3, 2, 1, 4, 5 };
		private static readonly string SpaceCoreNewSkillsPageQualifiedName = "SpaceCore.Interface.NewSkillsPage, SpaceCore";

		private static XPDisplay Instance = null!;

		private int[] XPValues = null!;

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
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

				if (Helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
				{
					harmony.Patch(
						original: AccessTools.Method(AccessTools.TypeByName(SpaceCoreNewSkillsPageQualifiedName), "draw", new Type[] { typeof(SpriteBatch) }),
						transpiler: new HarmonyMethod(typeof(XPDisplay), nameof(SpaceCore_NewSkillsPage_draw_Transpiler))
					);
				}
			}
			catch (Exception ex)
			{
				Monitor.Log($"Could not patch methods - XP View probably won't work.\nReason: {ex}", LogLevel.Error);
			}
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
			if (GetUnmodifiedSkillLevel(uiSkillIndex, spaceCoreSkillName) != levelIndex)
				return;
			int nextLevelXP = GetLevelXP(levelIndex, spaceCoreSkillName);
			int currentLevelXP = levelIndex == 0 ? 0 : GetLevelXP(levelIndex - 1, spaceCoreSkillName);
			int currentXP = GetCurrentXP(uiSkillIndex, spaceCoreSkillName);
			float nextLevelProgress = 1f * (currentXP - currentLevelXP) / (nextLevelXP - currentLevelXP);

			float scale = 4f;
			if ((levelIndex + 1) % 5 == 0) // "big" levels (5 and 10)
			{
				int rectangleWidthPixels = (int)(BigObtainedLevelCursorsRectangle.Height * nextLevelProgress);
				b.Draw(
					Game1.mouseCursors,
					new Vector2(x + levelIndex * 36, y - 4 + uiSkillIndex * 56),
					new Rectangle(
						BigObtainedLevelCursorsRectangle.Left,
						BigObtainedLevelCursorsRectangle.Top,
						rectangleWidthPixels,
						BigObtainedLevelCursorsRectangle.Height
					),
					Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.87f
				);
			}
			else
			{
				int rectangleHeightPixels = (int)(SmallObtainedLevelCursorsRectangle.Height * nextLevelProgress);
				b.Draw(
					Game1.mouseCursors,
					new Vector2(x + levelIndex * 36, y - 4 + uiSkillIndex * 56 + (SmallObtainedLevelCursorsRectangle.Height - rectangleHeightPixels) * scale),
					new Rectangle(
						SmallObtainedLevelCursorsRectangle.Left,
						SmallObtainedLevelCursorsRectangle.Top + SmallObtainedLevelCursorsRectangle.Height - rectangleHeightPixels,
						SmallObtainedLevelCursorsRectangle.Width,
						rectangleHeightPixels
					),
					Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.87f
				);
			}
		}

		private static int GetUnmodifiedSkillLevel(int uiSkillIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is null)
				return Game1.player.GetUnmodifiedSkillLevel(OrderedSkillIndexes[uiSkillIndex]);
			else
				return GetSpaceCoreUnmodifiedSkillLevel(spaceCoreSkillName);
		}

		private static int GetLevelXP(int levelIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is null)
				return Instance.XPValues[levelIndex];
			else
				return GetSpaceCoreLevelXP(levelIndex, spaceCoreSkillName);
		}

		private static int GetCurrentXP(int uiSkillIndex, string? spaceCoreSkillName)
		{
			if (spaceCoreSkillName is null)
				return Game1.player.experiencePoints[OrderedSkillIndexes[uiSkillIndex]];
			else
				return GetSpaceCoreCurrentXP(spaceCoreSkillName);
		}

		private static int GetSpaceCoreUnmodifiedSkillLevel(string spaceCoreSkillName)
		{
			var skill = Skills.GetSkill(spaceCoreSkillName);
			return Game1.player.GetCustomSkillLevel(skill);
		}

		private static int GetSpaceCoreLevelXP(int levelIndex, string spaceCoreSkillName)
		{
			var skill = Skills.GetSkill(spaceCoreSkillName);
			return skill.ExperienceCurve[levelIndex];
		}

		private static int GetSpaceCoreCurrentXP(string spaceCoreSkillName)
		{
			var skill = Skills.GetSkill(spaceCoreSkillName);
			return Game1.player.GetCustomSkillExperience(skill);
		}
	}
}
