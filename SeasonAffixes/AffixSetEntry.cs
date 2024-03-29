﻿namespace Shockah.SeasonAffixes;

public record AffixSetEntry(
	int Positive = 0,
	int Negative = 0,
	double Weight = 0
)
{
	internal bool IsValid()
		=> Positive >= 0 && Negative >= 0 && (Positive + Negative) >= 0 && Weight > 0;
}