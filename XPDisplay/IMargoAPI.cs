namespace Shockah.XPDisplay
{
	public interface IMargoAPI
	{
		IModConfig GetConfig();

		public interface IModConfig
		{
			bool EnableProfessions { get; }

			IProfessionsConfig Professions { get; }

			public interface IProfessionsConfig
			{
				bool EnablePrestige { get; }
			}
		}
	}
}
