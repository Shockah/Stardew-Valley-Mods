namespace Shockah.XPDisplay
{
	public interface IImmersiveProfessionsAPI
	{
		/// <summary>Get an interface for this mod's config settings.</summary>
		ModConfig GetConfigs();

		public interface ModConfig
		{
			bool EnablePrestige { get; }
			uint RequiredExpPerExtendedLevel { get; }
		}
	}
}