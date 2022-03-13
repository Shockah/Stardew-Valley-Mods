using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.UIKit.Geometry;
using StardewValley.BellsAndWhistles;
using System;

namespace Shockah.UIKit
{
	public interface IUIFont
	{
		UIVector2 Measure(string text, float maxWidth = float.PositiveInfinity);

		void Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text);

		public interface Scalable: IUIFont
		{
			UIVector2 IUIFont.Measure(string text, float maxWidth)
				=> Measure(text, null, maxWidth);

			UIVector2 Measure(string text, UIVector2? fontScale = null, float maxWidth = float.PositiveInfinity);

			void IUIFont.Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text)
				=> Draw(b, position, size, text, null);

			void Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text, UIVector2? fontScale = null);
		}

		public interface Colorable: IUIFont
		{
			void IUIFont.Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text)
				=> Draw(b, position, size, text, null);

			void Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text, Color? color = null);
		}

		public interface ScalableColorable: Scalable, Colorable
		{
			void IUIFont.Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text)
				=> Draw(b, position, size, text, null, null);

			void Scalable.Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text, UIVector2? fontScale)
				=> Draw(b, position, size, text, null, fontScale);

			void Colorable.Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text, Color? color)
				=> Draw(b, position, size, text, color, null);

			void Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text, Color? color = null, UIVector2? fontScale = null);
		}
	}

	public class UISpriteFont: IUIFont.ScalableColorable
	{
		public SpriteFont Font { get; private set; }

		public UISpriteFont(SpriteFont font)
		{
			this.Font = font;
		}

		public UIVector2 Measure(string text, UIVector2? fontScale = null, float maxWidth = float.PositiveInfinity)
			=> (UIVector2)Font.MeasureString(text) * (fontScale ?? UIVector2.One);

		public void Draw(SpriteBatch b, UIVector2 position, UIVector2 size, string text, Color? color = null, UIVector2? fontScale = null)
		{
			b.DrawString(Font, text, position, color ?? Color.White, 0f, Vector2.Zero, fontScale ?? UIVector2.One, SpriteEffects.None, 1);
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

	public class UISpriteTextFont: IUIFont
	{
		public UISpriteTextFontColor Color { get; private set; }

		public UISpriteTextFont(UISpriteTextFontColor color = UISpriteTextFontColor.Default)
		{
			this.Color = color;
		}

		public UIVector2 Measure(string text, float maxWidth = float.PositiveInfinity)
		{
			var intMaxWidth = (int)Math.Ceiling(Math.Min(maxWidth, 999999));
			return new Vector2(SpriteText.getWidthOfString(text, intMaxWidth), SpriteText.getHeightOfString(text, intMaxWidth));
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