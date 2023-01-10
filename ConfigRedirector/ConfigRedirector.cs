using HarmonyLib;
using StardewModdingAPI;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using Shockah.Kokoro;

namespace Shockah.ConfigRedirector
{
	public class ConfigRedirector : BaseMod
	{
		internal static ConfigRedirector Instance = null!;

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			HashSet<string> modsToLoadEarly;
			try
			{
				Type scoreType = AccessTools.TypeByName("StardewModdingAPI.Framework.SCore, StardewModdingAPI");
				Type sconfigType = AccessTools.TypeByName("StardewModdingAPI.Framework.Models.SConfig, StardewModdingAPI");

				MethodInfo scoreGetter = AccessTools.PropertyGetter(scoreType, "Instance");
				FieldInfo sconfigField = AccessTools.Field(scoreType, "Settings");
				MethodInfo modsToLoadEarlyGetter = AccessTools.PropertyGetter(sconfigType, "ModsToLoadEarly");

				object score = scoreGetter.Invoke(null, null)!;
				object sconfig = sconfigField.GetValue(score)!;
				modsToLoadEarly = (HashSet<string>)modsToLoadEarlyGetter.Invoke(sconfig, null)!;

				if (!modsToLoadEarly.Contains(ModManifest.UniqueID))
					Monitor.Log($"{ModManifest.Name} is not marked to be loaded early. There is no guarantee any other mods that loaded earlier will be affected.", LogLevel.Warn);
			}
			catch (Exception ex)
			{
				Monitor.Log($"Could not reflect into SMAPI - cannot detect mods marked to be loaded early.\nReason: {ex}", LogLevel.Error);
				modsToLoadEarly = new();
			}

			var allModsLoadedEarlier = helper.ModRegistry.GetAll().TakeWhile(m => m.Manifest.UniqueID != ModManifest.UniqueID).ToList();
			var modsLoadedEarlierOnPurpose = allModsLoadedEarlier.Where(m => modsToLoadEarly.Contains(m.Manifest.UniqueID)).ToHashSet();
			LinkedList<IModInfo> modsToCheckForDependencies = new(modsLoadedEarlierOnPurpose);

			while (modsToCheckForDependencies.Count != 0)
			{
				var modToCheck = modsToCheckForDependencies.First!.Value;
				modsToCheckForDependencies.RemoveFirst();

				foreach (var dependency in modToCheck.Manifest.Dependencies)
				{
					var dependencyMod = allModsLoadedEarlier.FirstOrDefault(m => m.Manifest.UniqueID == dependency.UniqueID);
					if (dependencyMod is null)
						continue;
					modsLoadedEarlierOnPurpose.Add(dependencyMod);
					modsToCheckForDependencies.AddLast(dependencyMod);
				}
			}

			var modsLoadedEarlierWithoutEntry = allModsLoadedEarlier.ToHashSet().Except(modsLoadedEarlierOnPurpose).ToHashSet();

			if (modsLoadedEarlierWithoutEntry.Count != 0)
				Monitor.Log($"Found mods which loaded before {ModManifest.Name} - there is no guarantee they will be affected:\n{string.Join("\n", modsLoadedEarlierWithoutEntry.Select(m => $"\t{m.Manifest.Name}"))}", LogLevel.Warn);
			if (modsLoadedEarlierOnPurpose.Count != 0)
				Monitor.Log($"Found mods which loaded before {ModManifest.Name}, but they were listed to be loaded early, assuming on purpose - there is no guarantee they will be affected:\n{string.Join("\n", modsLoadedEarlierOnPurpose.Select(m => $"\t{m.Manifest.Name}"))}", LogLevel.Info);
		}
	}
}
