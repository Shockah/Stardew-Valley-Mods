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
		private static IFluentApi.FluentFunction ModNameFunction { get; set; } = (locale, mod, arguments) => mod.Name;

		private IManifest ProjectFluentMod { get; set; }

		public BuiltInFluentFunctionProvider(IManifest projectFluentMod)
		{
			this.ProjectFluentMod = projectFluentMod;
		}

		public IEnumerable<(IManifest mod, string name, IFluentApi.FluentFunction function)> GetFluentFunctions()
		{
			yield return (ProjectFluentMod, "MOD_NAME", ModNameFunction);
		}
	}
}