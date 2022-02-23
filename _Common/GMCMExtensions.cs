using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Linq.Expressions;

namespace Shockah.CommonModCode
{
	public static class GMCMExtensions
	{
		public static void AddSectionTitle(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix)
		{
			api.AddSectionTitle(
				mod: mod,
				text: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip")
			);
		}

		public static void AddParagraph(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string key)
		{
			api.AddParagraph(
				mod: mod,
				text: () => translations.Get(key)
			);
		}

		public static void AddBoolOption(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Func<bool> getValue, Action<bool> setValue, string? fieldId = null)
		{
			api.AddBoolOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public static void AddBoolOption(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Expression<Func<bool>> property, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			api.AddBoolOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public static void AddNumberOption(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Func<int> getValue, Action<int> setValue, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null)
		{
			api.AddNumberOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				min: min,
				max: max,
				interval: interval,
				formatValue: formatValue,
				fieldId: fieldId
			);
		}

		public static void AddNumberOption(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Expression<Func<int>> property, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			api.AddNumberOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				min: min,
				max: max,
				interval: interval,
				formatValue: formatValue,
				fieldId: fieldId
			);
		}

		public static void AddNumberOption(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Func<float> getValue, Action<float> setValue, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null)
		{
			api.AddNumberOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				min: min,
				max: max,
				interval: interval,
				formatValue: formatValue,
				fieldId: fieldId
			);
		}

		public static void AddNumberOption(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Expression<Func<float>> property, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			api.AddNumberOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				min: min,
				max: max,
				interval: interval,
				formatValue: formatValue,
				fieldId: fieldId
			);
		}

		public static void AddTextOption(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Func<string> getValue, Action<string> setValue, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null)
		{
			api.AddTextOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				allowedValues: allowedValues,
				formatAllowedValue: formatAllowedValue,
				fieldId: fieldId
			);
		}

		public static void AddTextOption(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Expression<Func<string>> property, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			api.AddTextOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				allowedValues: allowedValues,
				formatAllowedValue: formatAllowedValue,
				fieldId: fieldId
			);
		}

		public static void AddEnumOption<EnumType>(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Expression<Func<EnumType>> property, string? fieldId = null) where EnumType : struct, Enum
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			api.AddTextOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: () => Enum.GetName(getValue())!,
				setValue: value => setValue(Enum.Parse<EnumType>(value)),
				allowedValues: Enum.GetNames<EnumType>(),
				formatAllowedValue: value => translations.Get($"{keyPrefix}.value.{value}"),
				fieldId: fieldId
			);
		}

		public static void AddEnumOption<EnumType>(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Func<EnumType> getValue, Action<EnumType> setValue, string? fieldId = null) where EnumType : struct, Enum
		{
			api.AddTextOption(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: () => Enum.GetName(getValue())!,
				setValue: value => setValue(Enum.Parse<EnumType>(value)),
				allowedValues: Enum.GetNames<EnumType>(),
				formatAllowedValue: value => translations.Get($"{keyPrefix}.value.{value}"),
				fieldId: fieldId
			);
		}

		public static void AddKeybind(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Func<SButton> getValue, Action<SButton> setValue, string? fieldId = null)
		{
			api.AddKeybind(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public static void AddKeybind(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Expression<Func<SButton>> property, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			api.AddKeybind(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public static void AddKeybindList(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Func<KeybindList> getValue, Action<KeybindList> setValue, string? fieldId = null)
		{
			api.AddKeybindList(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public static void AddKeybindList(this IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations, string keyPrefix, Expression<Func<KeybindList>> property, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			api.AddKeybindList(
				mod: mod,
				name: () => translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate(translations, $"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		private static Func<string>? GetOptionalTranslatedStringDelegate(ITranslationHelper translations, string key)
		{
			var translation = translations.Get(key);
			return translation.HasValue() ? () => translation : null;
		}

		private static Expression<Action<T>> CreateSetter<T>(Expression<Func<T>> getter)
		{
			var parameter = Expression.Parameter(typeof(T), "value");
			var body = Expression.Assign(getter.Body, parameter);
			var setter = Expression.Lambda<Action<T>>(body, parameter);
			return setter;
		}
	}
}
