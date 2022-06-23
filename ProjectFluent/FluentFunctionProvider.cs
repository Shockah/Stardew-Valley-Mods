using StardewModdingAPI;
using System;
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
		internal IFluentProvider FluentProvider { get; set; } = null!;

		private IDictionary<(IGameLocale locale, string mod, string? name), IFluent<string>> FluentCache { get; set; } = new Dictionary<(IGameLocale locale, string mod, string? name), IFluent<string>>();

		public BuiltInFluentFunctionProvider(IManifest projectFluentMod, IModRegistry modRegistry, IFluentValueFactory fluentValueFactory)
		{
			this.ProjectFluentMod = projectFluentMod;
			this.ModRegistry = modRegistry;
			this.FluentValueFactory = fluentValueFactory;
		}

		public IEnumerable<(IManifest mod, string name, IFluentApi.FluentFunction function)> GetFluentFunctions()
		{
			yield return (ProjectFluentMod, "MOD_NAME", ModNameFunction);
			yield return (ProjectFluentMod, "FLUENT", FluentFunction);
		}

		private IFluentApi.IFluentFunctionValue ModNameFunction(
			IGameLocale locale,
			IManifest mod,
			IReadOnlyList<IFluentApi.IFluentFunctionValue> positionalArguments,
			IReadOnlyDictionary<string, IFluentApi.IFluentFunctionValue> namedArguments)
		{
			var modID = mod.UniqueID;
			if (positionalArguments.Count >= 1)
				modID = positionalArguments[0].AsString();

			var otherMod = ModRegistry.Get(modID);
			return FluentValueFactory.CreateStringValue(otherMod?.Manifest.Name ?? modID);
		}

		private IFluentApi.IFluentFunctionValue FluentFunction(
			IGameLocale locale,
			IManifest mod,
			IReadOnlyList<IFluentApi.IFluentFunctionValue> positionalArguments,
			IReadOnlyDictionary<string, IFluentApi.IFluentFunctionValue> namedArguments)
		{
			if (positionalArguments.Count == 0)
				throw new ArgumentException("Missing `Key` positional argument.");
			string targetKey = positionalArguments[0].AsString();

			string targetFluentMod = mod.UniqueID;
			string? targetFluentName = null;

			var remainingNamedArguments = new Dictionary<string, IFluentApi.IFluentFunctionValue>(namedArguments);
			if (remainingNamedArguments.TryGetValue("Mod", out var modArg))
				targetFluentMod = modArg.AsString();
			if (remainingNamedArguments.TryGetValue("Name", out var nameArg))
				targetFluentName = nameArg.AsString();

			if (!FluentCache.TryGetValue((locale, targetFluentMod, targetFluentName), out var fluent))
			{
				var otherMod = ModRegistry.Get(targetFluentMod);
				if (otherMod is null)
					throw new ArgumentException($"Mod `{targetFluentMod}` is not installed.");
				fluent = FluentProvider.GetFluent(locale, otherMod.Manifest, targetFluentName);
				FluentCache[(locale, targetFluentMod, targetFluentName)] = fluent;
			}
			return FluentValueFactory.CreateStringValue(fluent.Get(targetKey));
		}
	}
}