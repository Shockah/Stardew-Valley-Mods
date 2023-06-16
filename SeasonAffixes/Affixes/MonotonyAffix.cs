using Shockah.Kokoro.UI;
using StardewValley;

namespace Shockah.SeasonAffixes;

internal sealed class MonotonyAffix : BaseSeasonAffix, ISeasonAffix
{
	private static string ShortID => "Monotony";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description");
	public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(128, 16, 16, 16));

	public MonotonyAffix() : base(ShortID, "neutral") { }

	public int GetPositivity(OrdinalSeason season)
		=> 0;

	public int GetNegativity(OrdinalSeason season)
		=> 0;

	public double GetProbabilityWeight(OrdinalSeason season)
		=> 0;
}