using Microsoft.Xna.Framework;
using Shockah.CommonModCode;
using Shockah.CommonModCode.Stardew;
using StardewValley;
using StardewValley.Objects;
using System.Linq;

namespace Shockah.AdventuresInTheMines
{
	public interface ITreasureGenerator
	{
		void GenerateTreasure(GameLocation location, IntPoint position, bool pregenerated);
	}

	public sealed class LootChestTreasureGenerator : ITreasureGenerator
	{
		private ILootProvider LootProvider { get; init; }

		public LootChestTreasureGenerator(ILootProvider lootProvider)
		{
			this.LootProvider = lootProvider;
		}

		public void GenerateTreasure(GameLocation location, IntPoint position, bool pregenerated)
		{
			// create chest
			location.RemoveAllPlaceables(position);
			Vector2 positionVector = new(position.X, position.Y);
			location.objects[positionVector] = new Chest(0, LootProvider.GenerateLoot().ToList(), positionVector);

			if (pregenerated)
				return;

			// making sound
			location.localSound("newArtifact");

			// poof
			var sprite = new TemporaryAnimatedSprite(
				textureName: Game1.mouseCursorsName,
				sourceRect: new Rectangle(464, 1792, 16, 16),
				animationInterval: 120f,
				animationLength: 5,
				numberOfLoops: 0,
				position: positionVector * Game1.tileSize,
				flicker: false,
				flipped: Game1.random.NextBool(),
				layerDepth: 1f,
				alphaFade: 0.01f,
				color: Color.White,
				scale: Game1.pixelZoom,
				scaleChange: 0.01f,
				rotation: 0f,
				rotationChange: 0f
			)
			{
				light = true
			};
			GameExt.Multiplayer.broadcastSprites(location, sprite);
		}
	}
}