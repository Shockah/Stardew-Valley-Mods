using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Shockah.XPDisplay.IMargoAPI.IModConfig;

namespace Shockah.XPDisplay.WalkOfLife
{
	public interface IProfessionsApi
	{
		/// <summary>Gets the mod's current config schema.</summary>
		/// <returns>The current <see cref="IProfessionsConfig"/> instance.</returns>
		IProfessionsConfig GetConfig();
	}
}
