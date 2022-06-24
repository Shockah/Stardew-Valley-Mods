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
}