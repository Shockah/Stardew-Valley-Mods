using Fluent.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shockah.ProjectFluent
{
	internal class FluentImpl: IFluent<string>
	{
		private readonly IFluent<string> Fallback;
		private readonly MessageContext Context;

		public FluentImpl(IGameLocale locale, string content, IFluent<string>? fallback = null)
		{
			this.Fallback = fallback ?? new NoOpFluent();

			var context = new MessageContext(locale.LanguageCode);
			var errors = context.AddMessages(content);
			if (errors.Count > 0)
				throw new ArgumentException($"Errors parsing Fluent:\n{String.Join('\n', errors.Select(e => $"\t{e.Message}"))}");
			this.Context = context;
		}

		private IDictionary<string, object?> ExtractTokens(object? tokens)
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

		public string Get(string key, object? tokens)
		{
			var dotIndex = key.IndexOf('.');
			var messageKey = dotIndex == -1 ? key : key[..dotIndex];
			var attributeKey = dotIndex == -1 ? null : key[(dotIndex + 1)..];
			
			if (Context.HasMessage(messageKey))
			{
				var message = Context.GetMessage(messageKey);
				var node = attributeKey == null ? message : message.Attributes[attributeKey];
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
}