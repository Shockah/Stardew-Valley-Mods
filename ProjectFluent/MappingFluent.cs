namespace Shockah.ProjectFluent
{
	internal class MappingFluent<Key>: IFluent<Key>
	{
		private readonly IFluent<string> wrapped;

		public MappingFluent(IFluent<string> wrapped)
		{
			this.wrapped = wrapped;
		}

		public string Get(Key key, object tokens)
		{
			var mappedKey = key is IFluentKey fluentKey ? fluentKey.FluentKey : key.ToString();
			return wrapped.Get(mappedKey, tokens);
		}
	}
}