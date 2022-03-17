using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.UIKit.Geometry;

namespace Shockah.UIKit
{
	public class UIQuad: UIView.Drawable
	{
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
			IntrinsicWidth = (Texture ?? UITextureRect.Pixel).SourceRect.Width;
			IntrinsicHeight = (Texture ?? UITextureRect.Pixel).SourceRect.Height;
		}

		public override void DrawSelf(RenderContext context)
		{
			context.SpriteBatch.Draw(
				texture: (Texture ?? UITextureRect.Pixel).Texture,
				position: context.Offset,
				sourceRectangle: (Texture ?? UITextureRect.Pixel).SourceRect,
				color: Color,
				rotation: 0f,
				origin: Vector2.Zero,
				scale: Size / ((Texture ?? UITextureRect.Pixel).SourceRect.Size),
				effects: SpriteEffects.None,
				layerDepth: 0f
			);
		}
	}
}
