using StardewModdingAPI;

namespace Shockah.ProjectFluent
{
	internal class CurrentLocaleFluent<Key>: IFluent<Key>
	{
		private readonly IManifest Mod;
		private readonly string? Name;

		private IGameLocale? Locale;
		private IFluent<Key> Wrapped = null!;

		public CurrentLocaleFluent(IManifest mod, string? name = null)
		{
			this.Mod = mod;
			this.Name = name;
		}

		private IFluent<Key> CurrentFluent
		{
			get
			{
				if (Locale is null || Locale.LanguageCode != ProjectFluent.Instance.Api.CurrentLocale.LanguageCode)
				{
					Locale = ProjectFluent.Instance.Api.CurrentLocale;
					Wrapped = ProjectFluent.Instance.Api.GetLocalizations<Key>(Locale, Mod, Name);
				}
				return Wrapped;
			}
		}

		public string Get(Key key, object? tokens)
		{
			return CurrentFluent.Get(key, tokens);
		}
	}
}