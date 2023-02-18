using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Kokoro;
using Shockah.Talented.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.Talented.Patches
{
	internal static class GameMenuPatches
	{
		private const int TalentsTabID = 1554501; // {NexusID}01

		private static readonly Rectangle TalentsTabIconRectangle = new(202, 374, 8, 8);

		private static Talented Instance
			=> Talented.Instance;

		private static IMonitor Monitor
			=> Instance.Monitor;

		private static IManifest ModManifest
			=> Instance.ModManifest;

		internal static void Apply(Harmony harmony)
		{
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Constructor(typeof(GameMenu), new Type[] { typeof(bool) }),
				postfix: new HarmonyMethod(typeof(GameMenuPatches), nameof(GameMenu_constructor_Postfix))
			);

			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(GameMenu), nameof(GameMenu.draw), new Type[] { typeof(SpriteBatch) }),
				transpiler: new HarmonyMethod(typeof(GameMenuPatches), nameof(GameMenu_draw_Transpiler))
			);

			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(GameMenu), nameof(GameMenu.getTabNumberFromName)),
				postfix: new HarmonyMethod(typeof(GameMenuPatches), nameof(GameMenu_getTabNumberFromName_Postfix))
			);
		}

		private static void GameMenu_constructor_Postfix(GameMenu __instance)
		{
			int tabOffset = __instance.tabs[1].bounds.X - __instance.tabs[0].bounds.X;

			// making space for a new tab
			for (int i = 2; i < __instance.tabs.Count; i++)
				__instance.tabs[i].bounds.X += tabOffset;

			// TODO: i18n
			__instance.tabs.Add(new ClickableComponent(new Rectangle(__instance.tabs[1].bounds.X + tabOffset, __instance.tabs[1].bounds.Y, __instance.tabs[1].bounds.Width, __instance.tabs[1].bounds.Height), $"{ModManifest.UniqueID}.talents", "Talents")
			{
				myID = TalentsTabID,
				downNeighborID = 0,
				leftNeighborID = __instance.tabs[1].myID,
				rightNeighborID = __instance.tabs[2].myID,
				tryDefaultIfNoDownNeighborExists = true,
				fullyImmutable = true
			});
			__instance.pages.Add(new TalentsPage(__instance.xPositionOnScreen, __instance.yPositionOnScreen, __instance.width, __instance.height));

			// updating neighbor IDs
			__instance.tabs[1].rightNeighborID = TalentsTabID;
			__instance.tabs[2].leftNeighborID = TalentsTabID;
		}

		private static IEnumerable<CodeInstruction> GameMenu_draw_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
		{
			try
			{
				var localVariables = originalMethod.GetMethodBody()!.LocalVariables;
				var load0Match = ILMatches.LdcI4(0);
				var load1Match = ILMatches.LdcI4(1);

				return new SequenceBlockMatcher<CodeInstruction>(instructions)

					// making the default tab icon the blank one
					.Find(
						new ElementMatch<CodeInstruction>($"{{{load0Match} or {load1Match}}}", i => load0Match.Matches(i) || load1Match.Matches(i)),
						ILMatches.Stloc<int>(localVariables),
						ILMatches.Ldloc<ClickableComponent>(localVariables),
						ILMatches.Ldfld(AccessTools.Field(typeof(ClickableComponent), nameof(ClickableComponent.name))),
						ILMatches.Stloc<string>(localVariables)
					)
					.PointerMatcher(SequenceMatcherRelativeElement.First)
					.Replace(new CodeInstruction(OpCodes.Ldc_I4_1))

					// injecting custom tab drawing
					.Find(
						ILMatches.Ldloc<ClickableComponent>(localVariables),
						ILMatches.Ldfld(AccessTools.Field(typeof(ClickableComponent), nameof(ClickableComponent.name))),
						ILMatches.Ldstr("skills"),
						ILMatches.Call("Equals"),
						ILMatches.Brfalse
					)
					.PointerMatcher(SequenceMatcherRelativeElement.First)
					.CreateLdlocInstruction(out var ldlocComponent)
					.Insert(
						SequenceMatcherPastBoundsDirection.Before, true,

						new CodeInstruction(OpCodes.Ldarg_0), // this, GameMenu
						new CodeInstruction(OpCodes.Ldarg_1), // SpriteBatch
						ldlocComponent,
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameMenuPatches), nameof(GameMenu_draw_Transpiler_DrawCustomTab)))
					)

					.AllElements();
			}
			catch (Exception ex)
			{
				Monitor.Log($"Could not patch methods - {ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		public static void GameMenu_draw_Transpiler_DrawCustomTab(GameMenu menu, SpriteBatch b, ClickableComponent component)
		{
			if (component.myID != TalentsTabID)
				return;

			b.Draw(
				Game1.mouseCursors,
				new Vector2(component.bounds.Center.X, component.bounds.Center.Y + 8 + (menu.tabs[menu.currentTab] == component ? 8 : 0)),
				TalentsTabIconRectangle,
				Color.White,
				0f,
				new Vector2(TalentsTabIconRectangle.Width / 2, TalentsTabIconRectangle.Height / 2),
				4f,
				SpriteEffects.None,
				0.1f
			);
		}

		private static void GameMenu_getTabNumberFromName_Postfix(GameMenu __instance, string name, ref int __result)
		{
			if (name == $"{ModManifest.UniqueID}.talents")
				__result = __instance.tabs.FirstIndex(t => t.myID == TalentsTabID)!.Value;
		}
	}
}