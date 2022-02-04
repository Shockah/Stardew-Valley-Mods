using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.DontStopMeNow
{
	public class DontStopMeNow: Mod
	{
		private static DontStopMeNow Instance { get; set; }

		internal ModConfig Config { get; private set; }

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			Config = helper.ReadConfig<ModConfig>();

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
			helper.Events.Input.ButtonPressed += OnButtonPressed;

			var harmony = new Harmony(ModManifest.UniqueID);
			try
			{
				harmony.Patch(
					original: AccessTools.Method(typeof(Farmer), nameof(Farmer.BeginUsingTool)),
					postfix: new HarmonyMethod(typeof(DontStopMeNow), nameof(Farmer_BeginUsingTool_Postfix))
				);
				harmony.Patch(
					original: AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.leftClick)),
					postfix: new HarmonyMethod(typeof(DontStopMeNow), nameof(MeleeWeapon_leftClick_Postfix))
				);
				harmony.Patch(
					original: AccessTools.Method(typeof(MeleeWeapon), "beginSpecialMove"),
					postfix: new HarmonyMethod(typeof(DontStopMeNow), nameof(MeleeWeapon_beginSpecialMove_Postfix))
				);

				foreach (var nestedType in typeof(Game1).GetTypeInfo().DeclaredNestedTypes)
				{
					if (!nestedType.DeclaredFields.Where(f => f.FieldType == typeof(Game1) && f.Name.EndsWith("__this")).Any())
						continue;
					if (!nestedType.DeclaredFields.Where(f => f.FieldType == typeof(KeyboardState) && f.Name == "currentKBState").Any())
						continue;
					if (!nestedType.DeclaredFields.Where(f => f.FieldType == typeof(MouseState) && f.Name == "currentMouseState").Any())
						continue;
					if (!nestedType.DeclaredFields.Where(f => f.FieldType == typeof(GamePadState) && f.Name == "currentPadState").Any())
						continue;
					if (!nestedType.DeclaredFields.Where(f => f.FieldType == typeof(GameTime) && f.Name == "time").Any())
						continue;

					foreach (var method in nestedType.DeclaredMethods)
					{
						if (!method.Name.StartsWith("<UpdateControlInput>"))
							continue;

						harmony.Patch(
							original: method,
							transpiler: new HarmonyMethod(typeof(DontStopMeNow), nameof(Game1_UpdateControlInput_Transpiler))
						);
						goto done;
					}
				}

				Monitor.Log($"Could not patch methods - Don't Stop Me Now probably won't work.\nReason: Cannot patch Game1.UpdateControlInput/hooks/OnGame1_UpdateControlInput/Delegate.", LogLevel.Error);
				done:;
			}
			catch (Exception e)
			{
				Monitor.Log($"Could not patch methods - Don't Stop Me Now probably won't work.\nReason: {e}", LogLevel.Error);
			}
		}

		private void SetupConfig()
		{
			// TODO: add translation support
			var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

			configMenu.Register(
				ModManifest,
				reset: () => Config = new ModConfig(),
				save: () => Helper.WriteConfig(Config)
			);

			configMenu.AddSectionTitle(
				mod: ModManifest,
				text: () => "Movement while swinging"
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Tools",
				tooltip: () => "Allows movement while swinging non-charging tools.",
				getValue: () => Config.MoveWhileSwingingTools,
				setValue: value => Config.MoveWhileSwingingTools = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Melee weapons",
				tooltip: () => "Allows movement while swinging a melee weapon.",
				getValue: () => Config.MoveWhileSwingingMeleeWeapons,
				setValue: value => Config.MoveWhileSwingingMeleeWeapons = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Special attacks",
				tooltip: () => "Allows movement while using a special attack of a melee weapon.",
				getValue: () => Config.MoveWhileSpecial,
				setValue: value => Config.MoveWhileSpecial = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Aiming slingshots",
				tooltip: () => "Allows movement while aiming a slingshot.",
				getValue: () => Config.MoveWhileAimingSlingshot,
				setValue: value => Config.MoveWhileAimingSlingshot = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Charging tools",
				tooltip: () => "Allows movement while charging tools.",
				getValue: () => Config.MoveWhileChargingTools,
				setValue: value => Config.MoveWhileChargingTools = value
			);

			configMenu.AddSectionTitle(
				mod: ModManifest,
				text: () => "Facing direction fixes"
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Tools",
				tooltip: () => "Allows changing the facing direction while using tools.",
				getValue: () => Config.FixToolFacing,
				setValue: value => Config.FixToolFacing = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Melee weapons",
				tooltip: () => "Allows changing the facing direction while using melee weapons.",
				getValue: () => Config.FixMeleeWeaponFacing,
				setValue: value => Config.FixMeleeWeaponFacing = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Charging tools",
				tooltip: () => "Allows changing the facing direction while charging a tool.",
				getValue: () => Config.FixChargingToolFacing,
				setValue: value => Config.FixChargingToolFacing = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Mouse",
				tooltip: () => "Face the direction of the mouse cursor when swinging.",
				getValue: () => Config.FixFacingOnMouse,
				setValue: value => Config.FixFacingOnMouse = value
			);

			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Controller",
				tooltip: () => "Face the direction of the cursor when swinging while playing using a controller.",
				getValue: () => Config.FixFacingOnController,
				setValue: value => Config.FixFacingOnController = value
			);
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			SetupConfig();
		}

		private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
		{
			if (!Context.IsPlayerFree)
				return;
			if (!Config.FixChargingToolFacing)
				return;
			var player = Game1.player;
			if (!player.UsingTool)
				return;
			if (player.toolHold <= 0 && player.toolPower <= 0)
				return;

			foreach (var useToolButton in Game1.options.useToolButton)
			{
				var sbutton = useToolButton.ToSButton();
				if (sbutton.IsPressed())
				{
					if (sbutton.GetButtonType() == InputHelper.ButtonType.Gamepad ? Config.FixFacingOnController : Config.FixFacingOnMouse)
					{
						FixFacingDirection();
						return;
					}
				}
			}
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (!Context.IsPlayerFree)
				return;
			if (!e.Button.IsUseToolButton() && !e.Button.IsActionButton())
				return;
			var player = Game1.player;
			if (!(player.CurrentTool is MeleeWeapon ? Config.FixMeleeWeaponFacing : Config.FixToolFacing))
				return;

			if (e.Button.GetButtonType() == InputHelper.ButtonType.Gamepad ? Config.FixFacingOnController : Config.FixFacingOnMouse)
				FixFacingDirection();
		}

		private static void FixFacingDirection()
		{
			var player = Game1.player;
			var cursor = new Vector2(Game1.viewport.X + Game1.getOldMouseX(), Game1.viewport.Y + Game1.getOldMouseY());
			
			var direction = cursor - player.GetBoundingBox().Center.ToVector2();
			if (Math.Abs(direction.X) > Math.Abs(direction.Y))
			{
				player.FacingDirection = direction.X >= 0 ? Game1.right : Game1.left;
			}
			else
			{
				player.FacingDirection = direction.Y >= 0 ? Game1.down : Game1.up;
			}
		}

		private static bool IsUsingPoweredUpOnHoldTool(Farmer player)
		{
			return player.UsingTool && (player.toolHold > 0 || player.toolPower > 0);
		}

		private static bool ShouldAllowMovement(Farmer player)
		{
			if (player.CurrentTool is MeleeWeapon weapon)
			{
				return weapon.isOnSpecial ? Instance.Config.MoveWhileSpecial : Instance.Config.MoveWhileSwingingMeleeWeapons;
			}
			else if (player.CurrentTool is Slingshot)
			{
				return Instance.Config.MoveWhileAimingSlingshot;
			}
			else
			{
				return IsUsingPoweredUpOnHoldTool(player) ? Instance.Config.MoveWhileChargingTools : Instance.Config.MoveWhileSwingingTools;
			}
		}

		private static bool Game1_UpdateControlInput_Transpiler_UsingToolReplacement()
		{
			var player = Game1.player;
			return player.UsingTool && !ShouldAllowMovement(player);
		}

		private static IEnumerable<CodeInstruction> Game1_UpdateControlInput_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();
			
			// IL to find:
			// IL_15d5: call class StardewValley.Farmer StardewValley.Game1::get_player()
			// IL_15da: callvirt instance bool StardewValley.Farmer::get_UsingTool()
			// IL_15df: brtrue IL_17a0

			var instructionsToFind = new List<Func<CodeInstruction, bool>> {
				i => i.opcode == OpCodes.Call && (MethodInfo)i.operand == AccessTools.Method(typeof(Game1), "get_player"),
				i => i.opcode == OpCodes.Callvirt && (MethodInfo)i.operand == AccessTools.Method(typeof(Farmer), "get_UsingTool"),
				i => i.opcode == OpCodes.Brtrue
			};

			var maxIndex = instructions.Count - instructionsToFind.Count;
			for (int index = 0; index < maxIndex; index++)
			{
				for (int toFindIndex = 0; toFindIndex < instructionsToFind.Count; toFindIndex++)
				{
					if (!instructionsToFind[toFindIndex](instructions[index + toFindIndex]))
						goto continueOuter;
				}

				// got a matching set of instructions; replacing
				instructions[index + 0] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(DontStopMeNow), nameof(Game1_UpdateControlInput_Transpiler_UsingToolReplacement)));
				instructions[index + 1] = new CodeInstruction(OpCodes.Nop);
				// instructions[index + 2]; // do nothing with instruction 2

				return instructions;
				continueOuter:;
			}

			Instance.Monitor.Log($"Could not patch methods - Don't Stop Me Now probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
			return instructions;
		}

		private static void Farmer_BeginUsingTool_Postfix(Farmer __instance)
		{
			if (!__instance.CanMove && ShouldAllowMovement(__instance))
				__instance.CanMove = true;
		}

		private static void MeleeWeapon_leftClick_Postfix(Farmer who)
		{
			if (!who.CanMove && ShouldAllowMovement(who))
				who.CanMove = true;
		}

		private static void MeleeWeapon_beginSpecialMove_Postfix(Farmer who)
		{
			if (!who.CanMove && ShouldAllowMovement(who))
				who.CanMove = true;
		}
	}
}