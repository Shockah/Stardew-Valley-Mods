using Newtonsoft.Json;
using Shockah.CommonModCode.Stardew;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.AdventuresInTheMines.Config
{
	public enum MineType
	{
		Earth, Frost, Lava, SkullCavern
	}

	public record MineLevelConditions(
		[property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] ISet<MineType>? MineTypes = null,
		[property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] bool? Dangerous = null,
		[property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] bool? DarkArea = null,
		[property: JsonProperty(NullValueHandling = NullValueHandling.Ignore)] bool? MonsterArea = null
	)
	{
		public bool Matches(MineShaft location)
		{
			if (location.mineLevel == MineShaft.quarryMineShaft)
				return false;
			if (location.mineLevel <= MineShaft.bottomOfMineLevel && location.mineLevel % 10 == 0)
				return false;

			if (MineTypes is not null)
			{
				MineType mineType = location.mineLevel switch
				{
					>= 0 and < MineShaft.frostArea => MineType.Earth,
					>= MineShaft.frostArea and < MineShaft.lavaArea => MineType.Frost,
					>= MineShaft.lavaArea and <= MineShaft.bottomOfMineLevel => MineType.Lava,
					> MineShaft.bottomOfMineLevel => MineType.SkullCavern,
					_ => throw new ArgumentException($"Invalid mine level {location.mineLevel}.")
				};
				if (!MineTypes.Contains(mineType))
					return false;
			}
			if (Dangerous is not null)
				if (Dangerous.Value != location.GetAdditionalDifficulty() > 0)
					return false;
			if (DarkArea is not null)
				if (DarkArea.Value != location.isDarkArea())
					return false;
			if (MonsterArea is not null)
				if (MonsterArea.Value != (location.isLevelSlimeArea() || location.IsMonsterArea()))
					return false;

			return true;
		}
	}

	public record MineLevelConditionedConfig<T>(
		T Value,
		IList<MineLevelConditions> Conditions
	);

	public static class MineLevelConditionedConfigListClassExt
	{
		public static T? GetMatchingConfig<T>(this IList<MineLevelConditionedConfig<T>> list, MineShaft location)
			where T : class
		{
			foreach (var entry in list)
			{
				if (entry.Conditions.Any(c => c.Matches(location)))
					return entry.Value;
			}
			return null;
		}
	}

	public static class MineLevelConditionedConfigListStructExt
	{
		public static T? GetMatchingConfig<T>(this IList<MineLevelConditionedConfig<T>> list, MineShaft location)
			where T : struct
		{
			foreach (var entry in list)
			{
				if (entry.Conditions.Any(c => c.Matches(location)))
					return entry.Value;
			}
			return null;
		}
	}
}