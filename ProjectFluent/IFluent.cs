using Shockah.CommonModCode.SMAPI;
using System;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public interface IFluent<Key>
	{
		bool ContainsKey(Key key);

		string Get(Key key)
			=> Get(key, null);
		string Get(Key key, object? tokens);

		string this[Key key]
			=> Get(key, null);
	}

	public interface IEnumFluent<EnumType>: IFluent<EnumType> where EnumType : Enum
	{
		EnumType GetFromLocalizedName(string localizedName);
		IEnumerable<string> GetAllLocalizedNames();
	}

	public class FluentTranslationSet<Key>: ITranslationSet<Key>
	{
		private IFluent<Key> Fluent { get; set; }

		public FluentTranslationSet(IFluent<Key> fluent)
		{
			this.Fluent = fluent;
		}

		public bool ContainsKey(Key key)
		{
			return Fluent.ContainsKey(key);
		}

		public string Get(Key key)
		{
			return Fluent.Get(key);
		}

		public string Get(Key key, object? tokens)
		{
			return Fluent.Get(key, tokens);
		}
	}
}