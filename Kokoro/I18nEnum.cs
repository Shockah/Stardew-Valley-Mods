using System;
using System.Collections.Generic;

namespace Shockah.Kokoro
{
	public static class I18nEnum
	{
		public static IEnumerable<string> GetTranslations<EnumType>(Func<EnumType, string> translationFunction) where EnumType : Enum
		{
			foreach (object value in Enum.GetValues(typeof(EnumType)))
				yield return translationFunction((EnumType)value);
		}

		public static EnumType? GetFromTranslation<EnumType>(string translation, Func<EnumType, string> translationFunction) where EnumType : Enum
		{
			foreach (object value in Enum.GetValues(typeof(EnumType)))
				if (translationFunction((EnumType)value) == translation)
					return (EnumType)value;
			return default;
		}
	}
}
