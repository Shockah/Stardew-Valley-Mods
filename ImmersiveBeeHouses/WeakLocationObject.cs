using StardewValley;
using System;
using System.Diagnostics.CodeAnalysis;
using SObject = StardewValley.Object;

namespace Shockah.ImmersiveBeeHouses
{
	internal readonly struct WeakLocationObject : IEquatable<WeakLocationObject>
	{
		internal readonly WeakReference<GameLocation> Location { get; private init; }
		internal readonly WeakReference<SObject> Object { get; private init; }

		public bool IsValid
			=> Location.TryGetTarget(out _) && Object.TryGetTarget(out _);

		public WeakLocationObject(GameLocation location, SObject @object)
		{
			this.Location = new(location);
			this.Object = new(@object);
		}

		public bool Equals(WeakLocationObject other)
		{
			bool hasLocation1 = Location.TryGetTarget(out var location1);
			bool hasLocation2 = other.Location.TryGetTarget(out var location2);
			if (hasLocation1 != hasLocation2)
				return false;
			if (location1 != location2)
				return false;

			bool hasObject1 = Object.TryGetTarget(out var object1);
			bool hasObject2 = other.Object.TryGetTarget(out var object2);
			if (hasObject1 != hasObject2)
				return false;
			if (object1 != object2)
				return false;

			return true;
		}

		public override bool Equals(object? obj)
			=> obj is WeakLocationObject @object && Equals(@object);

		public override int GetHashCode()
			=> (Location, Object).GetHashCode();

		public static bool operator ==(WeakLocationObject left, WeakLocationObject right)
			=> left.Equals(right);

		public static bool operator !=(WeakLocationObject left, WeakLocationObject right)
			=> !(left == right);

		public bool TryGet([NotNullWhen(true)] out GameLocation? location, [NotNullWhen(true)] out SObject? @object)
		{
			if (!Location.TryGetTarget(out location))
			{
				location = null;
				@object = null;
				return false;
			}
			if (!Object.TryGetTarget(out @object))
			{
				location = null;
				@object = null;
				return false;
			}
			return true;
		}
	}
}