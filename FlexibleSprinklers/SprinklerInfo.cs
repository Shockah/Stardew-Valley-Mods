using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.FlexibleSprinklers
{
	public struct SprinklerInfo: IEquatable<SprinklerInfo>
	{
		public ISet<Vector2> Layout { get; set; }

		public int Power { get; set; }

		public SprinklerInfo(ISet<Vector2> layout): this(layout, layout.Count) { }

		public SprinklerInfo(ISet<Vector2> layout, int power)
		{
			this.Layout = layout;
			this.Power = power;
		}

		public bool Equals(SprinklerInfo other)
			=> Power == other.Power && Layout.SetEquals(other.Layout);

		public override bool Equals(object? obj)
			=> obj is SprinklerInfo info && Equals(info);

		public override int GetHashCode()
			=> (Layout.Count, Power).GetHashCode();

		public static bool operator ==(SprinklerInfo left, SprinklerInfo right)
			=> left.Equals(right);

		public static bool operator !=(SprinklerInfo left, SprinklerInfo right)
			=> !(left == right);
	}
}