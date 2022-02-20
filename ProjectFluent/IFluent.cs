using System;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public interface IFluentKey
	{
		string FluentKey { get; }
	}

	public interface IFluent<Key>
	{
		string this[Key key]
			=> Get(key, null);

		string Get(Key key, object tokens);
	}

	public interface IEnumFluent<EnumType>: IFluent<EnumType> where EnumType: Enum
	{
		EnumType GetFromLocalizedName(string localizedName);
		IEnumerable<string> GetAllLocalizedNames();
	}
}