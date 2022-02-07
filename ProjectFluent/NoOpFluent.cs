namespace Shockah.ProjectFluent
{
	internal class NoOpFluent: IFluent<string>
	{
		public string Get(string key, object tokens)
		{
			return key;
		}
	}
}
