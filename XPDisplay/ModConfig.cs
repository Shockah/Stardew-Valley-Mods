namespace Shockah.XPView
{
	internal class ModConfig
	{
		public enum Orientation { Horizontal, Vertical }

		public Orientation SmallBarOrientation { get; set; } = Orientation.Vertical;
		public Orientation BigBarOrientation { get; set; } = Orientation.Horizontal;
		public float Alpha { get; set; } = 0.6f;
	}
}
