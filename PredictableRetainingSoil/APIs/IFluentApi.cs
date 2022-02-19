using StardewModdingAPI;
using System;

namespace Shockah.PredictableRetainingSoil
{
	public interface IFluent<Key>
	{
		string this[Key key] { get; }
		string Get(Key key, object tokens);
	}

	public interface IFluentApi
	{
		IFluent<Key> GetLocalizationsForCurrentLocale<Key>(IManifest mod, string name = null);
	}
}