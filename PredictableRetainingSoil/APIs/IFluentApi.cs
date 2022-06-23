using StardewModdingAPI;

namespace Shockah.PredictableRetainingSoil
{
	public interface IFluent<Key>
	{
		bool ContainsKey(Key key);

		string Get(Key key)
			=> Get(key, null);
		string Get(Key key, object tokens);

		string this[Key key]
			=> Get(key, null);
	}

	public interface IFluentApi
	{
		IFluent<string> GetLocalizationsForCurrentLocale(IManifest mod, string name = null);
	}
}