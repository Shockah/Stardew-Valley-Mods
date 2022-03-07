using StardewModdingAPI;

namespace Shockah.PleaseGiftMeInPerson
{
	internal class OverrideAssetEditor: IAssetEditor
	{
		public bool CanEdit<T>(IAssetInfo asset)
			=> asset.AssetNameEquals(PleaseGiftMeInPerson.OverrideAssetPath);

		public void Edit<T>(IAssetData asset)
		{
			if (!asset.AssetNameEquals(PleaseGiftMeInPerson.OverrideAssetPath))
				return;
			var dictionaryAsset = asset.AsDictionary<string, string>();
			dictionaryAsset.Data["Dwarf"] = $"{GiftPreference.Neutral}/{GiftPreference.Hates}";
			dictionaryAsset.Data["Elliott"] = $"{GiftPreference.Neutral}/{GiftPreference.Neutral}";
			dictionaryAsset.Data["Krobus"] = $"{GiftPreference.Neutral}/{GiftPreference.Hates}";
			dictionaryAsset.Data["Leo"] = $"{GiftPreference.Neutral}/{GiftPreference.LovesInfrequent}";
			dictionaryAsset.Data["Linus"] = $"{GiftPreference.Neutral}/{GiftPreference.DislikesAndHatesFrequent}";
			dictionaryAsset.Data["Penny"] = $"{GiftPreference.Neutral}/{GiftPreference.Neutral}";
			dictionaryAsset.Data["Sandy"] = $"{GiftPreference.LikesInfrequent}/{GiftPreference.LikesInfrequentButDislikesFrequent}";
			dictionaryAsset.Data["Sebastian"] = $"{GiftPreference.Dislikes}/{GiftPreference.Neutral}";
			dictionaryAsset.Data["Wizard"] = $"{GiftPreference.DislikesFrequent}/{GiftPreference.Neutral}";
		}
	}
}
