using Shockah.CommonModCode.UI;

namespace Shockah.XPDisplay
{
	public sealed class ModConfig
	{
		public Orientation SmallBarOrientation { get; set; } = Orientation.Vertical;
		public Orientation BigBarOrientation { get; set; } = Orientation.Horizontal;
		public float Alpha { get; set; } = 0.6f;
	}
}
