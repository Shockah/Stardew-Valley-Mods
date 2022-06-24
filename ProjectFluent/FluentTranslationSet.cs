using Shockah.CommonModCode.SMAPI;

namespace Shockah.ProjectFluent
{
	internal class FluentTranslationSet<Key>: ITranslationSet<Key>
	{
		private IFluent<Key> Fluent { get; set; }

		public FluentTranslationSet(IFluent<Key> fluent)
		{
			this.Fluent = fluent;
		}

		public bool ContainsKey(Key key)
			=> Fluent.ContainsKey(key);

		public string Get(Key key)
			=> Fluent.Get(key);

		public string Get(Key key, object? tokens)
			=> Fluent.Get(key, tokens);
	}
}