using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.UIKit.Geometry;
using StardewValley;
using System;

namespace Shockah.UIKit
{
	public class UIQuad: UIView.Drawable
	{
		protected static readonly Lazy<UITextureRect> Pixel = new(() =>
		{
			var pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
			pixel.SetData(new[] { Color.White });
			return new(pixel);
		});
		
		public UITextureRect? Texture
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

		public event OwnerValueChangeEvent<UIQuad, UITextureRect?>? TextureChanged;
		public event OwnerValueChangeEvent<UIQuad, Color>? ColorChanged;

		private UITextureRect? _texture = null;
		private Color _color = Color.White;

		protected override void OnUpdateConstraints()
		{
			base.OnUpdateConstraints();
			IntrinsicWidth = (Texture ?? Pixel.Value).SourceRect.Width;
			IntrinsicHeight = (Texture ?? Pixel.Value).SourceRect.Height;
		}

		public override void DrawSelf(RenderContext context)
		{
			context.SpriteBatch.Draw(
				texture: (Texture ?? Pixel.Value).Texture,
				position: context.Offset,
				sourceRectangle: (Texture ?? Pixel.Value).SourceRect,
				color: Color,
				rotation: 0f,
				origin: Vector2.Zero,
				scale: Size / ((Texture ?? Pixel.Value).SourceRect.Size),
				effects: SpriteEffects.None,
				layerDepth: 0f
			);
		}
	}
}
