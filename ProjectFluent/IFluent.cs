namespace Shockah.ProjectFluent
{
    public interface IFluentKey
	{
		string FluentKey { get; }
	}

	public interface IFluent<Key>
	{
		string this[Key key]
		{
			get
			{
				return Get(key, null);
			}
		}

		string Get(Key key, object tokens);
	}
}