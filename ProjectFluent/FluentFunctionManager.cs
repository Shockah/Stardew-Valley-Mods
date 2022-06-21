using Shockah.CommonModCode;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	internal interface IFluentFunctionManager
	{
		void RegisterFunction(IManifest mod, string name, IFluentApi.FluentFunction function);
		void UnregisterFunction(IManifest mod, string name);
	}

	internal class FluentFunctionManager: IFluentFunctionManager, IFluentFunctionProvider
	{
		private IList<(IManifest mod, string name, IFluentApi.FluentFunction function)> Functions { get; set; } = new List<(IManifest mod, string name, IFluentApi.FluentFunction function)>();

		public void RegisterFunction(IManifest mod, string name, IFluentApi.FluentFunction function)
		{
			// TODO: check for existing entries
			Functions.Add((mod, name, function));
		}

		public void UnregisterFunction(IManifest mod, string name)
		{
			var index = Functions.FirstIndex(f => f.mod.UniqueID == mod.UniqueID && f.name == name);
			if (index is not null)
				Functions.RemoveAt(index.Value);
		}

		public IEnumerable<(IManifest mod, string name, IFluentApi.FluentFunction function)> GetFluentFunctions()
			=> Functions;
	}
}