using StardewValley;
using System.Collections.Generic;

namespace Shockah.PleaseGiftMeInPerson;

public interface IFreeLoveApi
{
	public Dictionary<string, NPC> GetSpouses(Farmer farmer, bool all = true);
}