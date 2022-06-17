using HarmonyLib;
using Shockah.CommonModCode.IL;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.ProjectFluent
{
	internal class I18nIntegration
	{
		private delegate IDictionary<string, IDictionary<string, string>> ReadTranslationFilesDelegateType(string folderPath, out IList<string> errors);

		private static string? SetupEarlyErrorMessage;
		private static object SCoreInstance = null!;
		private static MethodInfo ReloadTranslationsEnumerableMethod = null!;
		private static Action ReloadTranslationsDelegate = null!;
		private static ReadTranslationFilesDelegateType ReadTranslationFilesDelegate = null!;

		// has to be called in the mod's constructor, NOT OnGameLaunched (the latter is too late and we won't obtain an `SCore` instance)
		// we can't add the transpiler here, because we're not ready to handle translations at this point
		internal static void SetupEarly(Harmony harmony)
		{
			try
			{
				Type scoreType = AccessTools.TypeByName("StardewModdingAPI.Framework.SCore, StardewModdingAPI");
				MethodInfo reloadTranslationsMethod = AccessTools.Method(scoreType, "ReloadTranslations", Array.Empty<Type>());
				ReloadTranslationsDelegate = () => reloadTranslationsMethod.Invoke(SCoreInstance, null);

				Type? imodMetadataType = AccessTools.TypeByName("StardewModdingAPI.Framework.IModMetadata, StardewModdingAPI");
				if (imodMetadataType is null)
				{
					SetupEarlyErrorMessage = "Could not patch SMAPI methods - i18n integration won't work.\nReason: IModMetadata type not found.";
					return;
				}

				Type rawEnumerableType = typeof(IEnumerable<>);
				Type imodMetadataEnumerableType = rawEnumerableType.MakeGenericType(imodMetadataType);
				ReloadTranslationsEnumerableMethod = AccessTools.Method(AccessTools.TypeByName("StardewModdingAPI.Framework.SCore, StardewModdingAPI"), "ReloadTranslations", new Type[] { imodMetadataEnumerableType });

				MethodInfo readTranslationFilesMethod = AccessTools.Method(scoreType, "ReadTranslationFiles");
				ReadTranslationFilesDelegate = (string folderPath, out IList<string> errors) =>
				{
					var parameters = new object?[] { folderPath, null };
					var result = (IDictionary<string, IDictionary<string, string>>)readTranslationFilesMethod.Invoke(SCoreInstance, parameters)!;
					errors = (IList<string>)parameters[1]!;
					return result;
				};
			}
			catch (Exception ex)
			{
				SetupEarlyErrorMessage = $"Could not reflect into SMAPI - i18n integration won't work.\nReason: {ex}";
				return;
			}

			try
			{
				harmony.Patch(
					original: ReloadTranslationsEnumerableMethod,
					prefix: new HarmonyMethod(typeof(I18nIntegration), nameof(SCore_ReloadTranslations_Prefix))
				);
			}
			catch (Exception ex)
			{
				SetupEarlyErrorMessage = $"Could not patch SMAPI methods - i18n integration won't work.\nReason: {ex}";
				return;
			}
		}

		internal static void Setup(Harmony harmony)
		{
			if (SetupEarlyErrorMessage is not null)
			{
				ProjectFluent.Instance.Monitor.Log(SetupEarlyErrorMessage, LogLevel.Error);
				return;
			}

			try
			{
				harmony.Patch(
					original: ReloadTranslationsEnumerableMethod,
					transpiler: new HarmonyMethod(typeof(I18nIntegration), nameof(SCore_ReloadTranslations_Transpiler))
				);
			}
			catch (Exception ex)
			{
				ProjectFluent.Instance.Monitor.Log($"Could not patch SMAPI methods - i18n integration won't work.\nReason: {ex}", LogLevel.Error);
			}
		}

		internal static void ReloadTranslations()
			=> ReloadTranslationsDelegate();

		private static void SCore_ReloadTranslations_Prefix(object __instance)
		{
			SCoreInstance = __instance;
		}

		private static IEnumerable<CodeInstruction> SCore_ReloadTranslations_Transpiler(IEnumerable<CodeInstruction> enumerableInstructions)
		{
			var instructions = enumerableInstructions.ToList();

			// IL to find (last occurence):
			// IL_0090: callvirt instance !0 class [System.Runtime]System.Collections.Generic.IEnumerator`1<class StardewModdingAPI.Framework.IModMetadata>::get_Current()
			// IL_0095: stloc.s 5
			// IL_0097: ldarg.0
			// IL_0098: ldloc.s 5
			// IL_009a: callvirt instance string StardewModdingAPI.Framework.IModMetadata::get_DirectoryPath()
			// IL_009f: ldstr "i18n"
			// IL_00a4: call string [System.Runtime]System.IO.Path::Combine(string, string)
			// IL_00a9: ldloca.s 7
			// IL_00ab: call instance class [System.Runtime]System.Collections.Generic.IDictionary`2<string, class [System.Runtime]System.Collections.Generic.IDictionary`2<string, string>> StardewModdingAPI.Framework.SCore::ReadTranslationFiles(string, class [System.Runtime]System.Collections.Generic.IList`1<string>&)
			// IL_00b0: stloc.s 6
			var worker = TranspileWorker.FindInstructionsBackwards(instructions, new Func<CodeInstruction, bool>[]
			{
				i => i.opcode == OpCodes.Callvirt && ((MethodBase)i.operand).Name == "get_Current",
				i => i.IsStloc(),
				i => i.IsLdarg(),
				i => i.IsLdloc(),
				i => i.opcode == OpCodes.Callvirt && ((MethodBase)i.operand).Name == "get_DirectoryPath",
				i => i.opcode == OpCodes.Ldstr && (string)i.operand == "i18n",
				i => i.opcode == OpCodes.Call && ((MethodBase)i.operand).Name == "Combine",
				i => i.IsLdloc(),
				i => i.opcode == OpCodes.Call && ((MethodBase)i.operand).Name == "ReadTranslationFiles",
				i => i.IsStloc()
			});
			if (worker is null)
			{
				ProjectFluent.Instance.Monitor.Log($"Could not patch SMAPI methods - Project Fluent probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
				return instructions;
			}

			worker.Postfix(new[]
			{
				worker[1].ToLoadLocal()!, // modInfo
				worker[9].ToLoadLocal()!, // translations
				worker[7].ToLoadLocalAddress()!, // errors
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(I18nIntegration), nameof(SCore_ReloadTranslations_Transpiler_ModifyList)))
			});

			return instructions;
		}

		private static void SCore_ReloadTranslations_Transpiler_ModifyList(
			IModInfo modInfo,
			IDictionary<string, IDictionary<string, string>> translations,
			ref IList<string> errors
		)
		{
			var asset = Game1.content.Load<Dictionary<string, List<string>>>(AssetManager.I18nPathsAssetPath);
			if (asset is null)
				return;

			if (asset.TryGetValue(modInfo.Manifest.UniqueID, out var entries))
			{
				foreach (string entry in (entries as IEnumerable<string>).Reverse())
				{
					string directory = entry;
					if (!Directory.Exists(directory))
					{
						IManifest? mod = ProjectFluent.Instance.Helper.ModRegistry.Get(entry)?.Manifest;
						if (mod is not null)
						{
							var modPath = ProjectFluent.Instance.GetModDirectoryPath(mod);
							if (modPath is not null)
								directory = Path.Combine(modPath, "i18n");
						}
					}
					if (Directory.Exists(directory))
					{
						var newTranslations = ReadTranslationFilesDelegate(directory, out var newErrors);
						foreach (var error in newErrors)
							errors.Add(error);
						foreach (var (key, value) in newTranslations)
							translations[key] = value;
					}
				}
			}
		}
	}
}