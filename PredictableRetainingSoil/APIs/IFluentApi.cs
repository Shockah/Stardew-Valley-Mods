using StardewModdingAPI;
using System;

namespace Shockah.PredictableRetainingSoil
{
	public interface IFluentApi
	{
		Func<string, object, string> GetLocalizationFunctionForStringKeysForCurrentLocale(IManifest mod);
		Func<string, object, string> GetLocalizationFunctionForStringKeysForCurrentLocale(IManifest mod, string name);
	}
}