using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;

namespace Shockah.UIKit
{
	public class UIQuad: UIView.Drawable
	{
		private static readonly Lazy<Texture2D> Pixel = new(() =>
		{
			var pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
			pixel.SetData(new[] { Color.White });
			return pixel;
		});
		
		public Texture2D? Texture
		{
			get => _texture;
			set
			{
				if (_texture == value)
					return;
				var oldValue = _texture;
				_texture = value;
				TextureChanged?.Invoke(this, oldValue, value);
			}
		}

		public Rectangle? TextureSourceRect
		{
			get => _textureSourceRect;
			set
			{
				if (_textureSourceRect == value)
					return;
				var oldValue = _textureSourceRect;
				_textureSourceRect = value;
				TextureSourceRectChanged?.Invoke(this, oldValue, value);
			}
		}

		public Color Color
		{
			get => _color;
			set
			{
				if (_color == value)
					return;
				var oldValue = _color;
				_color = value;
				ColorChanged?.Invoke(this, oldValue, value);
			}
		}

		public event OwnerValueChangeEvent<UIQuad, Texture2D?>? TextureChanged;
		public event OwnerValueChangeEvent<UIQuad, Rectangle?>? TextureSourceRectChanged;
		public event OwnerValueChangeEvent<UIQuad, Color>? ColorChanged;

		private Texture2D? _texture = null;
		private Rectangle? _textureSourceRect = null;
		private Color _color = Color.White;

		public override void DrawSelf(RenderContext context)
		{
			context.SpriteBatch.Draw(
				texture: Texture ?? Pixel.Value,
				position: new(context.X, context.Y),
				sourceRectangle: TextureSourceRect ?? null,
				color: Color,
				rotation: 0f,
				origin: Vector2.Zero,
				scale: new Vector2(Width, Height),
				effects: SpriteEffects.None,
				layerDepth: 0f
			);
		}
	}
}
