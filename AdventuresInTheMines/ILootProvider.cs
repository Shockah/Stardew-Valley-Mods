using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;

namespace Shockah.AdventuresInTheMines
{
	public interface ILootProvider
	{
		IReadOnlyList<Item> GenerateLoot();
	}

	public sealed class DefaultMineShaftLootProvider : ILootProvider
	{
		public IReadOnlyList<Item> GenerateLoot()
			=> new[] { MineShaft.getTreasureRoomItem() };
	}

	public sealed class BirthdayPresentLootProvider : ILootProvider
	{
		private WorldDate Date { get; init; }

		public BirthdayPresentLootProvider(WorldDate date)
		{
			this.Date = date;
		}

		public IReadOnlyList<Item> GenerateLoot()
		{
			var birthdayNpc = Utility.getTodaysBirthdayNPC(Date.Season, Date.DayOfMonth);
			if (birthdayNpc is null)
				return Array.Empty<Item>();

			var favoriteItem = birthdayNpc.getFavoriteItem();
			if (favoriteItem is null)
				return Array.Empty<Item>();

			return new[] { favoriteItem };
		}
	}

	public sealed class LimitedWithAlternativeLootProvider : ILootProvider
	{
		private ILootProvider Main { get; init; }
		private ILootProvider Alternative { get; init; }
		private int CountLeft { get; set; }

		public LimitedWithAlternativeLootProvider(ILootProvider main, ILootProvider alternative, int limit = 1)
		{
			this.Main = main;
			this.Alternative = alternative;
			this.CountLeft = limit;
		}

		public IReadOnlyList<Item> GenerateLoot()
		{
			if (CountLeft > 0)
			{
				var mainLoot = Main.GenerateLoot();
				if (mainLoot.Count != 0)
				{
					CountLeft--;
					return mainLoot;
				}
			}
			return Alternative.GenerateLoot();
		}
	}
}