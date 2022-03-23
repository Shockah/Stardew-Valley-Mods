using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shockah.CommonModCode;
using Shockah.CommonModCode.IL;
using Shockah.CommonModCode.Stardew;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Events;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Shockah.Hibernation
{
	public class Hibernation: Mod
	{
		internal static Hibernation Instance { get; private set; } = null!;
		internal ModConfig Config { get; private set; } = null!;

		private bool TouchSleepActionInProgress = false;

		private int NightsToSleep = 0;
		private Func<bool>? EarlyWakeUpTrigger = null;
		private bool PostponingHibernation = false;
		private bool InstantPostponedHibernation = true;
		private bool AnyEventTriggered = false;

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			Config = helper.ReadConfig<ModConfig>();
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.DayStarted += OnDayStarted;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			var harmony = new Harmony(ModManifest.UniqueID);

			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(GameLocation), nameof(GameLocation.performTouchAction)),
				prefix: new HarmonyMethod(typeof(Hibernation), nameof(GameLocation_performTouchAction_Prefix)),
				postfix: new HarmonyMethod(typeof(Hibernation), nameof(GameLocation_performTouchAction_Postfix))
			);

			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(GameLocation), nameof(GameLocation.createYesNoResponses)),
				postfix: new HarmonyMethod(typeof(Hibernation), nameof(GameLocation_createYesNoResponses_Postfix))
			);

			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(GameLocation), nameof(GameLocation.answerDialogueAction)),
				postfix: new HarmonyMethod(typeof(Hibernation), nameof(GameLocation_answerDialogueAction_Postfix))
			);

			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Utility), nameof(Utility.getDateStringFor)),
				transpiler: new HarmonyMethod(typeof(Hibernation), nameof(Utility_getDateStringFor_Transpiler))
			);

			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Game1), "_newDayAfterFade"),
				postfix: new HarmonyMethod(typeof(Hibernation), nameof(Game1__newDayAfterFade_Postfix))
			);

			harmony.TryPatchVirtual(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Game1), "_draw"),
				postfix: new HarmonyMethod(typeof(Hibernation), nameof(Game1__draw_Postfix))
			);

			harmony.TryPatchVirtual(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(ModHooks), nameof(ModHooks.OnGame1_ShowEndOfNightStuff)),
				postfix: new HarmonyMethod(typeof(Hibernation), nameof(ModHooks_OnGame1_ShowEndOfNightStuff_Postfix))
			);

			harmony.TryPatchVirtual(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(FarmEvent), nameof(FarmEvent.setUp)),
				postfix: new HarmonyMethod(typeof(Hibernation), nameof(FarmEvent_setUp_Postfix))
			);
		}

		private void OnDayStarted(object? sender, DayStartedEventArgs e)
		{
			NightsToSleep = Math.Max(NightsToSleep - 1, 0);
			if (NightsToSleep == 0)
				EarlyWakeUpTrigger = null;
		}

		private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
		{
			if (NightsToSleep > 0 && PostponingHibernation)
			{
				if (Game1.dialogueUp || !string.IsNullOrEmpty(Game1.messageAfterPause))
					InstantPostponedHibernation = false;
				if (Context.IsPlayerFree && !ShouldPostponeHibernation())
				{
					PostponingHibernation = false;
					ContinueHibernation();
					InstantPostponedHibernation = true;
				}
			}
		}

		private void ContinueHibernation()
		{
			var doSleepMethod = AccessTools.Method(typeof(GameLocation), "doSleep");
			doSleepMethod!.Invoke(Game1.player.currentLocation, null);

			if (InstantPostponedHibernation)
			{
				//Game1.player.freezePause = 100;
				//Game1.pauseTime = 100f;
				Game1.fadeToBlackAlpha = 1f;
			}
		}

		private static bool ShouldPostponeHibernation()
			=> Game1.activeClickableMenu is not null
			|| Game1.endOfNightMenus.Count != 0
			|| Game1.dialogueUp
			|| Game1.eventUp
			|| Game1.farmEvent is not null
			|| !string.IsNullOrEmpty(Game1.messageAfterPause);

		private void Hibernate(HibernateLength length, Func<bool>? earlyTrigger = null)
		{
			NightsToSleep = length.GetDayCount();
			EarlyWakeUpTrigger = earlyTrigger;
			InstantPostponedHibernation = false;
			Game1.currentLocation.answerDialogueAction("Sleep_Yes", Array.Empty<string>());
		}

		private void HibernateUntilDate(WorldDate date, Func<bool>? earlyTrigger = null)
		{
			if (date.TotalDays <= Game1.Date.TotalDays)
				return;
			Hibernate(new(date.TotalDays - Game1.Date.TotalDays, HibernateLengthUnit.Nights), earlyTrigger);
		}

		private WorldDate? GetNextBirthdayDate()
		{
			var currentDate = Game1.Date;
			var maxDays = currentDate.TotalDays + WorldDateExt.DaysPerSeason * 4; // safety net
			while (currentDate.TotalDays < maxDays)
			{
				currentDate = currentDate.GetByAddingDays(1);
				if (Utility.getTodaysBirthdayNPC(currentDate.Season, currentDate.DayOfMonth) is not null)
					return currentDate;
			}
			return null;
		}

		private WorldDate? GetNextFestivalDate()
		{
			var currentDate = Game1.Date;
			var maxDays = currentDate.TotalDays + WorldDateExt.DaysPerSeason * 4 * 2; // safety net
			while (currentDate.TotalDays < maxDays)
			{
				currentDate = currentDate.GetByAddingDays(1);
				if (Utility.isFestivalDay(currentDate.DayOfMonth, currentDate.Season))
					return currentDate;
			}
			return null;
		}

		private static void GameLocation_performTouchAction_Prefix(string fullActionString)
		{
			if (fullActionString.Split(' ')[0] != "Sleep")
				return;
			Instance.TouchSleepActionInProgress = true;
		}

		private static void GameLocation_performTouchAction_Postfix()
		{
			Instance.TouchSleepActionInProgress = false;
		}

		private static void GameLocation_createYesNoResponses_Postfix(ref Response[] __result)
		{
			if (!Instance.TouchSleepActionInProgress)
				return;

			IList<Response> responses = new List<Response>
			{
				__result[0],
				new Response("HibernateFor", Instance.Helper.Translation.Get("sleepDialogue.response.hibernateFor")),
				new Response("HibernateUntil", Instance.Helper.Translation.Get("sleepDialogue.response.hibernateUntil")),
				__result[1]
			};
			__result = responses.ToArray();
		}

		private static void GameLocation_answerDialogueAction_Postfix(GameLocation __instance, ref bool __result, string questionAndAnswer)
		{
			switch (questionAndAnswer)
			{
				case "Sleep_HibernateFor":
					{
						__result = true;
						var responses = Instance.Config.ParsedLengthOptions
							.Select(l => new Response($"{l}", l.GetLocalized()))
							.ToList();
						responses.Add(new Response("Cancel", Instance.Helper.Translation.Get("sleepDialogue.response.cancel")).SetHotKey(Keys.Escape));
						__instance.createQuestionDialogue(
							Instance.Helper.Translation.Get("sleepDialogue.response.hibernateFor"),
							responses.ToArray(),
							(farmer, answer) =>
							{
								if (answer == "Cancel")
									return;
								var length = HibernateLength.ParseOrNull(answer);
								if (length is not null)
									Instance.Hibernate(length.Value);
							}
						);
					}
					break;
				case "Sleep_HibernateUntil":
					{
						__result = true;
						IList<Response> responses = new List<Response>();
						{
							var nextBirthdayDate = Instance.GetNextBirthdayDate();
							if (nextBirthdayDate is not null)
							{
								responses.Add(new Response(
									"Birthday",
									Instance.Helper.Translation.Get(
										"sleepDialogue.response.hibernateUntil.nextBirthday",
										new { NpcName = Utility.getTodaysBirthdayNPC(nextBirthdayDate.Season, nextBirthdayDate.DayOfMonth)!.displayName }
									)
								));
							}
						}
						{
							var nextFestivalDate = Instance.GetNextFestivalDate();
							if (nextFestivalDate is not null)
							{
								var festivalDates = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\FestivalDates");
								responses.Add(new Response(
									"Festival",
									Instance.Helper.Translation.Get(
										"sleepDialogue.response.hibernateUntil.nextFestival",
										new { FestivalName = festivalDates[$"{nextFestivalDate.Season}{nextFestivalDate.DayOfMonth}"] }
									)
								));
							}
						}
						{
							var season = (Game1.Date.SeasonIndex + 1) % 4;
							responses.Add(new Response(
								"NextSeason",
								Instance.Helper.Translation.Get(
									"sleepDialogue.response.hibernateUntil.nextSeason",
									new { SeasonName = Utility.getSeasonNameFromNumber(season) }
								)
							));
						}
						responses.Add(new Response(
							"NextEvent",
							Instance.Helper.Translation.Get("sleepDialogue.response.hibernateUntil.nextEvent")
						));
						responses.Add(new Response("Cancel", Instance.Helper.Translation.Get("sleepDialogue.response.cancel")).SetHotKey(Keys.Escape));

						__instance.createQuestionDialogue(
							Instance.Helper.Translation.Get("sleepDialogue.response.hibernateUntil"),
							responses.ToArray(),
							(farmer, answer) =>
							{
								switch (answer)
								{
									case "Birthday":
										{
											var nextFestivalDate = Instance.GetNextBirthdayDate();
											if (nextFestivalDate is not null)
												Instance.HibernateUntilDate(nextFestivalDate);
										}
										break;
									case "Festival":
										{
											var nextFestivalDate = Instance.GetNextFestivalDate();
											if (nextFestivalDate is not null)
												Instance.HibernateUntilDate(nextFestivalDate);
										}
										break;
									case "NextSeason":
										{
											var season = Game1.Date.SeasonIndex + 1;
											var year = Game1.Date.Year;
											while (season >= 4)
											{
												season -= 4;
												year++;
											}
											var nextSeasonDate = WorldDateExt.New(year, season, 1);
											Instance.HibernateUntilDate(nextSeasonDate);
										}
										break;
									case "NextEvent":
										{
											static bool EarlyTrigger()
											{
												if (Instance.AnyEventTriggered)
												{
													Instance.AnyEventTriggered = false;
													return true;
												}
												return false;
											}

											var limitDate = Game1.Date.GetByAddingDays(WorldDateExt.DaysPerSeason * 4);
											Instance.HibernateUntilDate(limitDate, EarlyTrigger);
										}
										break;
									case "Cancel":
										break;
								}
							}
						);
					}
					break;
			}
		}

		[HarmonyPriority(Priority.Last)]
		private static IEnumerable<CodeInstruction> Utility_getDateStringFor_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();

			// IL to find:
			// ldarg.0
			// <any ldc.i4> <any value>
			var worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdarg(0),
				i => i.IsLdcI4(),
				i => i.opcode == OpCodes.Add
			});
			if (worker is null)
				return instructions;

			WorldDateExt.DaysPerSeason = worker[1].GetLdcI4Value()!.Value;
			return instructions;
		}

		private static void Game1__newDayAfterFade_Postfix()
		{
			Instance.PostponingHibernation = true;
		}

		[HarmonyPriority(Priority.High)]
		private static void Game1__draw_Postfix()
		{
			if (!(Instance.NightsToSleep > 0 && (Game1.activeClickableMenu is SaveGameMenu || (Game1.endOfNightMenus.Count == 0 && Game1.activeClickableMenu is null && Game1.fadeToBlackAlpha > 0f))))
				return;
			if (Game1.input.GetKeyboardState().IsKeyDown(Keys.Escape) && Instance.NightsToSleep > 2)
				Instance.NightsToSleep = 2;

			float spacing = 4f;
			IList<(string, Vector2, float)> lines = new List<(string, Vector2, float)>();
			float totalHeight = 0f;

			void AddLine(string text, float scale)
			{
				if (lines.Count != 0)
					totalHeight += spacing;
				var measure = Game1.dialogueFont.MeasureString(text);
				lines.Add((text, measure, scale));
				totalHeight += measure.Y * scale;
			}

			AddLine(Utility.getDateStringFor(Game1.Date.DayOfMonth, Game1.Date.SeasonIndex, Game1.Date.Year), 1.5f);
			AddLine(
				Instance.NightsToSleep > 1
					? (
						Instance.EarlyWakeUpTrigger is null
							? Instance.Helper.Translation.Get("hibernation.nightsLeft", new { Value = Instance.NightsToSleep })
							: Instance.Helper.Translation.Get("hibernation.nightsLeft.upTo", new { Value = Instance.NightsToSleep })
					) : Instance.Helper.Translation.Get("hibernation.wakingUp"),
				1f
			);
			if (Instance.NightsToSleep > 1)
			{
				AddLine(" ", 0.5f);
				AddLine(Instance.Helper.Translation.Get("hibernation.escapeToWakeUp"), 0.75f);
			}

			var alpha = Game1.activeClickableMenu is SaveGameMenu ? 1f : Math.Clamp(Game1.fadeToBlackAlpha, 0f, 1f);
			Game1.PushUIMode();
			Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

			var viewport = Game1.viewport.Size;

			if (Instance.NightsToSleep > 1)
				Game1.spriteBatch.Draw(Game1.mouseCursors, new Rectangle(0, 0, viewport.Width, viewport.Height), new Rectangle(128, 1884, 4, 4), Color.Black * alpha);

			var yy = viewport.Height * 0.5f - totalHeight * 0.5f;
			for (int i = 0; i < lines.Count; i++)
			{
				var (line, measure, scale) = lines[i];
				Game1.spriteBatch.DrawString(
					Game1.dialogueFont,
					line,
					new(viewport.Width * 0.5f - measure.X * 0.5f * scale, yy),
					Color.White * alpha,
					0f,
					Vector2.Zero,
					scale,
					SpriteEffects.None,
					0f
				);
				yy += measure.Y * scale + spacing;
			}

			Game1.spriteBatch.End();
			Game1.PopUIMode();
		}

		private static void ModHooks_OnGame1_ShowEndOfNightStuff_Postfix()
		{
			if (Instance.EarlyWakeUpTrigger?.Invoke() == true)
				Instance.NightsToSleep = 1;
			if (Instance.NightsToSleep > 1)
			{
				var saveMenu = Game1.activeClickableMenu as SaveGameMenu ?? Game1.endOfNightMenus.OfType<SaveGameMenu>().LastOrDefault();
				if (saveMenu is not null)
					saveMenu.quit = true;
			}
		}

		private static void FarmEvent_setUp_Postfix(FarmEvent __instance, bool __result)
		{
			if (Instance.NightsToSleep <= 0 || __result)
				return;
			Instance.AnyEventTriggered = true;
		}
	}
}
