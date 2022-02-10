using StardewModdingAPI;

namespace Shockah.ProjectFluent
{
	internal class CurrentLocaleFluent<Key>: IFluent<Key>
	{
		private readonly IManifest mod;
		private readonly string name;

		private GameLocale locale;
		private IFluent<Key> wrapped;

		public CurrentLocaleFluent(IManifest mod, string name = null)
		{
			this.mod = mod;
			this.name = name;
		}

		private IFluent<Key> CurrentFluent
		{
			get
			{
				if (locale == null || locale != ProjectFluent.Instance.Api.CurrentLocale)
				{
					locale = ProjectFluent.Instance.Api.CurrentLocale;
					wrapped = ProjectFluent.Instance.Api.GetLocalizations<Key>(mod, name, locale);
				}
				return wrapped;
			}
		}

		public string Get(Key key, object tokens)
		{
			return CurrentFluent.Get(key, tokens);
		}
	}
}