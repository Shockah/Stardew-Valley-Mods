using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public delegate IFluentFunctionValue FluentFunction(
		IGameLocale locale,
		IManifest mod,
		IReadOnlyList<IFluentFunctionValue> positionalArguments,
		IReadOnlyDictionary<string, IFluentFunctionValue> namedArguments
	);
}