namespace Shockah.XPDisplay
{
	public interface IImmersiveProfessionsAPI
	{
		/// <summary>Get an interface for this mod's config settings.</summary>
		IModConfig GetConfigs();

		public interface IModConfig
		{
			bool EnablePrestige { get; }
			uint RequiredExpPerExtendedLevel { get; }
		}
	}
}