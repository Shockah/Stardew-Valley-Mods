using HarmonyLib;
using Shockah.Kokoro;
using StardewModdingAPI;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Text;

namespace Shockah.ConfigRedirector
{
	public class ConfigRedirector : BaseMod
	{
		internal static ConfigRedirector Instance = null!;

		[ThreadStatic]
		private static string? CurrentlyUsedFile;

		private string ModsPath = null!;
		private List<string>? ModsToLoadEarly;

		public override void Entry(IModHelper helper)
		{
			Instance = this;

			ReflectIntoSMAPI();
			LogModsLoadedEarlier();
			PatchFileMethods();
		}

		private void ReflectIntoSMAPI()
		{
			try
			{
				Type scoreType = AccessTools.TypeByName("StardewModdingAPI.Framework.SCore, StardewModdingAPI");
				Type sconfigType = AccessTools.TypeByName("StardewModdingAPI.Framework.Models.SConfig, StardewModdingAPI");

				MethodInfo scoreGetter = AccessTools.PropertyGetter(scoreType, "Instance");
				object score = scoreGetter.Invoke(null, null)!;

				MethodInfo modsPathGetter = AccessTools.PropertyGetter(scoreType, "ModsPath");
				ModsPath = (string)modsPathGetter.Invoke(score, null)!;

				FieldInfo sconfigField = AccessTools.Field(scoreType, "Settings");
				object sconfig = sconfigField.GetValue(score)!;

				MethodInfo modsToLoadEarlyGetter = AccessTools.PropertyGetter(sconfigType, "ModsToLoadEarly");
				ModsToLoadEarly = ((HashSet<string>)modsToLoadEarlyGetter.Invoke(sconfig, null)!).ToList();
			}
			catch (Exception ex)
			{
				Monitor.Log($"Could not reflect into SMAPI - cannot detect mods marked to be loaded early.\nReason: {ex}", LogLevel.Error);
			}
		}

		private void LogModsLoadedEarlier()
		{
			if (ModsToLoadEarly is null)
				return;

			if (!ModsToLoadEarly.Contains(ModManifest.UniqueID))
				Monitor.Log($"{ModManifest.Name} is not marked to be loaded early. There is no guarantee any other mods that loaded earlier will be affected.", LogLevel.Warn);

			var allModsLoadedEarlier = Helper.ModRegistry.GetAll().TakeWhile(m => m.Manifest.UniqueID != ModManifest.UniqueID).ToList();
			var modsLoadedEarlierOnPurpose = allModsLoadedEarlier.Where(m => ModsToLoadEarly.Contains(m.Manifest.UniqueID)).ToHashSet();
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

		private void PatchFileMethods()
		{
			var harmony = new Harmony(ModManifest.UniqueID);

			#region Generic file opens

			{
				// `path` parameter
				var methodsToPatch = new Func<MethodInfo>[]
				{
					() => AccessTools.Method(typeof(File), nameof(File.Open), new Type[] { typeof(string), typeof(FileMode) }),
					() => AccessTools.Method(typeof(File), nameof(File.Open), new Type[] { typeof(string), typeof(FileMode), typeof(FileAccess) }),
					() => AccessTools.Method(typeof(File), nameof(File.Open), new Type[] { typeof(string), typeof(FileMode), typeof(FileAccess), typeof(FileShare) })
				};

				foreach (var methodToPatch in methodsToPatch)
					harmony.TryPatch(
						monitor: Monitor,
						original: methodToPatch,
						prefix: new HarmonyMethod(AccessTools.Method(typeof(ConfigRedirector), nameof(ConfigRedirector.FileOpen_PathArgument_ModeArgument_Prefix))),
						finalizer: new HarmonyMethod(AccessTools.Method(typeof(ConfigRedirector), nameof(ConfigRedirector.AnyFile_Cleanup_Finalizer)))
					);
			}

			#endregion

			#region File reads

			{
				// `path` parameter
				var methodsToPatch = new Func<MethodInfo>[]
				{
					() => AccessTools.Method(typeof(File), nameof(File.OpenRead), new Type[] { typeof(string) }),
					() => AccessTools.Method(typeof(File), nameof(File.OpenText), new Type[] { typeof(string) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllText), new Type[] { typeof(string) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllText), new Type[] { typeof(string), typeof(Encoding) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllTextAsync), new Type[] { typeof(string), typeof(CancellationToken) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllTextAsync), new Type[] { typeof(string), typeof(Encoding), typeof(CancellationToken) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllLines), new Type[] { typeof(string) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllLines), new Type[] { typeof(string), typeof(Encoding) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllLinesAsync), new Type[] { typeof(string), typeof(CancellationToken) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllLinesAsync), new Type[] { typeof(string), typeof(Encoding), typeof(CancellationToken) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadLines), new Type[] { typeof(string) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadLines), new Type[] { typeof(string), typeof(Encoding) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllBytes), new Type[] { typeof(string) }),
					() => AccessTools.Method(typeof(File), nameof(File.ReadAllBytesAsync), new Type[] { typeof(string), typeof(CancellationToken) })
				};

				foreach (var methodToPatch in methodsToPatch)
					harmony.TryPatch(
						monitor: Monitor,
						original: methodToPatch,
						prefix: new HarmonyMethod(AccessTools.Method(typeof(ConfigRedirector), nameof(ConfigRedirector.FileRead_PathArgument_Prefix))),
						finalizer: new HarmonyMethod(AccessTools.Method(typeof(ConfigRedirector), nameof(ConfigRedirector.AnyFile_Cleanup_Finalizer)))
					);
			}

			#endregion

			#region File writes

			{
				// `path` parameter
				var methodsToPatch = new Func<MethodInfo>[]
				{
					() => AccessTools.Method(typeof(File), nameof(File.OpenWrite), new Type[] { typeof(string) })
				};

				foreach (var methodToPatch in methodsToPatch)
					harmony.TryPatch(
						monitor: Monitor,
						original: methodToPatch,
						prefix: new HarmonyMethod(AccessTools.Method(typeof(ConfigRedirector), nameof(ConfigRedirector.FileWrite_PathArgument_Prefix))),
						finalizer: new HarmonyMethod(AccessTools.Method(typeof(ConfigRedirector), nameof(ConfigRedirector.AnyFile_Cleanup_Finalizer)))
					);
			}

			#endregion
		}

		private void HandleFileRead(ref string path)
		{
			CurrentlyUsedFile = path;
			path = GetRedirectedPath(path);
		}

		private void HandleFileWrite(ref string path)
		{
			CurrentlyUsedFile = path;
			path = GetRedirectedPath(path);
		}

		private string GetRedirectedPath(string path)
		{
		}

		private static void FileOpen_PathArgument_ModeArgument_Prefix(ref string path, FileMode mode)
		{
			switch (mode)
			{
				case FileMode.Open:
					Instance.HandleFileRead(ref path);
					break;
				case FileMode.CreateNew:
				case FileMode.Create:
				case FileMode.OpenOrCreate:
				case FileMode.Truncate:
				case FileMode.Append:
				default:
					Instance.HandleFileWrite(ref path);
					break;
			}
		}

		private static void FileRead_PathArgument_Prefix(ref string path)
			=> Instance.HandleFileRead(ref path);

		private static void FileWrite_PathArgument_Prefix(ref string path)
			=> Instance.HandleFileWrite(ref path);

		private static void AnyFile_Cleanup_Finalizer()
		{
			CurrentlyUsedFile = null;
		}
	}
}
