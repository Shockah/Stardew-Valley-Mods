using System;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	public interface IEnumFluent<EnumType>: IFluent<EnumType> where EnumType : Enum
	{
		EnumType GetFromLocalizedName(string localizedName);
		IEnumerable<string> GetAllLocalizedNames();
	}
}