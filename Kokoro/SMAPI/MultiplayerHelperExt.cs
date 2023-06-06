using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;

namespace Shockah.Kokoro.SMAPI;

public static class MultiplayerHelperExt
{
	public static Farmer GetPlayer(this IMultiplayerPeer peer)
		=> Game1.getAllFarmers().First(p => p.UniqueMultiplayerID == peer.PlayerID);

	public static Farmer GetPlayer(this ModMessageReceivedEventArgs args)
		=> Game1.getAllFarmers().First(p => p.UniqueMultiplayerID == args.FromPlayerID);
}