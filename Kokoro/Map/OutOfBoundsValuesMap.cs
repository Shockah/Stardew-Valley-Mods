using System;

namespace Shockah.Kokoro.Map;

public sealed class OutOfBoundsValuesMap<TTile> : IMap<TTile>.WithKnownSize
{
	public TTile this[IntPoint point]
	{
		get
		{
			if (KnownSizeMap.Bounds.Contains(point))
				return KnownSizeMap[point];
			else
				return OutOfBoundsProvider(point);
		}
	}

	public IntRectangle Bounds
		=> KnownSizeMap.Bounds;

	private readonly IMap<TTile>.WithKnownSize KnownSizeMap;
	private readonly Func<IntPoint, TTile> OutOfBoundsProvider;

	public OutOfBoundsValuesMap(IMap<TTile>.WithKnownSize knownSizeMap, TTile outOfBoundsDefaultTile) : this(knownSizeMap, _ => outOfBoundsDefaultTile) { }

	public OutOfBoundsValuesMap(IMap<TTile>.WithKnownSize knownSizeMap, Func<IntPoint, TTile> outOfBoundsProvider)
	{
		this.KnownSizeMap = knownSizeMap;
		this.OutOfBoundsProvider = outOfBoundsProvider;
	}
}