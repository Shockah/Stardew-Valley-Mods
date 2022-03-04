using System.Collections.Generic;

namespace Shockah.PleaseGiftMeInPerson
{
	internal class ModConfig
	{
		internal struct Entry
		{
			public int GiftsToRemember { get; set; }
			public int DaysToRemember { get; set; }
			public int MailsUntilDislike { get; set; }
			public int MailsUntilHate { get; set; }
			public int MailsUntilLike { get; set; }
			public int MailsUntilLove { get; set; }

			public Entry(
				int giftsToRemember,
				int daysToRemember,
				int mailsUntilDislike,
				int mailsUntilHate,
				int mailsUntilLike = -1,
				int mailsUntilLove = -1
			)
			{
				this.GiftsToRemember = giftsToRemember;
				this.DaysToRemember = daysToRemember;
				this.MailsUntilDislike = mailsUntilDislike;
				this.MailsUntilHate = mailsUntilHate;
				this.MailsUntilLike = mailsUntilLike;
				this.MailsUntilLove = mailsUntilLove;
			}
		}

		public Entry Default { get; set; } = new(
			giftsToRemember: 10,
			daysToRemember: 28,
			mailsUntilDislike: 2,
			mailsUntilHate: 3
		);

		public IDictionary<string, Entry> PerNPC { get; set; } = new Dictionary<string, Entry>();
	}
}