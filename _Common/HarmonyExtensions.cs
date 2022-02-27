using HarmonyLib;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.CommonModCode
{
	public static class HarmonyExtensions
	{
		public static void PatchVirtual(this Harmony self, MethodBase original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? finalizer = null, IMonitor? monitor = null)
		{
			Type? declaringType = original.DeclaringType;
			if (declaringType == null)
				throw new ArgumentException($"{nameof(original)}.{nameof(original.DeclaringType)} is null.");
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				IEnumerable<Type> subtypes = Enumerable.Empty<Type>();
				try
				{
					subtypes = assembly.GetTypes().Where(t => t.IsAssignableTo(declaringType));
				}
				catch (Exception ex)
				{
					monitor?.Log($"There was a problem while getting types defined in assembly {assembly.GetName().Name}, ignoring it. Reason:\n{ex}", LogLevel.Trace);
				}
				
				foreach (Type subtype in subtypes)
				{
					var originalParameters = original.GetParameters();
					var subtypeOriginal = AccessTools.Method(
						subtype,
						original.Name,
						originalParameters.Select(p => p.ParameterType).ToArray()
					);
					if (subtypeOriginal is null)
						continue;
					if (!subtypeOriginal.IsDeclaredMember())
						continue;

					static bool ContainsNonSpecialArguments(HarmonyMethod patch)
						=> patch.method.GetParameters().Any(p => !(p.Name ?? "").StartsWith("__"));

					if (
						(prefix is not null && ContainsNonSpecialArguments(prefix)) ||
						(postfix is not null && ContainsNonSpecialArguments(postfix)) ||
						(finalizer is not null && ContainsNonSpecialArguments(finalizer))
					)
					{
						var subtypeOriginalParameters = subtypeOriginal.GetParameters();
						for (int i = 0; i < original.GetParameters().Length; i++)
							if (originalParameters[i].Name != subtypeOriginalParameters[i].Name)
								throw new InvalidOperationException($"Method {declaringType.Name}.{original.Name} cannot be automatically patched for subtype {subtype.Name}, because argument #{i} has a mismatched name: `{originalParameters[i].Name}` vs `{subtypeOriginalParameters[i].Name}`.");
					}

					self.Patch(subtypeOriginal, prefix, postfix, null, finalizer);
					monitor?.Log($"Patched method {original.Name} for type {subtype.FullName ?? subtype.Name}.", LogLevel.Debug);
				}
			}
		}
	}
}
