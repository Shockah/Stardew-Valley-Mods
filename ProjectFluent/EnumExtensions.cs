using System;
using System.Collections.Generic;

namespace Shockah.ProjectFluent
{
	internal static class EnumExtensions
	{
		internal static IFluent<EnumType> ForEnum<EnumType>(this IFluent<string> self, string keyPrefix)
		{
			if (!typeof(EnumType).IsEnum)
				throw new ArgumentException($"{typeof(EnumType)} is not an enum.");
			return new EnumFluent<EnumType>(self, keyPrefix);
		}

		internal static EnumType GetFromLocalizedName<EnumType>(this IFluent<EnumType> self, string localizedName)
		{
			if (!typeof(EnumType).IsEnum)
				throw new ArgumentException($"{typeof(EnumType)} is not an enum.");
			foreach (var value in Enum.GetValues(typeof(EnumType)))
			{
				var valueLocalizedName = self[(EnumType)value];
				if (valueLocalizedName == localizedName)
					return (EnumType)value;
			}
			throw new ArgumentException($"{typeof(EnumType)} is not an enum.");
		}

		internal static IEnumerable<string> GetAllLocalizedNames<EnumType>(this IFluent<EnumType> self)
		{
			if (!typeof(EnumType).IsEnum)
				throw new ArgumentException($"{typeof(EnumType)} is not an enum.");
			foreach (var value in Enum.GetValues(typeof(EnumType)))
			{
				yield return self[(EnumType)value];
			}
		}
	}

	internal class EnumFluent<EnumType>: IFluent<EnumType>
	{
		private readonly IFluent<string> wrapped;
		private readonly string keyPrefix;

		public EnumFluent(IFluent<string> wrapped, string keyPrefix)
		{
			if (!typeof(EnumType).IsEnum)
				throw new ArgumentException($"{typeof(EnumType)} is not an enum.");
			this.wrapped = wrapped;
			this.keyPrefix = keyPrefix;
		}

		public string Get(EnumType key, object tokens)
		{
			return wrapped.Get($"{keyPrefix}{Enum.GetName(typeof(EnumType), key)}", tokens);
		}
	}
}