using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.PleaseGiftMeInPerson
{
	internal class OverrideAssetLoader: IAssetLoader
	{
		public bool CanLoad<T>(IAssetInfo asset)
			=> asset.AssetNameEquals(PleaseGiftMeInPerson.OverrideAssetPath);

		public T Load<T>(IAssetInfo asset)
			=> (T)(object)new Dictionary<string, string>();
	}
}
