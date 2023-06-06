using Shockah.Kokoro.GMCM;
using System;

namespace Shockah.CommonModCode.GMCM.Helper;

public static class MultiPageLinkOptionForHelper
{
	public static void AddMultiPageLinkOption<T>(
		this GMCMI18nHelper helper,
		string keyPrefix,
		Func<T, string> pageID,
		Func<float, int> columns,
		T[] pageValues,
		Func<T, string>? pageName = null,
		object? tokens = null
	)
	{
		helper.Api.AddMultiPageLinkOption(
			mod: helper.Mod,
			name: () => helper.Translations.Get($"{keyPrefix}.name", tokens),
			tooltip: helper.GetOptionalTranslatedStringDelegate($"{keyPrefix}.tooltip", tokens),
			pageID: pageID,
			pageName: pageName ?? (pageValue => helper.Translations.Get($"{keyPrefix}.value.{pageValue}", tokens)),
			columns: columns,
			pageValues: pageValues
		);
	}
}
