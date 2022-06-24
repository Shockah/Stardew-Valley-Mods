using StardewModdingAPI;

namespace Shockah.CommonModCode.SMAPI
{
	public interface ITranslationSet<Key>
	{
		bool ContainsKey(Key key);
		string Get(Key key);
		string Get(Key key, object? tokens);
	}

	public sealed class SMAPITranslationSetWrapper : ITranslationSet<string>
	{
		private ITranslationHelper Helper { get; set; }

		public SMAPITranslationSetWrapper(ITranslationHelper helper)
		{
			this.Helper = helper;
		}

		public bool ContainsKey(string key)
			=> Helper.Get(key).HasValue();

		public string Get(string key)
			=> Helper.Get(key);

		public string Get(string key, object? tokens)
			=> Helper.Get(key, tokens);
	}
}