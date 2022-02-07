using HarmonyLib;
using StardewModdingAPI;
using System;

namespace Shockah.ProjectFluent
{
	internal static class ContentPatcherIntegration
	{
		private static readonly string ContentPatcherModID = "Pathoschild.ContentPatcher";
		private static readonly string ContentPatcherModTokenContextQualifiedName = "ContentPatcher.Framework.ModTokenContext, ContentPatcher";
		private static readonly string RegisterFluentTokenManifestKey = "UsesFluentContentPatcherTokens";

		private static bool didTryGetTokenWithNamespace = false;
		private static Func<object, string> getScope;
		private static Func<object, string, bool, object> getToken;

		internal static void Setup(Harmony harmony)
		{
			var api = ProjectFluent.Instance.Helper.ModRegistry.GetApi<IContentPatcherApi>(ContentPatcherModID);
			if (api == null)
				return;

			Patch(harmony);
			RegisterTokenInContentPacks(api);
		}
		
		private static void Patch(Harmony harmony)
		{
			try
			{
				var modTokenContextType = Type.GetType(ContentPatcherModTokenContextQualifiedName);

				var getScopeField = AccessTools.Field(modTokenContextType, "Scope");
				getScope = (context) => (string)getScopeField.GetValue(context);

				var getTokenMethod = AccessTools.Method(modTokenContextType, "GetToken");
				getToken = (context, name, enforceContext) => getTokenMethod.Invoke(context, new object[] { name, enforceContext });

				harmony.Patch(
					original: getTokenMethod,
					postfix: new HarmonyMethod(typeof(ContentPatcherIntegration), nameof(ModTokenContext_GetToken_Postfix))
				);
			}
			catch (Exception e)
			{
				ProjectFluent.Instance.Monitor.Log($"Could not patch Content Patcher methods - integration with it probably won't work.\nReason: {e}", LogLevel.Error);
			}
		}

		private static void RegisterTokenInContentPacks(IContentPatcherApi api)
		{
			foreach (var modInfo in ProjectFluent.Instance.Helper.ModRegistry.GetAll())
			{
				if (modInfo.Manifest.ContentPackFor?.UniqueID != ContentPatcherModID)
					continue;
				if (modInfo.Manifest.ExtraFields.TryGetValue(RegisterFluentTokenManifestKey, out object value) && value is true)
				{
					api.RegisterToken(modInfo.Manifest, "Fluent", new ContentPatcherToken(modInfo.Manifest, null));
				}
			}
		}

		private static void ModTokenContext_GetToken_Postfix(ref object __instance, ref object __result, string name, bool enforceContext)
		{
			if (__result != null)
				return;
			if (didTryGetTokenWithNamespace)
				return;
			if (name.Contains('/'))
				return;

			didTryGetTokenWithNamespace = true;
			__result = getToken(__instance, $"{getScope(__instance)}/{name}", enforceContext);
			didTryGetTokenWithNamespace = false;
		}
	}
}