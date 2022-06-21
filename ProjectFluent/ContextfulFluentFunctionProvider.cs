using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal interface IContextfulFluentFunctionProvider
	{
		IEnumerable<(string name, ContextfulFluentFunction function)> GetFluentFunctionsForMod(IManifest mod);
	}

	internal delegate object ContextfulFluentFunction(IGameLocale locale, IReadOnlyList<object> arguments);

	internal class ContextfulFluentFunctionProvider: IContextfulFluentFunctionProvider
	{
		private IManifest ProjectFluentMod { get; set; }
		private IFluentFunctionProvider FluentFunctionProvider { get; set; }

		public ContextfulFluentFunctionProvider(IManifest projectFluentMod, IFluentFunctionProvider fluentFunctionProvider)
		{
			this.ProjectFluentMod = projectFluentMod;
			this.FluentFunctionProvider = fluentFunctionProvider;
		}

		public IEnumerable<(string name, ContextfulFluentFunction function)> GetFluentFunctionsForMod(IManifest mod)
		{
			var remainingFunctions = FluentFunctionProvider.GetFluentFunctions().ToList();

			var projectFluentFunctions = remainingFunctions.Where(f => f.mod.UniqueID == ProjectFluentMod.UniqueID).ToList();
			foreach (var function in projectFluentFunctions)
				remainingFunctions.Remove(function);

			var modFunctions = remainingFunctions.Where(f => f.mod.UniqueID == mod.UniqueID).ToList();
			foreach (var function in modFunctions)
				remainingFunctions.Remove(function);

			IEnumerable<(string name, ContextfulFluentFunction function)> EnumerableFunctions(IEnumerable<(IManifest mod, string name, IFluentApi.FluentFunction function)> input)
			{
				foreach (var function in input)
				{
					ContextfulFluentFunction contextfulFunction = (locale, arguments) => function.function(locale, mod, arguments);
					yield return (function.name, contextfulFunction);
					yield return ($"{ProjectFluentMod.UniqueID}/{function.name}", contextfulFunction);
				}
			}

			foreach (var function in EnumerableFunctions(projectFluentFunctions))
				yield return function;
			foreach (var function in EnumerableFunctions(modFunctions))
				yield return function;
			foreach (var function in EnumerableFunctions(remainingFunctions))
				yield return function;
		}
	}
}