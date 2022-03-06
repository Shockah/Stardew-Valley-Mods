using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Shockah.PleaseGiftMeInPerson
{
	internal class ModConfig
	{
		public enum ReturningBehavior { Never, NormallyLiked, Always }

		public class Entry: IEquatable<Entry>
		{
			public int GiftsToRemember { get; set; }
			public int DaysToRemember { get; set; }
			public int MailsUntilDislike { get; set; }
			public int MailsUntilHate { get; set; }
			public int MailsUntilLike { get; set; }
			public int MailsUntilLove { get; set; }

			[JsonConstructor]
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

			public Entry(Entry other): this(
				giftsToRemember: other.GiftsToRemember,
				daysToRemember: other.DaysToRemember,
				mailsUntilDislike: other.MailsUntilDislike,
				mailsUntilHate: other.MailsUntilHate,
				mailsUntilLike: other.MailsUntilLike,
				mailsUntilLove: other.MailsUntilLove
			)
			{
			}

			public void CopyFrom(Entry other)
			{
				this.GiftsToRemember = other.GiftsToRemember;
				this.DaysToRemember = other.DaysToRemember;
				this.MailsUntilDislike = other.MailsUntilDislike;
				this.MailsUntilHate = other.MailsUntilHate;
				this.MailsUntilLike = other.MailsUntilLike;
				this.MailsUntilLove = other.MailsUntilLove;
			}

			public bool Equals(Entry? other)
				=> other is not null
				&& GiftsToRemember == other.GiftsToRemember
				&& DaysToRemember == other.DaysToRemember
				&& MailsUntilDislike == other.MailsUntilDislike
				&& MailsUntilHate == other.MailsUntilHate
				&& MailsUntilLike == other.MailsUntilLike
				&& MailsUntilLove == other.MailsUntilLove;

			public override bool Equals(object? obj)
				=> obj is Entry entry && Equals(entry);

			public override int GetHashCode()
				=> (GiftsToRemember, DaysToRemember, MailsUntilDislike, MailsUntilHate, MailsUntilLike, MailsUntilLove).GetHashCode();

			public static bool operator ==(Entry lhs, Entry rhs)
				=> lhs.Equals(rhs);

			public static bool operator !=(Entry lhs, Entry rhs)
				=> !lhs.Equals(rhs);
		}

		public Entry Default { get; set; } = new(
			giftsToRemember: 5,
			daysToRemember: 14,
			mailsUntilDislike: 2,
			mailsUntilHate: 3
		);

		public IDictionary<string, Entry> PerNPC { get; set; } = new Dictionary<string, Entry>();

		public ReturningBehavior ReturnUnlikedItems { get; set; } = ReturningBehavior.NormallyLiked;
		public bool ReturnMailsInCollection { get; set; } = true;

		public ModConfig()
		{
		}

		public ModConfig(ModConfig other) : this()
		{
			Default.CopyFrom(other.Default);
			foreach (var (name, entry) in other.PerNPC)
				PerNPC[name] = new(entry);
		}

		public Entry GetForNPC(string npcName)
		{
			if (!PerNPC.TryGetValue(npcName, out var entry))
				entry = Default;
			return entry;
		}
	}
}