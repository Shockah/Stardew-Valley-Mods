using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shockah.PleaseGiftMeInPerson
{
	internal static class NetMessage
	{
		public struct RecordGift
		{
			public long PlayerID { get; set; }
			public string NpcName { get; set; }
			public GiftEntry GiftEntry { get; set; }

			public RecordGift(long playerID, string npcName, GiftEntry giftEntry)
			{
				this.PlayerID = playerID;
				this.NpcName = npcName;
				this.GiftEntry = giftEntry;
			}
		}
	}
}
