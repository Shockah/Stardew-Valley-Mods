using Shockah.Kokoro;
using System;

namespace Shockah.JunimoWarp
{
	internal static class NetMessage
	{
		public record NextWarpRequest(
			Guid ID,
			string LocationName,
			IntPoint Point
		);

		public record NextWarpResponse(
			Guid ID,
			string LocationName,
			IntPoint Point
		);
	}
}