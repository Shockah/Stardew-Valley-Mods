using Shockah.CommonModCode.Stardew;
using StardewValley.Locations;
using System;
using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines.Config
{
	public enum MineType
	{
		Earth, Frost, Lava, SkullCavern
	}

	public record MineLevelConditions(
		ISet<MineType>? MineTypes = null,
		bool? Dangerous = null,
		bool? DarkArea = null,
		bool? MonsterArea = null
	)
	{
		public MineLevelConditions(
			MineType MineType,
			bool? Dangerous = null,
			bool? DarkArea = null,
			bool? MonsterArea = null
		) : this(new HashSet<MineType>() { MineType }, Dangerous, DarkArea, MonsterArea ) { }

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
}