using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.UIKit.Geometry;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;

namespace Shockah.UIKit
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested interfaces")]
	public interface IUIFont
	{
		UIVector2 Measure(string text, float maxWidth = float.PositiveInfinity);

		public interface Uncolorable: IUIFont
		{
			void Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text);
		}

		public interface Colorable: IUIFont
		{
			void Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text, Color color);
		}
	}

	public class UIDialogueFont: IUIFont.Colorable
	{
		public UIVector2 Scale { get; private set; }

		public UIDialogueFont(UIVector2? scale = null)
		{
			this.Scale = scale ?? UIVector2.One;
		}

		public UIVector2 Measure(string text, float maxWidth = float.PositiveInfinity)
		{
			return (UIVector2)Game1.dialogueFont.MeasureString(text) * Scale;
		}

		public void Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text, Color color)
		{
			b.DrawString(Game1.dialogueFont, text, position, color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 1);
		}
	}

	public enum UISpriteTextFontColor
	{
		Default = -1,
		Black = SpriteText.color_Black,
		Blue = SpriteText.color_Blue,
		Red = SpriteText.color_Red,
		Purple = SpriteText.color_Purple,
		White = SpriteText.color_White,
		Orange = SpriteText.color_Orange,
		Green = SpriteText.color_Green,
		Cyan = SpriteText.color_Cyan,
		Gray = SpriteText.color_Gray
	}

	public class UISpriteTextFont: IUIFont.Uncolorable
	{
		public float Scale { get; private set; }
		public UISpriteTextFontColor Color { get; private set; }

		public UISpriteTextFont(float scale = 1f, UISpriteTextFontColor color = UISpriteTextFontColor.Default)
		{
			this.Scale = scale;
			this.Color = color;
		}

		public UIVector2 Measure(string text, float maxWidth = float.PositiveInfinity)
		{
			var intMaxWidth = (int)Math.Ceiling(Math.Min(maxWidth, 999999));
			return new Vector2(SpriteText.getWidthOfString(text, intMaxWidth) * Scale, SpriteText.getHeightOfString(text, intMaxWidth) * Scale);
		}

		public void Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text)
		{
			SpriteText.drawString(
				b, text,
				x: (int)position.X,
				y: (int)position.Y,
				color: (int)Color
			);
		}
	}
}