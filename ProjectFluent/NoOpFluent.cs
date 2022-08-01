namespace Shockah.ProjectFluent
{
	internal class NoOpFluent : IFluent<string>
	{
		public bool ContainsKey(string key)
			=> false;

		public string Get(string key, object? tokens)
			=> key;
	}
}