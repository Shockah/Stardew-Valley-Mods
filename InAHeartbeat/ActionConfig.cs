using Newtonsoft.Json;
using System;

namespace Shockah.InAHeartbeat
{
	public sealed class ActionConfig
	{
		[JsonProperty] public int RegularFriendship { get; internal set; }
		[JsonProperty] public int SilverFriendship { get; internal set; }
		[JsonProperty] public int GoldFriendship { get; internal set; }
		[JsonProperty] public int IridiumFriendship { get; internal set; }

		[JsonIgnore]
		public int MinFriendship => Math.Min(Math.Min(RegularFriendship, SilverFriendship), Math.Min(GoldFriendship, IridiumFriendship));

		public ActionConfig(int regularFriendship, int silverFriendship, int goldFriendship, int iridiumFriendship)
		{
			this.RegularFriendship = regularFriendship;
			this.SilverFriendship = silverFriendship;
			this.GoldFriendship = goldFriendship;
			this.IridiumFriendship = iridiumFriendship;
		}
	}
}