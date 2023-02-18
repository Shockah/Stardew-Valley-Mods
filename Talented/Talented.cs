using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew.Skill;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Shockah.Talented
{
	public class Talented : BaseMod
	{
		private static readonly int[] OrderedSkillIndexes = new[] { 0, 3, 2, 1, 4, 5 };
		private static readonly string SpaceCoreNewSkillsPageQualifiedName = "SpaceCore.Interface.NewSkillsPage, SpaceCore";

		private static readonly Rectangle ContinueArrowRectangle = new(232, 346, 9, 9);
		private const int ContinueArrowFrames = 6;

		private static readonly Rectangle SparkleRectangle = new(666, 1851, 8, 8);
		private static readonly int[] SparkleFrames = new int[] { 0, 1, 2, 3, 2, 1 };

		internal static Talented Instance = null!;

		private static readonly PerScreen<Dictionary<(int uiSkillIndex, string? spaceCoreSkillName), (Vector2?, Vector2?)>> SkillBarCorners = new(() => new());

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.Display.MenuChanged += OnMenuChanged;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			var harmony = new Harmony(ModManifest.UniqueID);

			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(SkillsPage), nameof(SkillsPage.draw), new Type[] { typeof(SpriteBatch) }),
				transpiler: new HarmonyMethod(typeof(Talented), nameof(SkillsPage_draw_Transpiler))
			);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(SkillsPage), nameof(SkillsPage.receiveLeftClick)),
				postfix: new HarmonyMethod(typeof(Talented), nameof(AnySkillsPage_receiveLeftClick_Postfix))
			);

			if (Helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
			{
				harmony.TryPatch(
					monitor: Monitor,
					original: () => AccessTools.Method(AccessTools.TypeByName(SpaceCoreNewSkillsPageQualifiedName), "draw", new Type[] { typeof(SpriteBatch) }),
					transpiler: new HarmonyMethod(typeof(Talented), nameof(SpaceCore_NewSkillsPage_draw_Transpiler))
				);
				harmony.TryPatch(
					monitor: Monitor,
					original: () => AccessTools.Method(AccessTools.TypeByName(SpaceCoreNewSkillsPageQualifiedName), "receiveLeftClick"),
					postfix: new HarmonyMethod(typeof(Talented), nameof(AnySkillsPage_receiveLeftClick_Postfix))
				);
			}
		}

		private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
		{
			if (e.NewMenu is GameMenu)
				SkillBarCorners.Value.Clear();
		}

		private bool HasTalentDefinitions(ISkill skill)
			=> skill is VanillaSkill vanilla && vanilla.SkillIndex is Farmer.fishingSkill or Farmer.foragingSkill;

		private bool HasUnspentTalentPoints(ISkill skill)
			=> skill is VanillaSkill vanilla && vanilla.SkillIndex is Farmer.fishingSkill;

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
							.ExtractLabels(out var labels)
							.Insert(
								SequenceMatcherPastBoundsDirection.Before, true,

								new CodeInstruction(OpCodes.Ldloc_0).WithLabels(labels), // this *should* be the `x` local
								new CodeInstruction(OpCodes.Ldloc_2), // this *should* be the `addedX` local
								new CodeInstruction(OpCodes.Add),

								new CodeInstruction(OpCodes.Ldloc_1), // this *should* be the `y` local
								new CodeInstruction(OpCodes.Ldloc_3), // this *should* be the `i` local - the currently drawn level index (0-9)
								new CodeInstruction(OpCodes.Ldloc, 4), // this *should* be the `j` local - the skill index
								new CodeInstruction(OpCodes.Ldnull), // no skill name, it's a built-in one
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Talented), nameof(AnySkillsPage_draw_RecordSize)))
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
							.ExtractLabels(out var labels)
							.Insert(
								SequenceMatcherPastBoundsDirection.Before, true,

								new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels), // `SpriteBatch`
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Talented), nameof(AnySkillsPage_draw_FinishDrawing)))
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
									.ExtractLabels(out var labels)
									.Insert(
										SequenceMatcherPastBoundsDirection.Before, true,

										new CodeInstruction(OpCodes.Ldloc_0).WithLabels(labels), // this *should* be the `x` local
										new CodeInstruction(OpCodes.Ldloc_3), // this *should* be the `xOffset` local
										new CodeInstruction(OpCodes.Add),

										new CodeInstruction(OpCodes.Ldloc_1), // this *should* be the `y` local
										new CodeInstruction(OpCodes.Ldloc, 8), // this *should* be the `levelIndex` local
										new CodeInstruction(OpCodes.Ldloc, 9), // this *should* be the `skillIndex` local
										new CodeInstruction(OpCodes.Ldnull), // no skill name, it's a built-in one
										new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Talented), nameof(AnySkillsPage_draw_RecordSize)))
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
									.ExtractLabels(out var labels)
									.Insert(
										SequenceMatcherPastBoundsDirection.Before, true,

										new CodeInstruction(OpCodes.Ldloc_0).WithLabels(labels), // this *should* be the `x` local
										new CodeInstruction(OpCodes.Ldloc_3), // this *should* be the `xOffset` local
										new CodeInstruction(OpCodes.Add),

										new CodeInstruction(OpCodes.Ldloc_1), // this *should* be the `y` local
										new CodeInstruction(OpCodes.Ldloc, 19), // this *should* be the `levelIndex` local
										new CodeInstruction(OpCodes.Ldloc_2), // this *should* be the `indexWithLuckSkill` local
										new CodeInstruction(OpCodes.Ldloc, 17), // this *should* be the `skillName` local
										new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Talented), nameof(AnySkillsPage_draw_RecordSize)))
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
							.ExtractLabels(out var labels)
							.Insert(
								SequenceMatcherPastBoundsDirection.Before, true,

								new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels), // `SpriteBatch`
								new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Talented), nameof(AnySkillsPage_draw_FinishDrawing)))
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

		public static void AnySkillsPage_draw_RecordSize(int x, int y, int levelIndex, int uiSkillIndex, string? spaceCoreSkillName)
		{
			if (levelIndex is 0 or 9)
			{
				int barTextureWidth = (levelIndex == 9) ? 13 : 7;
				int barTextureHeight = 9;
				float scale = 4f;
				Vector2 topLeft = new(x + levelIndex * 36, y - 4 + uiSkillIndex * 56);
				Vector2 bottomRight = topLeft + new Vector2(barTextureWidth, barTextureHeight) * scale;

				var key = (uiSkillIndex, spaceCoreSkillName);
				if (!SkillBarCorners.Value.ContainsKey(key))
					SkillBarCorners.Value[key] = (null, null);
				if (levelIndex == 0)
					SkillBarCorners.Value[key] = (topLeft, SkillBarCorners.Value[key].Item2);
				else if (levelIndex == 9)
					SkillBarCorners.Value[key] = (SkillBarCorners.Value[key].Item1, bottomRight);
			}
		}

		public static void AnySkillsPage_draw_FinishDrawing(SpriteBatch b)
		{
			foreach (var ((uiSkillIndex, spaceCoreSkillName), (topLeft, bottomRight)) in SkillBarCorners.Value)
			{
				if (topLeft is null || bottomRight is null)
					continue;
				int skillIndex = OrderedSkillIndexes.Length > uiSkillIndex ? OrderedSkillIndexes[uiSkillIndex] : uiSkillIndex;
				ISkill skill = SkillExt.GetSkill(skillIndex, spaceCoreSkillName);

				if (!Instance.HasTalentDefinitions(skill))
					continue;

				Vector2 hoverTopLeft = new(topLeft.Value.X - 184, topLeft.Value.Y - 8);
				Vector2 hoverBottomRight = new(bottomRight.Value.X + 64, bottomRight.Value.Y + 8);

				Vector2 iconLeftPosition = new(topLeft.Value.X - 208, (topLeft.Value.Y + bottomRight.Value.Y) / 2f + 4f);
				Vector2 iconRightPosition = new(bottomRight.Value.X + 72, (topLeft.Value.Y + bottomRight.Value.Y) / 2f + 4f);
				if (Instance.HasUnspentTalentPoints(skill))
				{
					b.Draw(
						Game1.mouseCursors,
						iconLeftPosition + new Vector2(192, 0),
						new Rectangle(
							SparkleRectangle.X + SparkleFrames[(int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250) % SparkleFrames.Length] * SparkleRectangle.Width,
							SparkleRectangle.Y,
							SparkleRectangle.Width,
							SparkleRectangle.Height
						),
						Color.White,
						0f,
						new(SparkleRectangle.Width / 2f, SparkleRectangle.Height / 2f),
						4f,
						SpriteEffects.None,
						0f
					);
				}

				if (Game1.getMouseX() >= hoverTopLeft.X && Game1.getMouseY() >= hoverTopLeft.Y && Game1.getMouseX() < hoverBottomRight.X && Game1.getMouseY() < hoverBottomRight.Y)
				{
					b.Draw(
						Game1.mouseCursors,
						iconLeftPosition,
						new Rectangle(
							ContinueArrowRectangle.X + ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100) % ContinueArrowFrames) * ContinueArrowRectangle.Width,
							ContinueArrowRectangle.Y,
							ContinueArrowRectangle.Width,
							ContinueArrowRectangle.Height
						),
						Color.White,
						(float)-Math.PI / 2f,
						new(ContinueArrowRectangle.Width / 2f, ContinueArrowRectangle.Height / 2f),
						4f,
						SpriteEffects.None,
						0f
					);
					b.Draw(
						Game1.mouseCursors,
						iconRightPosition,
						new Rectangle(
							ContinueArrowRectangle.X + ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100) % ContinueArrowFrames) * ContinueArrowRectangle.Width,
							ContinueArrowRectangle.Y,
							ContinueArrowRectangle.Width,
							ContinueArrowRectangle.Height
						),
						Color.White,
						(float)Math.PI / 2f,
						new(ContinueArrowRectangle.Width / 2f, ContinueArrowRectangle.Height / 2f),
						4f,
						SpriteEffects.None,
						0f
					);
				}
			}
		}

		public static void AnySkillsPage_receiveLeftClick_Postfix(IClickableMenu __instance, int x, int y)
		{
			if (Game1.activeClickableMenu is not GameMenu menu || menu.GetCurrentPage() != __instance)
				return;

			foreach (var ((uiSkillIndex, spaceCoreSkillName), (topLeft, bottomRight)) in SkillBarCorners.Value)
			{
				if (topLeft is null || bottomRight is null)
					continue;
				int skillIndex = OrderedSkillIndexes.Length > uiSkillIndex ? OrderedSkillIndexes[uiSkillIndex] : uiSkillIndex;
				ISkill skill = SkillExt.GetSkill(skillIndex, spaceCoreSkillName);

				if (!Instance.HasTalentDefinitions(skill))
					continue;

				Vector2 hoverTopLeft = new(topLeft.Value.X - 184, topLeft.Value.Y - 8);
				Vector2 hoverBottomRight = new(bottomRight.Value.X + 64, bottomRight.Value.Y + 8);

				if (!(x >= hoverTopLeft.X && y >= hoverTopLeft.Y && x < hoverBottomRight.X && y < hoverBottomRight.Y))
					continue;

				// TODO: open talents menu
				Instance.Monitor.Log($"Received click for skill {skill}.", LogLevel.Info);
			}
		}
	}
}