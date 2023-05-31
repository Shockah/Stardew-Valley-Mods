using StardewValley;
using System;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.Kokoro.Stardew
{
	public static class FarmerExt
	{
		public static bool ConsumeItem(this Farmer player, Item item, int? amount = null, bool exactQuality = true)
		{
			bool IsRequestedItem(Item? inventoryItem, bool exactQuality)
			{
				if (inventoryItem is null)
					return false;
				if (!inventoryItem.IsSameItem(item, ignoringAmount: true))
					return false;

				if (item is SObject @object && inventoryItem is SObject inventoryObject)
				{
					if (exactQuality)
					{
						if (inventoryObject.Quality != @object.Quality)
							return false;
					}
					else
					{
						if (inventoryObject.Quality < @object.Quality)
							return false;
					}
				}

				return true;
			}

			void RemoveAmount(int index, int amount)
			{
				if (amount > player.Items[index].Stack)
					throw new ArgumentException("Not enough of an item to remove.");

				if (player.Items[index].Stack == amount)
					player.Items[index] = null;
				else
					player.Items[index].Stack -= amount;
			}

			int amountLeft = amount ?? item.Stack;

			var itemsPossibleToRemove = Enumerable.Range(0, player.Items.Count)
				.Where(i => IsRequestedItem(player.Items[i], exactQuality))
				.OrderBy(i => player.Items[i] is SObject inventoryObject ? inventoryObject.Quality : SObject.lowQuality)
				.ThenBy(i => player.Items[i]!.Stack)
				.ToList();

			foreach (var i in itemsPossibleToRemove)
			{
				if (player.Items[i]!.Stack == amountLeft)
				{
					RemoveAmount(i, amountLeft);
					return true;
				}
			}

			if (itemsPossibleToRemove.Sum(i => player.Items[i]!.Stack) < amountLeft)
				return false;

			foreach (var i in itemsPossibleToRemove)
			{
				int amountToRemove = Math.Min(amountLeft, player.Items[i]!.Stack);
				RemoveAmount(i, amountToRemove);
				amountLeft -= amountToRemove;
				if (amountLeft <= 0)
					return true;
			}

			throw new InvalidOperationException("Invalid state.");
		}
	}
}