﻿using HarmonyLib;
using Nanoray.Shrike.Harmony;
using Nanoray.Shrike;
using Shockah.Kokoro;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using StardewValley.Menus;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;
using Shockah.Kokoro.Stardew;

namespace Shockah.JunimoWarp
{
	internal class ItemGrabMenuPatches
	{
		private const int WarpButtonID = 1567601; // {NexusID}01

		private static JunimoWarp Instance
			=> JunimoWarp.Instance;

		private static readonly ConditionalWeakTable<ItemGrabMenu, ClickableTextureComponent> WarpButtons = new();

		internal static void Apply(Harmony harmony)
		{
			harmony.TryPatch(
				monitor: Instance.Monitor,
				original: () => AccessTools.Method(typeof(ItemGrabMenu), nameof(ItemGrabMenu.RepositionSideButtons)),
				transpiler: new HarmonyMethod(typeof(ItemGrabMenuPatches), nameof(RepositionSideButtons_Transpiler))
			);

			harmony.TryPatch(
				monitor: Instance.Monitor,
				original: () => AccessTools.Method(typeof(ItemGrabMenu), nameof(ItemGrabMenu.draw), new Type[] { typeof(SpriteBatch) }),
				transpiler: new HarmonyMethod(typeof(ItemGrabMenuPatches), nameof(draw_Transpiler))
			);

			harmony.TryPatch(
				monitor: Instance.Monitor,
				original: () => AccessTools.Method(typeof(ItemGrabMenu), nameof(ItemGrabMenu.performHoverAction)),
				postfix: new HarmonyMethod(typeof(ItemGrabMenuPatches), nameof(performHoverAction_Postfix)),
				transpiler: new HarmonyMethod(typeof(ItemGrabMenuPatches), nameof(performHoverAction_Transpiler))
			);

			harmony.TryPatch(
				monitor: Instance.Monitor,
				original: () => AccessTools.Method(typeof(ItemGrabMenu), nameof(ItemGrabMenu.receiveLeftClick)),
				postfix: new HarmonyMethod(typeof(ItemGrabMenuPatches), nameof(receiveLeftClick_Postfix))
			);
		}

		public static ClickableTextureComponent ObtainWarpButton(ItemGrabMenu menu)
		{
			if (!WarpButtons.TryGetValue(menu, out var button))
			{
				// TODO: i18n
				button = new ClickableTextureComponent("", new Rectangle(menu.xPositionOnScreen + menu.width, menu.yPositionOnScreen + menu.height / 3 - 64, 64, 64), "", "Warp", Game1.mouseCursors, new Rectangle(108, 491, 16, 16), 4f)
				{
					myID = WarpButtonID,
					region = 15923
				};
				WarpButtons.AddOrUpdate(menu, button);
			}
			return button;
		}

		private static IEnumerable<CodeInstruction> RepositionSideButtons_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.AsAnchorable<CodeInstruction, Guid, Guid, SequencePointerMatcher<CodeInstruction>, SequenceBlockMatcher<CodeInstruction>>()
					.Find(
						ILMatches.Newobj(AccessTools.DeclaredConstructor(typeof(List<ClickableComponent>), Array.Empty<Type>())),
						ILMatches.Stloc<List<ClickableComponent>>(originalMethod.GetMethodBody()!.LocalVariables).WithAutoAnchor(out Guid sideButtonsStlocPointer)
					)
					.MoveToPointerAnchor(sideButtonsStlocPointer)
					.CreateLdlocInstruction(out var ldlocSideButtons)
					.Insert(
						SequenceMatcherPastBoundsDirection.After, true,

						ldlocSideButtons,
						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemGrabMenuPatches), nameof(ObtainWarpButton))),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<ClickableComponent>), nameof(List<ClickableComponent>.Add)))
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Instance.Monitor.Log($"Could not patch methods - {Instance.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static IEnumerable<CodeInstruction> draw_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.Find(
						ILMatches.Ldarg(0),
						ILMatches.Ldfld(AccessTools.Field(typeof(MenuWithInventory), nameof(MenuWithInventory.hoverText))),
						ILMatches.Brfalse
					)
					.PointerMatcher(SequenceMatcherRelativeElement.First)
					.ExtractLabels(out var labels)
					.Insert(
						SequenceMatcherPastBoundsDirection.Before, true,

						new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemGrabMenuPatches), nameof(ObtainWarpButton))),
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ClickableTextureComponent), nameof(ClickableTextureComponent.draw), new Type[] { typeof(SpriteBatch) }))
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Instance.Monitor.Log($"Could not patch methods - {Instance.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static IEnumerable<CodeInstruction> performHoverAction_Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			try
			{
				return new SequenceBlockMatcher<CodeInstruction>(instructions)
					.Find(
						ILMatches.Ldarg(0),
						ILMatches.Ldarg(1),
						ILMatches.Ldarg(2),
						ILMatches.Call("performHoverAction")
					)
					.Insert(
						SequenceMatcherPastBoundsDirection.After, true,

						new CodeInstruction(OpCodes.Ldarg_0),
						new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemGrabMenuPatches), nameof(ObtainWarpButton))),
						new CodeInstruction(OpCodes.Ldarg_1),
						new CodeInstruction(OpCodes.Ldarg_2),
						new CodeInstruction(OpCodes.Ldc_R4, 0.25f),
						new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ClickableTextureComponent), nameof(ClickableTextureComponent.tryHover)))
					)
					.AllElements();
			}
			catch (Exception ex)
			{
				Instance.Monitor.Log($"Could not patch methods - {Instance.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
				return instructions;
			}
		}

		private static void performHoverAction_Postfix(ItemGrabMenu __instance, int x, int y)
		{
			var button = ObtainWarpButton(__instance);
			button.tryHover(x, y);
			if (button.containsPoint(x, y))
				__instance.hoverText = button.hoverText;
		}

		private static void receiveLeftClick_Postfix(ItemGrabMenu __instance, int x, int y)
		{
			var button = ObtainWarpButton(__instance);
			if (button.containsPoint(x, y))
			{
				if (__instance.context is not Chest chest)
					return;

				Game1.exitActiveMenu();
				Instance.RequestNextWarp(Game1.player.currentLocation, new((int)chest.TileLocation.X, (int)chest.TileLocation.Y), (warpLocation, warpPoint) =>
				{
					JunimoWarp.AnimatePlayerWarp(warpLocation, warpPoint);
				});
			}
		}
	}
}