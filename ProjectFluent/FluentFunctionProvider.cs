using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal interface IFluentFunctionProvider
	{
		IEnumerable<(IManifest mod, string name, IFluentApi.FluentFunction function)> GetFluentFunctions();
	}

	internal class SerialFluentFunctionProvider: IFluentFunctionProvider
	{
		private IFluentFunctionProvider[] Providers { get; set; }

		public SerialFluentFunctionProvider(params IFluentFunctionProvider[] providers)
		{
			// making a copy on purpose
			this.Providers = providers.ToArray();
		}

		public IEnumerable<(IManifest mod, string name, IFluentApi.FluentFunction function)> GetFluentFunctions()
		{
			foreach (var provider in Providers)
				foreach (var function in provider.GetFluentFunctions())
					yield return function;
		}
	}

	internal class BuiltInFluentFunctionProvider: IFluentFunctionProvider
	{
		private IManifest ProjectFluentMod { get; set; }
		private IModRegistry ModRegistry { get; set; }

		private IFluentApi.FluentFunction ModNameFunction { get; set; }

		public BuiltInFluentFunctionProvider(IManifest projectFluentMod, IModRegistry modRegistry)
		{
			this.ProjectFluentMod = projectFluentMod;
			this.ModRegistry = modRegistry;

			ModNameFunction = (locale, mod, arguments) =>
			{
				var modID = mod.UniqueID;
				if (arguments.Count >= 1 && arguments[0] is string argumentModID)
					modID = argumentModID;

				var otherMod = ModRegistry.Get(modID);
				return otherMod?.Manifest.Name ?? modID;
			};
		}

		public IEnumerable<(IManifest mod, string name, IFluentApi.FluentFunction function)> GetFluentFunctions()
		{
			yield return (ProjectFluentMod, "MOD_NAME", ModNameFunction);
		}
	}
}