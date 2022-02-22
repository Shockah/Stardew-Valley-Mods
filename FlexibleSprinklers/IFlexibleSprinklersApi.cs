using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace Shockah.FlexibleSprinklers
{
	/// <summary>The API which provides access to Flexible Sprinklers for other mods.</summary>
	public interface IFlexibleSprinklersApi
	{
		/// <summary>
		/// Register a new sprinkler tier provider, to add support for Flexible Sprinklers for your custom tiered sprinklers in your mod or override existing ones.<br />
		/// This is only used for tiered sprinkler power config overrides (how many tiles they water).<br />
		/// Return `null` if you don't want to modify this specific tier.
		/// </summary>
		void RegisterSprinklerTierProvider(Func<SObject, int?> provider);

		/// <summary>
		/// Register a new sprinkler coverage provider, to add support for Flexible Sprinklers for your custom sprinklers in your mod or override existing ones.<br />
		/// Returned tile coverage should be relative.<br />
		/// Return `null` if you don't want to modify this specific coverage.
		/// </summary>
		void RegisterSprinklerCoverageProvider(Func<SObject, Vector2[]> provider);

		/// <summary>Activates sprinklers in a collective way, taking into account the Flexible Sprinklers mod behavior.</summary>
		void ActivateCollectiveSprinklersInLocation(GameLocation location);

		/// <summary>Activates a sprinkler, taking into account the Flexible Sprinklers mod behavior.</summary>
		void ActivateSprinkler(SObject sprinkler, GameLocation location);

		/// <summary>Returns the sprinkler's power after config modifications (that is, the number of tiles it will water).</summary>
		int GetSprinklerPower(SObject sprinkler);

		/// <summary>Returns a sprinkler's flood fill range (that is, how many tiles away will it look for tiles to water) for a given sprinkler power.</summary>
		int GetFloodFillSprinklerRange(int power);

		/// <summary>Get the relative tile coverage by supported sprinkler ID. This API is location/position-agnostic. Note that sprinkler IDs may change after a save is loaded due to Json Assets reallocating IDs.</summary>
		Vector2[] GetUnmodifiedSprinklerCoverage(SObject sprinkler);

		/// <summary>Get the relative tile coverage by supported sprinkler ID, modified by the Flexible Sprinklers mod. This API takes into consideration the location and position. Note that sprinkler IDs may change after a save is loaded due to Json Assets reallocating IDs.</summary>
		Vector2[] GetModifiedSprinklerCoverage(SObject sprinkler, GameLocation location);

		/// <summary>Returns whether a given tile is in range of the specified sprinkler.</summary>
		bool IsTileInRangeOfSprinkler(SObject sprinkler, GameLocation location, Vector2 tileLocation);

		/// <summary>Returns whether a given tile is in range of specified sprinklers.</summary>
		bool IsTileInRangeOfSprinklers(IEnumerable<SObject> sprinklers, GameLocation location, Vector2 tileLocation);
	}
}