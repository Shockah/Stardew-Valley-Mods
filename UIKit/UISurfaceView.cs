using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode;
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

		public event OwnerValueChangeEvent<UISurfaceView, Color>? ColorChanged;

		private RenderTarget2D? RenderTarget;
		private Color _color = Color.White;

		public UISurfaceView()
		{
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
			if (Width <= 0 || Height <= 0)
				return;
			RenderTarget = new RenderTarget2D(Game1.graphics.GraphicsDevice, (int)Math.Ceiling(Width), (int)Math.Ceiling(Height));
		}

		public override void DrawChildren(RenderContext context)
		{
			if (Width <= 0 || Height <= 0)
				return;
			if (RenderTarget is null)
				UpdateRenderTarget();

			bool wasInProgress = true;
			try
			{
				context.SpriteBatch.End();
			}
			catch
			{
				wasInProgress = false;
			}

			var oldRenderTarget = context.SpriteBatch.GraphicsDevice.GetRenderTargets().FirstOrNull()?.RenderTarget as RenderTarget2D;
			context.SpriteBatch.GraphicsDevice.SetRenderTarget(RenderTarget);
			context.SpriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
			context.SpriteBatch.GraphicsDevice.Clear(Color.Transparent);
			base.DrawChildren(new RenderContext(context.SpriteBatch));
			context.SpriteBatch.End();
			context.SpriteBatch.GraphicsDevice.SetRenderTarget(oldRenderTarget);

			if (wasInProgress)
				context.SpriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);

			context.SpriteBatch.Draw(RenderTarget!, new Vector2(context.X, context.Y), Color);
		}
	}
}