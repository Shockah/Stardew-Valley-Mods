namespace Shockah.ProjectFluent
{
	internal class MappingFluent<Key>: IFluent<Key> where Key : notnull
	{
		private readonly IFluent<string> Wrapped;

		public MappingFluent(IFluent<string> wrapped)
		{
			this.Wrapped = wrapped;
		}

		public string Get(Key key, object? tokens)
		{
			string mappedKey = key is IFluentKey fluentKey ? fluentKey.FluentKey : $"{key}";
			return Wrapped.Get(mappedKey, tokens);
		}
	}
}