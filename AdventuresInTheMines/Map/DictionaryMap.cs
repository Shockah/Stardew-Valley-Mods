using Shockah.CommonModCode;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.AdventuresInTheMines.Map
{
	internal sealed class DictionaryMap<TTile> : IMap<TTile>.Writable
	{
		public TTile this[IntPoint point]
		{
			get
			{
				if (Dictionary.TryGetValue(point, out var value))
					return value;
				return DefaultTile(point);
			}
			set
			{
				Dictionary[point] = value;
			}
		}

		private readonly Dictionary<IntPoint, TTile> Dictionary = new();
		private readonly Func<IntPoint, TTile> DefaultTile;

		public DictionaryMap(TTile defaultTile) : this(_ => defaultTile) { }

		public DictionaryMap(Func<IntPoint, TTile> defaultTile)
		{
			this.DefaultTile = defaultTile;
		}

		public override bool Equals(object? obj)
		{
			if (obj is not DictionaryMap<TTile> other)
				return false;
			if (!other.Dictionary.ToHashSet().SequenceEqual(Dictionary.ToHashSet()))
				return false;
			return true;
		}

		public DictionaryMap<TTile> Clone()
		{
			DictionaryMap<TTile> clone = new(DefaultTile);
			foreach (var (point, tile) in Dictionary)
				clone.Dictionary[point] = tile;
			return clone;
		}
	}
}