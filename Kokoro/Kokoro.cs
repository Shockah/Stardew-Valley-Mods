using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Shockah.Kokoro.Stardew;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace Shockah.Kokoro;

public class Kokoro : BaseMod
{
	public static Kokoro Instance { get; private set; } = null!;

	private PerScreen<LinkedList<string>> QueuedObjectDialogue { get; init; } = new(() => new());

	public override void Entry(IModHelper helper)
	{
		Instance = this;

		// force-referencing Shrike assemblies, otherwise none dependent mods will load
		_ = typeof(ISequenceMatcher<CodeInstruction>).Name;
		_ = typeof(ILMatches).Name;

		helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
		MachineTracker.Setup(Monitor, helper, new Harmony(ModManifest.UniqueID));
	}

	private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
	{
		var harmony = new Harmony(ModManifest.UniqueID);
		FarmTypeManagerPatches.Apply(harmony);
	}

	private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
	{
		// dequeue object dialogue
		var message = QueuedObjectDialogue.Value.First;
		if (message is not null && Game1.activeClickableMenu is not DialogueBox)
		{
			QueuedObjectDialogue.Value.RemoveFirst();
			Game1.drawObjectDialogue(message.Value);
		}
	}

	public void QueueObjectDialogue(string message)
	{
		if (Game1.activeClickableMenu is DialogueBox)
			QueuedObjectDialogue.Value.AddLast(message);
		else
			Game1.drawObjectDialogue(message);
	}
}