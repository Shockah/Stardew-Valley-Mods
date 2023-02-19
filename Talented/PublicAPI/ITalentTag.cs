using Shockah.Kokoro.Stardew.Skill;
using Shockah.Kokoro.UI;

namespace Shockah.Talented
{
	public interface ITalentTag
	{
		string UniqueID { get; }
		TextureRectangle Icon { get; }
		string Name { get; }
		ITalentTag? Parent => null;
		ISkill? Skill => null;
	}
}