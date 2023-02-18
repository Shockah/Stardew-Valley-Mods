using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;
using System;

namespace Shockah.Talented.UI
{
	internal sealed class TalentTagButton : ClickableComponent
	{
		public enum DisplayStyleEnum
		{
			Normal,
			Hovered,
			Deselected
		}

		public ITalentTag Tag { get; private init; }
		public DisplayStyleEnum DisplayStyle { get; set; } = DisplayStyleEnum.Normal;

		public TalentTagButton(Rectangle bounds, ITalentTag tag) : base(bounds, tag.Name)
		{
			this.Tag = tag;
		}

		public void Draw(SpriteBatch b)
		{
			var color = DisplayStyle switch
			{
				DisplayStyleEnum.Normal or DisplayStyleEnum.Hovered => Color.White,
				DisplayStyleEnum.Deselected => Color.Gray,
				_ => throw new ArgumentException($"{nameof(DisplayStyleEnum)} has an invalid value.")
			};
			var size = DisplayStyle switch
			{
				DisplayStyleEnum.Normal or DisplayStyleEnum.Deselected => new Vector2(bounds.Width, bounds.Height),
				DisplayStyleEnum.Hovered => new Vector2(bounds.Width + 8, bounds.Height + 8),
				_ => throw new ArgumentException($"{nameof(DisplayStyleEnum)} has an invalid value.")
			};

			var icon = Tag.Icon;
			float scale = 1f;
			if (icon.Rectangle.Width * scale < size.X)
				scale = 1f * size.X / (icon.Rectangle.Width * scale);
			if (icon.Rectangle.Height * scale < size.Y)
				scale = 1f * size.Y / (icon.Rectangle.Height * scale);
			if (icon.Rectangle.Width * scale > size.X)
				scale = 1f * size.X / (icon.Rectangle.Width * scale);
			if (icon.Rectangle.Height * scale > size.Y)
				scale = 1f * size.Y / (icon.Rectangle.Height * scale);

			b.Draw(icon.Texture, new Vector2(bounds.Center.X - scale, bounds.Center.Y + scale), icon.Rectangle, Color.Black * 0.3f, 0f, new Vector2(icon.Rectangle.Width / 2f, icon.Rectangle.Height / 2f), scale, SpriteEffects.None, 4f);
			b.Draw(icon.Texture, new Vector2(bounds.Center.X, bounds.Center.Y), icon.Rectangle, color, 0f, new Vector2(icon.Rectangle.Width / 2f, icon.Rectangle.Height / 2f), scale, SpriteEffects.None, 4f);
		}
	}
}