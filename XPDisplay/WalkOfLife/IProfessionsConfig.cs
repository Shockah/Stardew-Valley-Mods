using Newtonsoft.Json;
using StardewModdingAPI.Utilities;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shockah.XPDisplay.WalkOfLife
{
	public interface IProfessionsConfig
	{

		/// <inheritdoc cref="IMasteriesConfig"/>
		public IMasteriesConfig Masteries { get; }
	}
}