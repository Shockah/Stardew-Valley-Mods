﻿using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Linq.Expressions;

namespace Shockah.CommonModCode.GMCM
{
	public class GMCMI18nHelper
	{
		private readonly IGenericModConfigMenuApi Api;
		private readonly IManifest Mod;
		private readonly ITranslationHelper Translations;

		public GMCMI18nHelper(IGenericModConfigMenuApi api, IManifest mod, ITranslationHelper translations)
		{
			this.Api = api;
			this.Mod = mod;
			this.Translations = translations;
		}

		public void AddSectionTitle(string keyPrefix)
		{
			Api.AddSectionTitle(
				mod: Mod,
				text: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip")
			);
		}

		public void AddParagraph(string key)
		{
			Api.AddParagraph(
				mod: Mod,
				text: () => Translations.Get(key)
			);
		}

		public void AddBoolOption(string keyPrefix, Func<bool> getValue, Action<bool> setValue, string? fieldId = null)
		{
			Api.AddBoolOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public void AddBoolOption(string keyPrefix, Expression<Func<bool>> property, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			Api.AddBoolOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public void AddNumberOption(string keyPrefix, Func<int> getValue, Action<int> setValue, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null)
		{
			Api.AddNumberOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				min: min,
				max: max,
				interval: interval,
				formatValue: formatValue,
				fieldId: fieldId
			);
		}

		public void AddNumberOption(string keyPrefix, Expression<Func<int>> property, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			Api.AddNumberOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				min: min,
				max: max,
				interval: interval,
				formatValue: formatValue,
				fieldId: fieldId
			);
		}

		public void AddNumberOption(string keyPrefix, Func<float> getValue, Action<float> setValue, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null)
		{
			Api.AddNumberOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				min: min,
				max: max,
				interval: interval,
				formatValue: formatValue,
				fieldId: fieldId
			);
		}

		public void AddNumberOption(string keyPrefix, Expression<Func<float>> property, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			Api.AddNumberOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				min: min,
				max: max,
				interval: interval,
				formatValue: formatValue,
				fieldId: fieldId
			);
		}

		public void AddTextOption(string keyPrefix, Func<string> getValue, Action<string> setValue, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null)
		{
			Api.AddTextOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				allowedValues: allowedValues,
				formatAllowedValue: formatAllowedValue,
				fieldId: fieldId
			);
		}

		public void AddTextOption(string keyPrefix, Expression<Func<string>> property, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			Api.AddTextOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				allowedValues: allowedValues,
				formatAllowedValue: formatAllowedValue,
				fieldId: fieldId
			);
		}

		public void AddEnumOption<EnumType>(string keyPrefix, Expression<Func<EnumType>> property, string? valuePrefix = null, string? fieldId = null) where EnumType: struct, Enum
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			Api.AddTextOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: () => Enum.GetName(getValue())!,
				setValue: value => setValue(Enum.Parse<EnumType>(value)),
				allowedValues: Enum.GetNames<EnumType>(),
				formatAllowedValue: value => Translations.Get($"{valuePrefix ?? keyPrefix}.value.{value}"),
				fieldId: fieldId
			);
		}

		public void AddEnumOption<EnumType>(string keyPrefix, Func<EnumType> getValue, Action<EnumType> setValue, string? valuePrefix = null, string? fieldId = null) where EnumType: struct, Enum
		{
			Api.AddTextOption(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: () => Enum.GetName(getValue())!,
				setValue: value => setValue(Enum.Parse<EnumType>(value)),
				allowedValues: Enum.GetNames<EnumType>(),
				formatAllowedValue: value => Translations.Get($"{valuePrefix ?? keyPrefix}.value.{value}"),
				fieldId: fieldId
			);
		}

		public void AddKeybind(string keyPrefix, Func<SButton> getValue, Action<SButton> setValue, string? fieldId = null)
		{
			Api.AddKeybind(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public void AddKeybind(string keyPrefix, Expression<Func<SButton>> property, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			Api.AddKeybind(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public void AddKeybindList(string keyPrefix, Func<KeybindList> getValue, Action<KeybindList> setValue, string? fieldId = null)
		{
			Api.AddKeybindList(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public void AddKeybindList(string keyPrefix, Expression<Func<KeybindList>> property, string? fieldId = null)
		{
			var getValue = property.Compile()!;
			var setValue = CreateSetter(property).Compile()!;
			Api.AddKeybindList(
				mod: Mod,
				name: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip"),
				getValue: getValue,
				setValue: setValue,
				fieldId: fieldId
			);
		}

		public void AddPage(string pageId, string? keyPrefix = null)
		{
			Api.AddPage(
				mod: Mod,
				pageId: pageId,
				pageTitle: keyPrefix is null ? null : () => Translations.Get($"{keyPrefix}.name")
			);
		}

		public void AddPageLink(string pageId, string keyPrefix)
		{
			Api.AddPageLink(
				mod: Mod,
				pageId: pageId,
				text: () => Translations.Get($"{keyPrefix}.name"),
				tooltip: GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip")
			);
		}

		private Func<string>? GetOptionalTranslatedStringDelegate(string key)
		{
			var translation = Translations.Get(key);
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
