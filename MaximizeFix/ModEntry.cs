﻿using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Kokoro;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace Shockah.MaximizeFix;

public sealed class ModEntry : Mod
{
	private static ModEntry Instance = null!;

	public override void Entry(IModHelper helper)
	{
		Instance = this;
		var harmony = new Harmony(ModManifest.UniqueID);

		harmony.TryPatch(
			monitor: Monitor,
			original: () => AccessTools.DeclaredMethod(typeof(Game1), nameof(Game1.SetWindowSize)),
			transpiler: new HarmonyMethod(typeof(ModEntry), nameof(Game1_SetWindowSize_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> Game1_SetWindowSize_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldsfld(AccessTools.Field(typeof(Game1), nameof(Game1.graphics))),
					ILMatches.Ldarg(1),
					ILMatches.Call("set_PreferredBackBufferWidth"),
					ILMatches.Ldsfld(AccessTools.Field(typeof(Game1), nameof(Game1.graphics))),
					ILMatches.Ldarg(2),
					ILMatches.Call("set_PreferredBackBufferHeight")
				)
				.Remove()
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Monitor.Log($"Could not patch methods - {Instance.ModManifest.Name} probably won't work.\nReason: {ex}", LogLevel.Error);
			return instructions;
		}
	}
}