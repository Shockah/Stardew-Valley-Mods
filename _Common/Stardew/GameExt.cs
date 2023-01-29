using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.CommonModCode.Stardew
{
    public enum MultiplayerMode { SinglePlayer, Client, Server }
	
	public static class GameExt
	{
		private static readonly Lazy<Texture2D> LazyPixel = new(() =>
		{
			var texture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
			texture.SetData(new[] { Color.White });
			return texture;
		});

		public static Texture2D Pixel
			=> LazyPixel.Value;

		public static MultiplayerMode GetMultiplayerMode()
			=> (MultiplayerMode)Game1.multiplayerMode;

		public static Farmer GetHostPlayer()
			=> Game1.getAllFarmers().First(p => p.slotCanHost);

		public static IReadOnlyList<GameLocation> GetAllLocations()
		{
			List<GameLocation> locations = new();
			Utility.ForAllLocations(l => locations.Add(l));
			foreach (var player in Game1.getAllFarmers())
				if (!locations.Contains(player.currentLocation))
					locations.Add(player.currentLocation);
			return locations;
		}
	}
}
