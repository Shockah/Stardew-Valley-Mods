namespace Shockah.XPDisplay
{
	public interface IImmersiveProfessionsAPI
	{
		IModConfig GetConfigs();

		public interface IModConfig
		{
			bool EnablePrestige { get; }
		}
	}
}