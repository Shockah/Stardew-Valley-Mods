using HarmonyLib;
using Microsoft.Xna.Framework;
using Shockah.CommonModCode;
using Shockah.CommonModCode.IL;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.SafeLightning
{
	public class SafeLightning: Mod
	{
		private static SafeLightning Instance = null!;

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			var harmony = new Harmony(ModManifest.UniqueID);
			harmony.TryPatch(
				monitor: Monitor,
				original: () => AccessTools.Method(typeof(Utility), nameof(Utility.performLightningUpdate)),
				transpiler: new HarmonyMethod(typeof(SafeLightning), nameof(Utility_performLightningUpdate_Transpiler))
			);
		}

		private static IEnumerable<CodeInstruction> Utility_performLightningUpdate_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions, ILGenerator il, MethodBase __originalMethod)
		{
			var lightningRodsLocalIndex = __originalMethod.GetMethodBody()?.LocalVariables.FirstIndex(l => l.LocalType.IsAssignableTo(typeof(IList<Vector2>)));
			if (lightningRodsLocalIndex is null)
			{
				Instance.Monitor.Log($"Could not patch methods - Safe Lightning probably won't work.\nReason: Method changed, possibly due to a major game update (or conflicting mod).", LogLevel.Error);
				return enumerableInstructions;
			}

			var instructions = enumerableInstructions.ToList();

			// IL to find:
			// IL_0402: newobj instance void StardewValley.Farm / LightningStrikeEvent::.ctor()
			var worker = TranspileWorker.FindInstructionsBackwards(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.opcode == OpCodes.Newobj && (ConstructorInfo)i.operand == AccessTools.Constructor(typeof(Farm.LightningStrikeEvent))
			});
			if (worker is null)
			{
				Instance.Monitor.Log($"Could not patch methods - Safe Lightning probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
				return instructions;
			}

			var smallLightningJumpLabel = il.DefineLabel();
			worker[0].labels.Add(smallLightningJumpLabel);

			// IL to find:
			// IL_00e6: ldloc.3
			// IL_00e7: callvirt instance int32 class [System.Collections] System.Collections.Generic.List`1<valuetype[MonoGame.Framework] Microsoft.Xna.Framework.Vector2>::get_Count()
			// IL_00ec: ldc.i4.0
			// IL_00ed: ble IL_01cf
		 
			worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.IsLdlocWithIndex(lightningRodsLocalIndex.Value),
				i => i.opcode == OpCodes.Callvirt && ((MethodBase)i.operand).Name == "get_Count",
				i => i.IsLdcI4(0),
				i => i.IsBle()
			});
			if (worker is null)
			{
				Instance.Monitor.Log($"Could not patch methods - Safe Lightning probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
				return instructions;
			}

			var noLightningRodsJumpLabel = (Label)worker[3].operand;

			// moving worker to the label
			worker = TranspileWorker.FindInstructions(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.labels.Contains(noLightningRodsJumpLabel)
			});
			if (worker is null)
			{
				Instance.Monitor.Log($"Could not patch methods - Safe Lightning probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
				return instructions;
			}

			worker.Prefix(new[]
			{
				new CodeInstruction(OpCodes.Br, smallLightningJumpLabel)
			});

			return instructions;
		}
	}
}
