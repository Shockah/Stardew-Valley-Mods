using Newtonsoft.Json;
using Shockah.CommonModCode.Stardew;
using StardewValley;
using System;

namespace Shockah.PleaseGiftMeInPerson
{
	internal enum GiftMethod { InPerson, ByMail }
	
	internal struct GiftEntry
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
	}
}