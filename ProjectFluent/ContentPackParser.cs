using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal interface IContentPackParser
	{
		ParseResult<ContentPackContent> Parse(IManifest? context, RawContentPackContent raw);
	}

	internal record ParseResult<Result> where Result : notnull
	{
		public Result? Parsed { get; init; }
		public ImmutableList<string> Errors { get; init; }
		public ImmutableList<string> Warnings { get; init; }

		public ParseResult(Result? parsed, ImmutableList<string> errors, ImmutableList<string> warnings)
		{
			this.Parsed = parsed;
			this.Errors = errors;
			this.Warnings = warnings;
		}
	}

	internal class ContentPackParser: IContentPackParser
	{
		private ISemanticVersion ProjectFluentVersion { get; set; }
		private IModRegistry ModRegistry { get; set; }

		public ContentPackParser(ISemanticVersion projectFluentVersion, IModRegistry modRegistry)
		{
			this.ProjectFluentVersion = projectFluentVersion;
			this.ModRegistry = modRegistry;
		}

		public ParseResult<ContentPackContent> Parse(IManifest? context, RawContentPackContent raw)
		{
			List<string> warnings = new();
			List<string> errors = new();

			if (raw.Format is null)
				errors.Add("Missing `Format` field.");
			else if (raw.Format.IsNewerThan(ProjectFluentVersion))
				errors.Add("`Format` is newer than {ProjectFluentVersion} and cannot be parsed.");

			if (errors.Count != 0)
				return new(null, warnings.ToImmutableList(), errors.ToImmutableList());

			List<ContentPackContent.AdditionalFluentPath> additionalFluentPaths = new();
			if (raw.AdditionalFluentPaths is not null)
			{
				foreach (var (entry, entryIndex) in raw.AdditionalFluentPaths.Select((e, i) => (e, i)))
				{
					var entryParseResult = ParseAdditionalFluentPath(context, entry, raw.Format!);
					foreach (var warning in entryParseResult.Warnings)
						warnings.Add($"`AdditionalFluentPaths`: {entryIndex}: {warning}");
					foreach (var error in entryParseResult.Errors)
						warnings.Add($"`AdditionalFluentPaths`: {entryIndex}: {error}");
					if (entryParseResult.Parsed is not null)
						additionalFluentPaths.Add(entryParseResult.Parsed);
				}
			}

			List<ContentPackContent.AdditionalI18nPath> additionalI18nPaths = new();
			if (raw.AdditionalI18nPaths is not null)
			{
				foreach (var (entry, entryIndex) in raw.AdditionalI18nPaths.Select((e, i) => (e, i)))
				{
					var entryParseResult = ParseAdditionalI18nPath(context, entry, raw.Format!);
					foreach (var warning in entryParseResult.Warnings)
						warnings.Add($"`AdditionalI18nPaths`: {entryIndex}: {warning}");
					foreach (var error in entryParseResult.Errors)
						warnings.Add($"`AdditionalI18nPaths`: {entryIndex}: {error}");
					if (entryParseResult.Parsed is not null)
						additionalI18nPaths.Add(entryParseResult.Parsed);
				}
			}

			ContentPackContent? parsed = errors.Count == 0 ? new(
				raw.Format!,
				additionalFluentPaths,
				additionalI18nPaths
			) : null;
			return new(parsed, warnings.ToImmutableList(), errors.ToImmutableList());
		}

		private ParseResult<ContentPackContent.AdditionalFluentPath> ParseAdditionalFluentPath(IManifest? context, RawContentPackContent.AdditionalFluentPath raw, ISemanticVersion format)
		{
			List<string> warnings = new();
			List<string> errors = new();

			if (raw.LocalizedMod is null)
				errors.Add("Missing `LocalizedMod` field.");

			string? localizingMod = null;
			if (raw.LocalizingMod is not null)
				localizingMod = raw.LocalizingMod;
			else if (context is not null)
				localizingMod = context.UniqueID;
			else
				errors.Add("Missing `LocalizingMod` field.");

			if (localizingMod is not null)
			{
				if (localizingMod.Equals("this", StringComparison.InvariantCultureIgnoreCase))
				{
					if (context is null)
					{
						errors.Add("`LocalizingMod` is `this`, but we do not know the context of `this` (are you trying to use `this` while asset editing?).");
						localizingMod = null;
					}
					else
					{
						localizingMod = context.UniqueID;
					}
				}
				else
				{
					var localizingModInstance = ModRegistry.Get(localizingMod);
					if (localizingModInstance is null)
						warnings.Add($"`LocalizingMod` specifies mod {localizingMod} that is not currently loaded.");
				}
			}

			ContentPackContent.AdditionalFluentPath? parsed = errors.Count == 0 ? new(
				raw.LocalizedMod!,
				localizingMod!,
				raw.LocalizingFile,
				raw.LocalizedFile,
				raw.LocalizingSubdirectory
			) : null;
			return new(parsed, warnings.ToImmutableList(), errors.ToImmutableList());
		}

		private ParseResult<ContentPackContent.AdditionalI18nPath> ParseAdditionalI18nPath(IManifest? context, RawContentPackContent.AdditionalI18nPath raw, ISemanticVersion format)
		{
			List<string> warnings = new();
			List<string> errors = new();

			if (raw.LocalizedMod is null)
				errors.Add("Missing `LocalizedMod` field.");

			string? localizingMod = null;
			if (raw.LocalizingMod is not null)
				localizingMod = raw.LocalizingMod;
			else if (context is not null)
				localizingMod = context.UniqueID;
			else
				errors.Add("Missing `LocalizingMod` field.");

			if (localizingMod is not null)
			{
				if (localizingMod.Equals("this", StringComparison.InvariantCultureIgnoreCase))
				{
					if (context is null)
					{
						errors.Add("`LocalizingMod` is `this`, but we do not know the context of `this` (are you trying to use `this` while asset editing?).");
						localizingMod = null;
					}
					else
					{
						localizingMod = context.UniqueID;
					}
				}
				else
				{
					var localizingModInstance = ModRegistry.Get(localizingMod);
					if (localizingModInstance is null)
						warnings.Add($"`LocalizingMod` specifies mod {localizingMod} that is not currently loaded.");
				}
			}

			ContentPackContent.AdditionalI18nPath? parsed = errors.Count == 0 ? new(
				raw.LocalizedMod!,
				localizingMod!,
				raw.LocalizingSubdirectory
			) : null;
			return new(parsed, warnings.ToImmutableList(), errors.ToImmutableList());
		}
	}
}