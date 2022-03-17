using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode;
using Shockah.UIKit.Geometry;
using StardewValley;
using System;

namespace Shockah.UIKit
{
	public class UISurfaceView: UIView, IDisposable
	{
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

		public UIVector2 Origin
		{
			get => _origin;
			set
			{
				if (_origin == value)
					return;
				var oldValue = _origin;
				_origin = value;
				OriginChanged?.Invoke(this, oldValue, value);
			}
		}

		public UIVector2 RenderScale
		{
			get => _renderScale;
			set
			{
				if (_renderScale == value)
					return;
				var oldValue = _renderScale;
				_renderScale = value;
				RenderScaleChanged?.Invoke(this, oldValue, value);
			}
		}

		public float RenderRotation
		{
			get => _renderRotation;
			set
			{
				if (_renderRotation == value)
					return;
				var oldValue = _renderRotation;
				_renderRotation = value;
				RotationChanged?.Invoke(this, oldValue, value);
			}
		}

		public bool UsesSurface { get; set; } = true;

		public Texture2D? RenderedTexture => DidDraw ? RenderTarget : null;

		public event OwnerValueChangeEvent<UISurfaceView, Color>? ColorChanged;
		public event OwnerValueChangeEvent<UISurfaceView, UIVector2>? OriginChanged;
		public event OwnerValueChangeEvent<UISurfaceView, UIVector2>? RenderScaleChanged;
		public event OwnerValueChangeEvent<UISurfaceView, float>? RotationChanged;

		private RenderTarget2D? RenderTarget;
		private Color _color = Color.White;
		private UIVector2 _origin = new(0.5f);
		private UIVector2 _renderScale = UIVector2.One;
		private float _renderRotation = 0f;
		private bool DidDraw = false;

		public UISurfaceView()
		{
			ClipsSubviewTouchesToBounds = true;
			SizeChanged += (_, _, _) => UpdateRenderTarget();
		}

		public void Dispose()
		{
			RenderTarget?.Dispose();
			GC.SuppressFinalize(this);
		}

		private void UpdateRenderTarget()
		{
			RenderTarget?.Dispose();
			RenderTarget = null;
			DidDraw = false;
			if (Width <= 0 || Height <= 0)
				return;
			RenderTarget = new RenderTarget2D(Game1.graphics.GraphicsDevice, (int)Math.Ceiling(Width), (int)Math.Ceiling(Height));
		}

		public override void DrawChildren(RenderContext context)
		{
			if (!UsesSurface)
			{
				base.DrawChildren(context);
				return;
			}

			if (Width <= 0 || Height <= 0)
				return;
			if (RenderTarget is null)
				UpdateRenderTarget();

			bool wasInProgress = context.SpriteBatch.TryEnd();

			var oldRenderTarget = context.SpriteBatch.GraphicsDevice.GetRenderTargets().FirstOrNull()?.RenderTarget as RenderTarget2D;
			context.SpriteBatch.GraphicsDevice.SetRenderTarget(RenderTarget);
			context.SpriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
			context.SpriteBatch.GraphicsDevice.Clear(Color.Transparent);
			base.DrawChildren(new RenderContext(context.SpriteBatch));
			context.SpriteBatch.End();
			context.SpriteBatch.GraphicsDevice.SetRenderTarget(oldRenderTarget);
			DidDraw = true;

			if (wasInProgress)
				context.SpriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);

			context.SpriteBatch.Draw(RenderTarget, context.Offset + Size * Origin, null, Color, RenderRotation, Size * Origin, RenderScale, SpriteEffects.None, 0f);
		}
	}
}