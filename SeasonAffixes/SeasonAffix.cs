namespace Shockah.SeasonAffixes;

internal abstract class BaseSeasonAffix
{
	protected static ModEntry Mod
		=> ModEntry.Instance;

	public string UniqueID { get; init; }
	protected string I18nPrefix { get; init; }

	public virtual string LocalizedName => Mod.Helper.Translation.Get($"{I18nPrefix}.name");

	protected BaseSeasonAffix(string shortUniqueID, string affixType)
	{
		this.UniqueID = $"{Mod.ModManifest.UniqueID}.{shortUniqueID}";
		this.I18nPrefix = $"affix.{affixType}.{shortUniqueID}";
	}

	public override bool Equals(object? obj)
		=> obj is ISeasonAffix affix && UniqueID == affix.UniqueID;

	public override int GetHashCode()
		=> UniqueID.GetHashCode();
}

internal abstract class BaseVariantedSeasonAffix : BaseSeasonAffix
{
	public AffixVariant Variant { get; init; }

	protected BaseVariantedSeasonAffix(string shortUniqueID, AffixVariant variant) : base(shortUniqueID, variant == AffixVariant.Positive ? "positive" : "negative")
	{
		this.Variant = variant;
	}
}