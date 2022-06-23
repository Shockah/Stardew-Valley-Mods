using Fluent.Net;
using Fluent.Net.RuntimeAst;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Shockah.ProjectFluent
{
	internal class FluentImpl: IFluent<string>
	{
		private IFluent<string> Fallback { get; set; }
		private MessageContext Context { get; set; }

		public FluentImpl(IEnumerable<(string name, ContextfulFluentFunction function)> functions, IGameLocale locale, string content, IFluent<string>? fallback = null)
		{
			this.Fallback = fallback ?? new NoOpFluent();

			var context = new MessageContext(locale.LanguageCode);
			foreach (var (functionName, function) in functions)
			{
				if (context.Functions.ContainsKey(functionName))
					continue;
				context.Functions[functionName] = (fluentPositionalArguments, _) =>
				{
					var positionalArguments = fluentPositionalArguments.Select(a =>
					{
						if (a is IFluentType fluentValue)
							return new FluentFunctionValue(fluentValue);
						else
							throw new ArgumentException($"Unsupported Fluent function argument type `{a.GetType()}`.");
					}).ToList();

					var result = function(locale, positionalArguments, ImmutableDictionary.Create<string, IFluentApi.IFluentFunctionValue>());
					var fluentResult = result.AsFluentValue();

					if (fluentResult is FluentType fluentResultValue)
						return fluentResultValue;
					else
						throw new ArgumentException($"Function `{functionName}` returned a value that is not a `{nameof(FluentType)}`.");
				};
			}

			var errors = context.AddMessages(content);
			if (errors.Count > 0)
				throw new ArgumentException($"Errors parsing Fluent:\n{string.Join('\n', errors.Select(e => $"\t{e.Message}"))}");
			this.Context = context;
		}

		private static IDictionary<string, object?> ExtractTokens(object? tokens)
		{
			// source: https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Translation.cs

			var results = new Dictionary<string, object?>();
			if (tokens == null)
				return results;

			if (tokens is IDictionary dictionary)
			{
				foreach (DictionaryEntry entry in dictionary)
				{
					string? key = entry.Key?.ToString()?.Trim();
					if (key is not null)
						results[key] = entry.Value;
				}
			}
			else
			{
				Type type = tokens.GetType();
				foreach (FieldInfo field in type.GetFields())
					results[field.Name] = field.GetValue(tokens);
				foreach (PropertyInfo prop in type.GetProperties())
					results[prop.Name] = prop.GetValue(tokens);
			}

			return results;
		}

		public bool ContainsKey(string key)
		{
			int dotIndex = key.IndexOf('.');
			string? messageKey = dotIndex == -1 ? key : key[..dotIndex];
			string? attributeKey = dotIndex == -1 ? null : key[(dotIndex + 1)..];

			if (!Context.HasMessage(messageKey))
				return false;
			var message = Context.GetMessage(messageKey);

			if (attributeKey is not null && !message.Attributes.ContainsKey(attributeKey))
				return false;

			return true;
		}

		public string Get(string key, object? tokens)
		{
			var dotIndex = key.IndexOf('.');
			var messageKey = dotIndex == -1 ? key : key[..dotIndex];
			var attributeKey = dotIndex == -1 ? null : key[(dotIndex + 1)..];
			
			if (Context.HasMessage(messageKey))
			{
				var message = Context.GetMessage(messageKey);
				Node? node = message;

				if (attributeKey is not null)
				{
					if (!message.Attributes.TryGetValue(attributeKey, out node))
						return Fallback.Get(key, tokens);
				}

				if (node == null)
					return Fallback.Get(key, tokens);
				return Context.Format(node, ExtractTokens(tokens)).Replace("\u2068", "").Replace("\u2069", "");
			}
			else
			{
				return Fallback.Get(key, tokens);
			}
		}
	}

	internal record FluentFunctionValue: IFluentApi.IFluentFunctionValue
	{
		internal IFluentType Value { get; set; }

		public FluentFunctionValue(IFluentType value)
		{
			this.Value = value;
		}

		public object AsFluentValue()
			=> Value;

		public string AsString()
			=> Value.Value;

		public int? AsIntOrNull()
			=> int.TryParse(Value.Value, out var @int) ? @int : null;

		public long? AsLongOrNull()
			=> int.TryParse(Value.Value, out var @int) ? @int : null;

		public float? AsFloatOrNull()
			=> float.TryParse(Value.Value, out var @int) ? @int : null;

		public double? AsDoubleOrNull()
			=> double.TryParse(Value.Value, out var @int) ? @int : null;
	}

	internal class FluentValueFactory: IFluentValueFactory
	{
		public IFluentApi.IFluentFunctionValue CreateStringValue(string value)
			=> new FluentFunctionValue(new FluentString(value));

		public IFluentApi.IFluentFunctionValue CreateIntValue(int value)
			=> new FluentFunctionValue(new FluentNumber($"{value}"));

		public IFluentApi.IFluentFunctionValue CreateLongValue(long value)
			=> new FluentFunctionValue(new FluentNumber($"{value}"));

		public IFluentApi.IFluentFunctionValue CreateFloatValue(float value)
			=> new FluentFunctionValue(new FluentNumber($"{value}"));

		public IFluentApi.IFluentFunctionValue CreateDoubleValue(double value)
			=> new FluentFunctionValue(new FluentNumber($"{value}"));
	}
}