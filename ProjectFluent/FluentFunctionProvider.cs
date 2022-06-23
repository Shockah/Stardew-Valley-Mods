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
		private IFluentValueFactory FluentValueFactory { get; set; }

		private IFluentApi.FluentFunction ModNameFunction { get; set; }

		public BuiltInFluentFunctionProvider(IManifest projectFluentMod, IModRegistry modRegistry, IFluentValueFactory fluentValueFactory)
		{
			this.ProjectFluentMod = projectFluentMod;
			this.ModRegistry = modRegistry;
			this.FluentValueFactory = fluentValueFactory;

			ModNameFunction = (locale, mod, positionalArguments, namedArguments) =>
			{
				var modID = mod.UniqueID;
				if (positionalArguments.Count >= 1)
					modID = positionalArguments[0].AsString();

				var otherMod = ModRegistry.Get(modID);
				return fluentValueFactory.CreateStringValue(otherMod?.Manifest.Name ?? modID);
			};
		}

		public IEnumerable<(IManifest mod, string name, IFluentApi.FluentFunction function)> GetFluentFunctions()
		{
			yield return (ProjectFluentMod, "MOD_NAME", ModNameFunction);
		}
	}
}