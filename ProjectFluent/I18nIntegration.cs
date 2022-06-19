using HarmonyLib;
using Shockah.CommonModCode.IL;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Shockah.ProjectFluent
{
	internal static class I18nIntegration
	{
		private delegate IDictionary<string, IDictionary<string, string>> ReadTranslationFilesDelegateType(string folderPath, out IList<string> errors);

		private static IMonitor Monitor { get; set; } = null!;
		private static II18nDirectoryProvider I18nDirectoryProvider { get; set; } = null!;

		private static object SCoreInstance { get; set; } = null!;
		private static Action ReloadTranslationsDelegate { get; set; } = null!;
		private static ReadTranslationFilesDelegateType ReadTranslationFilesDelegate { get; set; } = null!;

		internal static void Setup(IMonitor monitor, Harmony harmony, II18nDirectoryProvider i18nDirectoryProvider)
		{
			Monitor = monitor;
			I18nDirectoryProvider = i18nDirectoryProvider;

			try
			{
				Type rawEnumerableType = typeof(IEnumerable<>);
				Type scoreType = AccessTools.TypeByName("StardewModdingAPI.Framework.SCore, StardewModdingAPI");
				Type modMetadataType = AccessTools.TypeByName("StardewModdingAPI.Framework.IModMetadata, StardewModdingAPI");
				Type modMetadataEnumerableType = rawEnumerableType.MakeGenericType(modMetadataType);

				MethodInfo scoreInstanceGetter = AccessTools.PropertyGetter(scoreType, "Instance");
				MethodInfo reloadTranslationsMethod = AccessTools.Method(scoreType, "ReloadTranslations", Array.Empty<Type>());
				MethodInfo reloadTranslationsEnumerableMethod = AccessTools.Method(scoreType, "ReloadTranslations", new Type[] { modMetadataEnumerableType });
				MethodInfo readTranslationFilesMethod = AccessTools.Method(scoreType, "ReadTranslationFiles");

				SCoreInstance = scoreInstanceGetter.Invoke(null, null)!;
				ReloadTranslationsDelegate = () => reloadTranslationsMethod.Invoke(SCoreInstance, null);
				ReadTranslationFilesDelegate = (string folderPath, out IList<string> errors) =>
				{
					var parameters = new object?[] { folderPath, null };
					var result = (IDictionary<string, IDictionary<string, string>>)readTranslationFilesMethod.Invoke(SCoreInstance, parameters)!;
					errors = (IList<string>)parameters[1]!;
					return result;
				};

				harmony.Patch(
					original: reloadTranslationsEnumerableMethod,
					transpiler: new HarmonyMethod(AccessTools.Method(typeof(I18nIntegration), nameof(SCore_ReloadTranslations_Transpiler)))
				);
			}
			catch (Exception ex)
			{
				monitor.Log($"Could not hook into SMAPI - i18n integration won't work.\nReason: {ex}", LogLevel.Error);
				return;
			}
		}

		internal static void ReloadTranslations()
			=> ReloadTranslationsDelegate();

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
				Monitor.Log($"Could not patch SMAPI methods - Project Fluent probably won't work.\nReason: Could not find IL to transpile.", LogLevel.Error);
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
			foreach (var directory in I18nDirectoryProvider.GetI18nDirectories(modInfo.Manifest))
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