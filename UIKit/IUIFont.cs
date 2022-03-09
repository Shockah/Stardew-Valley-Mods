using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;

namespace Shockah.UIKit
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested interfaces")]
	public interface IUIFont
	{
		Vector2 Measure(string text, float maxWidth = float.PositiveInfinity);

		public interface Uncolorable: IUIFont
		{
			void Draw(SpriteBatch b, Vector2 position, string text);
		}

		public interface Colorable: IUIFont
		{
			void Draw(SpriteBatch b, Vector2 position, string text, Color color);
		}
	}

	public class UIDialogueFont: IUIFont.Colorable
	{
		public float Scale { get; private set; }

		public UIDialogueFont(float scale)
		{
			this.Scale = scale;
		}

		public Vector2 Measure(string text, float maxWidth = float.PositiveInfinity)
		{
			return Game1.dialogueFont.MeasureString(text) * Scale;
		}

		public void Draw(SpriteBatch b, Vector2 position, string text, Color color)
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

		public UISpriteTextFont(float scale, UISpriteTextFontColor Color = UISpriteTextFontColor.Default)
		{
			this.Scale = scale;
			this.Color = Color;
		}

		public Vector2 Measure(string text, float maxWidth = float.PositiveInfinity)
		{
			var intMaxWidth = (int)Math.Min(maxWidth, 999999);
			return new Vector2(SpriteText.getWidthOfString(text, intMaxWidth) * Scale, SpriteText.getHeightOfString(text, intMaxWidth) * Scale);
		}

		public void Draw(SpriteBatch b, Vector2 position, string text)
		{
			SpriteText.drawString(b, text, (int)position.X, (int)position.Y, layerDepth: 1, color: (int)Color);
		}
	}
}