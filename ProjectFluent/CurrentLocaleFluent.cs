using StardewModdingAPI;

namespace Shockah.ProjectFluent
{
	internal class CurrentLocaleFluent: IFluent<string>
	{
		private IManifest Mod { get; set; }
		private string? Name { get; set; }

		private IGameLocale? Locale { get; set; }
		private IFluent<string> Wrapped { get; set; } = null!;

		public CurrentLocaleFluent(IManifest mod, string? name = null)
		{
			this.Mod = mod;
			this.Name = name;
		}

		private IFluent<string> CurrentFluent
		{
			get
			{
				if (Locale is null || Locale.LanguageCode != ProjectFluent.Instance.CurrentLocale.LanguageCode)
				{
					Locale = ProjectFluent.Instance.CurrentLocale;
					Wrapped = ProjectFluent.Instance.Api.GetLocalizations(Locale, Mod, Name);
				}
				return Wrapped;
			}
		}

		public bool ContainsKey(string key)
			=> CurrentFluent.ContainsKey(key);

		public string Get(string key, object? tokens)
			=> CurrentFluent.Get(key, tokens);
	}
}