using Fluent.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Shockah.ProjectFluent
{
	internal class FluentImpl: IFluent<string>
	{
		private readonly IFluent<string> fallback;
		private readonly MessageContext context;

		public FluentImpl(GameLocale locale, string content, IFluent<string> fallback = null)
		{
			this.fallback = fallback;

			var context = new MessageContext(locale.LanguageCode);
			var errors = context.AddMessages(content);
			this.context = context;
		}

		private IDictionary<string, object> ExtractTokens(object tokens)
		{
			// source: https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Translation.cs

			var results = new Dictionary<string, object>();
			if (tokens == null)
				return results;

			if (tokens is IDictionary dictionary)
			{
				foreach (DictionaryEntry entry in dictionary)
				{
					string key = entry.Key?.ToString().Trim();
					if (key != null)
						results[key] = entry.Value;
				}
			}
			else
			{
				Type type = tokens.GetType();
				foreach (FieldInfo field in type.GetFields())
				{
					results[field.Name] = field.GetValue(tokens);
				}
				foreach (PropertyInfo prop in type.GetProperties())
				{
					results[prop.Name] = prop.GetValue(tokens);
				}
			}

			return results;
		}

		public string Get(string key, object tokens)
		{
			if (context.HasMessage(key))
			{
				var message = context.GetMessage(key);
				return context.Format(message, ExtractTokens(tokens));
			}
			else
			{
				return fallback?.Get(key, tokens);
			}
		}
	}
}