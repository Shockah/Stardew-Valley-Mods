using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.UIKit.Geometry;

namespace Shockah.UIKit
{
	public class UINinePatch: UIQuad
	{
		public UIVector2 Scale
		{
			get => _scale;
			set
			{
				if (_scale == value)
					return;
				var oldValue = _scale;
				_scale = value;
				ScaleChanged?.Invoke(this, oldValue, value);
			}
		}

		public UIEdgeInsets NinePatchInsets
		{
			get => _ninePatchInsets;
			set
			{
				if (_ninePatchInsets == value)
					return;
				var oldValue = _ninePatchInsets;
				_ninePatchInsets = value;
				NinePatchInsetsChanged?.Invoke(this, oldValue, value);
			}
		}

		public event OwnerValueChangeEvent<UINinePatch, UIVector2>? ScaleChanged;
		public event OwnerValueChangeEvent<UINinePatch, UIEdgeInsets>? NinePatchInsetsChanged;

		private UIVector2 _scale = UIVector2.One;
		private UIEdgeInsets _ninePatchInsets = new();

		public override void DrawSelf(RenderContext context)
		{
			if (Texture is null)
			{
				base.DrawSelf(context);
				return;
			}

			var wholeSourceRect = Texture.Value.SourceRect;

			// top-left
			if (NinePatchInsets.Left > 0 && NinePatchInsets.Top > 0)
			{
				context.SpriteBatch.Draw(
					texture: Texture.Value.Texture,
					position: context.Offset,
					sourceRectangle: new(
						wholeSourceRect.Left,
						wholeSourceRect.Top,
						(int)NinePatchInsets.Left,
						(int)NinePatchInsets.Top
					),
					color: Color,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: Scale,
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// top-right
			if (NinePatchInsets.Right > 0 && NinePatchInsets.Top > 0)
			{
				context.SpriteBatch.Draw(
					texture: Texture.Value.Texture,
					position: context.Offset + (
						Width - NinePatchInsets.Right * Scale.X,
						0f
					),
					sourceRectangle: new(
						wholeSourceRect.Right - (int)NinePatchInsets.Right,
						wholeSourceRect.Top,
						(int)NinePatchInsets.Right,
						(int)NinePatchInsets.Top
					),
					color: Color,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: Scale,
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// bottom-left
			if (NinePatchInsets.Left > 0 && NinePatchInsets.Bottom > 0)
			{
				context.SpriteBatch.Draw(
					texture: Texture.Value.Texture,
					position: context.Offset + (
						0f,
						Height - NinePatchInsets.Bottom * Scale.Y
					),
					sourceRectangle: new(
						wholeSourceRect.Left,
						wholeSourceRect.Bottom - (int)NinePatchInsets.Bottom,
						(int)NinePatchInsets.Left,
						(int)NinePatchInsets.Bottom
					),
					color: Color,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: Scale,
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// bottom-right
			if (NinePatchInsets.Right > 0 && NinePatchInsets.Bottom > 0)
			{
				context.SpriteBatch.Draw(
					texture: Texture.Value.Texture,
					position: context.Offset + (
						Width - NinePatchInsets.Right * Scale.X,
						Height - NinePatchInsets.Bottom * Scale.Y
					),
					sourceRectangle: new(
						wholeSourceRect.Right - (int)NinePatchInsets.Right,
						wholeSourceRect.Bottom - (int)NinePatchInsets.Bottom,
						(int)NinePatchInsets.Right,
						(int)NinePatchInsets.Bottom
					),
					color: Color,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: Scale,
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// top
			if (NinePatchInsets.Top > 0)
			{
				context.SpriteBatch.Draw(
					texture: Texture.Value.Texture,
					position: context.Offset + (
						NinePatchInsets.Left * Scale.X,
						0f
					),
					sourceRectangle: new(
						wholeSourceRect.Left + (int)NinePatchInsets.Left,
						wholeSourceRect.Top,
						wholeSourceRect.Width - (int)NinePatchInsets.Horizontal,
						(int)NinePatchInsets.Top
					),
					color: Color,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: new Vector2(
						(Width - NinePatchInsets.Horizontal * Scale.X) / (wholeSourceRect.Width - (int)NinePatchInsets.Horizontal),
						Scale.Y
					),
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// bottom
			if (NinePatchInsets.Bottom > 0)
			{
				context.SpriteBatch.Draw(
					texture: Texture.Value.Texture,
					position: context.Offset + (
						NinePatchInsets.Left * Scale.X,
						Height - NinePatchInsets.Bottom * Scale.Y
					),
					sourceRectangle: new(
						wholeSourceRect.Left + (int)NinePatchInsets.Left,
						wholeSourceRect.Bottom - (int)NinePatchInsets.Bottom,
						wholeSourceRect.Width - (int)NinePatchInsets.Horizontal,
						(int)NinePatchInsets.Bottom
					),
					color: Color,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: new Vector2(
						(Width - NinePatchInsets.Horizontal * Scale.X) / (wholeSourceRect.Width - (int)NinePatchInsets.Horizontal),
						Scale.Y
					),
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// left
			if (NinePatchInsets.Left > 0)
			{
				context.SpriteBatch.Draw(
					texture: Texture.Value.Texture,
					position: context.Offset + (
						0f,
						NinePatchInsets.Top * Scale.Y
					),
					sourceRectangle: new(
						wholeSourceRect.Left,
						wholeSourceRect.Top + (int)NinePatchInsets.Top,
						(int)NinePatchInsets.Left,
						wholeSourceRect.Height - (int)NinePatchInsets.Vertical
					),
					color: Color,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: new Vector2(
						Scale.X,
						(Height - NinePatchInsets.Vertical * Scale.Y) / (wholeSourceRect.Height - (int)NinePatchInsets.Vertical)
					),
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// right
			if (NinePatchInsets.Right > 0)
			{
				context.SpriteBatch.Draw(
					texture: Texture.Value.Texture,
					position: context.Offset + (
						Width - NinePatchInsets.Right * Scale.X,
						NinePatchInsets.Top * Scale.Y
					),
					sourceRectangle: new(
						wholeSourceRect.Right - (int)NinePatchInsets.Right,
						wholeSourceRect.Top + (int)NinePatchInsets.Top,
						(int)NinePatchInsets.Right,
						wholeSourceRect.Height - (int)NinePatchInsets.Vertical
					),
					color: Color,
					rotation: 0f,
					origin: Vector2.Zero,
					scale: new Vector2(
						Scale.X,
						(Height - NinePatchInsets.Vertical * Scale.Y) / (wholeSourceRect.Height - (int)NinePatchInsets.Vertical)
					),
					effects: SpriteEffects.None,
					layerDepth: 0f
				);
			}

			// center
			context.SpriteBatch.Draw(
				texture: Texture.Value.Texture,
				position: context.Offset + (
					NinePatchInsets.Left * Scale.X,
					NinePatchInsets.Top * Scale.Y
				),
				sourceRectangle: new(
					wholeSourceRect.Left + (int)NinePatchInsets.Left,
					wholeSourceRect.Top + (int)NinePatchInsets.Top,
					wholeSourceRect.Width - (int)NinePatchInsets.Horizontal,
					wholeSourceRect.Height - (int)NinePatchInsets.Vertical
				),
				color: Color,
				rotation: 0f,
				origin: Vector2.Zero,
				scale: new Vector2(
					(Width - NinePatchInsets.Horizontal * Scale.X) / (wholeSourceRect.Width - (int)NinePatchInsets.Horizontal),
					(Height - NinePatchInsets.Vertical * Scale.Y) / (wholeSourceRect.Height - (int)NinePatchInsets.Vertical)
				),
				effects: SpriteEffects.None,
				layerDepth: 0f
			);
		}
	}
}
