using Newtonsoft.Json;
using Shockah.CommonModCode.Stardew;
using StardewValley;
using System;

namespace Shockah.PleaseGiftMeInPerson
{
	internal enum GiftMethod { InPerson, ByMail }
	
	internal struct GiftEntry: IEquatable<GiftEntry>
	{
		public int Year { get; set; }
		public int SeasonIndex { get; set; }
		public int DayOfMonth { get; set; }
		public GiftTaste GiftTaste { get; set; }
		public GiftMethod GiftMethod { get; set; }

		[JsonIgnore]
		public Season Season
			=> (Season)SeasonIndex;

		[JsonIgnore]
		public WorldDate Date
			=> new(Year, Enum.GetName(Season)?.ToLower(), DayOfMonth);

		public GiftEntry(int year, int seasonIndex, int dayOfMonth, GiftTaste giftTaste, GiftMethod giftMethod)
		{
			this.Year = year;
			this.SeasonIndex = seasonIndex;
			this.DayOfMonth = dayOfMonth;
			this.GiftTaste = giftTaste;
			this.GiftMethod = giftMethod;
		}

		public GiftEntry(WorldDate date, GiftTaste giftTaste, GiftMethod giftMethod)
			: this(date.Year, date.SeasonIndex, date.DayOfMonth, giftTaste, giftMethod)
		{
		}

		public bool Equals(GiftEntry other)
			=> Year == other.Year && SeasonIndex == other.SeasonIndex && DayOfMonth == other.DayOfMonth && GiftTaste == other.GiftTaste && GiftMethod == other.GiftMethod;

		public override bool Equals(object? obj)
			=> obj is GiftEntry entry && Equals(entry);

		public override int GetHashCode()
			=> (Year, SeasonIndex, DayOfMonth, GiftTaste, GiftMethod).GetHashCode();
	}
}