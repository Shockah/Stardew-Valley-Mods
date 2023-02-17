using StardewModdingAPI;

namespace Shockah.Kokoro
{
	public class Kokoro : BaseMod
	{
		public static Kokoro Instance { get; private set; } = null!;

		public override void Entry(IModHelper helper)
		{
			Instance = this;
		}
	}
}