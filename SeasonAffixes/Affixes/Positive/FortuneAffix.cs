namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class FortuneAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Fortune";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public AffixScore Score => AffixScore.Positive;

		public FortuneAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		// TODO: Fortune implementation
	}
}