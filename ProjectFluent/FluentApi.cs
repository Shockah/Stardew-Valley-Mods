using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using System;

namespace Shockah.ProjectFluent
{
	public class FluentApi: IFluentApi
	{
		private IFluentProvider FluentProvider { get; set; }
		private IFluentFunctionManager FluentFunctionManager { get; set; }
		private IFluentValueFactory FluentValueFactory { get; set; }

		public IGameLocale DefaultLocale { get; private set; }

		internal FluentApi(IFluentProvider fluentProvider, IFluentFunctionManager fluentFunctionManager, IFluentValueFactory fluentValueFactory, IGameLocale defaultLocale)
		{
			this.FluentProvider = fluentProvider;
			this.FluentFunctionManager = fluentFunctionManager;
			this.FluentValueFactory = fluentValueFactory;
			this.DefaultLocale = defaultLocale;
		}

		public IGameLocale CurrentLocale =>
			ProjectFluent.Instance.CurrentLocale;

		public IGameLocale GetBuiltInLocale(LocalizedContentManager.LanguageCode languageCode)
			=> new BuiltInGameLocale(languageCode);

		public IGameLocale GetModLocale(ModLanguage language)
			=> new ModGameLocale(language);

		public IFluent<string> GetLocalizations(IGameLocale locale, IManifest mod, string? name = null)
			=> FluentProvider.GetFluent(locale, mod, name);

		public IFluent<string> GetLocalizationsForCurrentLocale(IManifest mod, string? name = null)
			=> new CurrentLocaleFluent(mod, name);

		public IEnumFluent<EnumType> GetEnumFluent<EnumType>(IFluent<string> baseFluent, string keyPrefix) where EnumType : struct, Enum
			=> new EnumFluent<EnumType>(baseFluent, keyPrefix);

		public IFluent<Input> GetMappingFluent<Input, Output>(IFluent<Output> baseFluent, Func<Input, Output> mapper)
			=> new MappingFluent<Input, Output>(baseFluent, mapper);

		public void RegisterFunction(IManifest mod, string name, FluentFunction function)
			=> FluentFunctionManager.RegisterFunction(mod, name, function);

		public void UnregisterFunction(IManifest mod, string name)
			=> FluentFunctionManager.UnregisterFunction(mod, name);

		public IFluentFunctionValue CreateStringValue(string value)
			=> FluentValueFactory.CreateStringValue(value);

		public IFluentFunctionValue CreateIntValue(int value)
			=> FluentValueFactory.CreateIntValue(value);

		public IFluentFunctionValue CreateLongValue(long value)
			=> FluentValueFactory.CreateLongValue(value);

		public IFluentFunctionValue CreateFloatValue(float value)
			=> FluentValueFactory.CreateFloatValue(value);

		public IFluentFunctionValue CreateDoubleValue(double value)
			=> FluentValueFactory.CreateDoubleValue(value);
	}
}