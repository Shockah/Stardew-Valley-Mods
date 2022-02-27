using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using SObject = StardewValley.Object;

namespace Shockah.CommonModCode.UI
{
	public class ItemRenderer
	{
		private Texture2D GetItemTexture(SObject @object)
		{
			if (@object.bigCraftable.Value)
				return Game1.bigCraftableSpriteSheet;
			else
				return Game1.objectSpriteSheet;
		}

		private Rectangle GetItemSourceRectangle(SObject @object)
		{
			var index = @object.ParentSheetIndex;
			if (@object.showNextIndex.Value)
				index++;
			if (@object.bigCraftable.Value)
				return SObject.getSourceRectForBigCraftable(index);
			else
				return Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, index, 16, 16);
		}

		public void DrawItem(SpriteBatch batch, SObject @object, Vector2 rectLocation, Vector2 rectSize, Color color, UIAnchorSide rectAnchorSide = UIAnchorSide.TopLeft, float layerDepth = 0f)
		{
			var texture = GetItemTexture(@object);
			var sourceRectangle = GetItemSourceRectangle(@object);

			var itemSize = new Vector2(sourceRectangle.Size.X, sourceRectangle.Size.Y);
			if (itemSize.X < rectSize.X && itemSize.Y < rectSize.Y)
				itemSize *= Math.Max(rectSize.X, rectSize.Y);
			if (itemSize.X > rectSize.X)
				itemSize = itemSize / itemSize.X * rectSize.X;
			if (itemSize.Y > rectSize.Y)
				itemSize = itemSize / itemSize.Y * rectSize.Y;
			var itemPosition = rectAnchorSide.GetAnchorPoint(
				new Vector2(
					rectLocation.X + (rectSize.X - itemSize.X) * 0.5f,
					rectLocation.Y + (rectSize.Y - itemSize.Y) * 0.5f
				),
				-rectSize
			);
			var scale = itemSize.X / sourceRectangle.Width;
			batch.Draw(texture, itemPosition, sourceRectangle, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
		}
	}
}
