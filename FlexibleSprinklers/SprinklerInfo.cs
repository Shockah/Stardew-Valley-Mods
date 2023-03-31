using Shockah.Kokoro;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	public readonly struct SprinklerInfo : IEquatable<SprinklerInfo>
	{
		public readonly IntRectangle OccupiedSpace { get; init; }
		public readonly IReadOnlySet<IntPoint> Coverage { get; init; }

		public int Power
			=> Coverage.Count;

		public SprinklerInfo(IntRectangle occupiedSpace, IReadOnlySet<IntPoint> coverage)
		{
			this.OccupiedSpace = occupiedSpace;
			this.Coverage = coverage.ToHashSet();
		}

		public void Deconstruct(out IntRectangle occupiedSpace, out IReadOnlySet<IntPoint> coverage)
		{
			occupiedSpace = OccupiedSpace;
			coverage = Coverage;
		}

		public bool Equals(SprinklerInfo other)
			=> OccupiedSpace == other.OccupiedSpace && Coverage.SetEquals(other.Coverage);

		public override bool Equals(object? obj)
			=> obj is SprinklerInfo info && Equals(info);

		public override int GetHashCode()
			=> (OccupiedSpace, Coverage).GetHashCode();

		public static bool operator ==(SprinklerInfo left, SprinklerInfo right)
			=> left.Equals(right);

		public static bool operator !=(SprinklerInfo left, SprinklerInfo right)
			=> !(left == right);

		public static SprinklerInfo CreateBasic(IntPoint position)
			=> new(new(position), SprinklerLayouts.Basic.Select(p => position + p).ToHashSet());

		public static SprinklerInfo CreateQuality(IntPoint position)
			=> new(new(position), SprinklerLayouts.Quality.Select(p => position + p).ToHashSet());

		public static SprinklerInfo CreateIridium(IntPoint position)
			=> new(new(position), SprinklerLayouts.Iridium.Select(p => position + p).ToHashSet());

		public static SprinklerInfo CreateIridiumWithPressureNozzle(IntPoint position)
			=> new(new(position), SprinklerLayouts.IridiumWithPressureNozzle.Select(p => position + p).ToHashSet());
	}
}