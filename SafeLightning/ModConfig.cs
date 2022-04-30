namespace Shockah.SafeLightning
{
	internal class ModConfig
	{
		public bool SafeTiles { get; set; } = true;
		public bool SafeFruitTrees { get; set; } = true;
		public BigLightningBehavior BigLightningBehavior { get; set; } = BigLightningBehavior.WhenSupposedToStrike;
	}
}