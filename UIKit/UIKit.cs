using Microsoft.Xna.Framework;
using Shockah.CommonModCode;
using Shockah.CommonModCode.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Linq;

namespace Shockah.UIKit
{
	public class UIKit: Mod
	{
		private readonly UIRoot Root = new();
		private UISurfaceView surfaceView = null!;

		public override void Entry(IModHelper helper)
		{
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.Display.RenderedHud += OnRenderedHud;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			new UIColorableLabel(new UIDialogueFont(2f), "Top-left label.").With(Root, (self, parent) =>
			{
				parent.AddSubview(self);
				self.LeftAnchor.MakeConstraintTo(parent).Activate();
				self.TopAnchor.MakeConstraintTo(parent).Activate();
			});

			surfaceView = new UISurfaceView().With(Root, (self, parent) =>
			{
				new UIQuad().With(self, (self, parent) =>
				{
					self.Color = Color.DarkSalmon * 0.5f;

					new UIStackView(Orientation.Vertical).With(self, (self, parent) =>
					{
						self.ContentInsets = new(16);
						self.Alignment = UIStackViewAlignment.Center;

						for (int i = 0; i < 4; i++)
						{
							new UIColorableLabel(new UIDialogueFont()).With(self, (self, parent) =>
							{
								self.Text = $"Label no. {string.Concat(Enumerable.Repeat($"{i + 1}", i + 1))}";
								//self.TextAlignment = TextAlignment.Center;

								parent.AddArrangedSubview(self);
							});

							new UIUncolorableLabel(new UISpriteTextFont(color: UISpriteTextFontColor.White)).With(self, (self, parent) =>
							{
								self.Text = $"Label no. {string.Concat(Enumerable.Repeat($"{i + 1}", i + 1))}";
								//self.TextAlignment = TextAlignment.Center;

								parent.AddArrangedSubview(self);
							});
						}

						parent.AddSubview(self);
						self.MakeEdgeConstraintsToSuperview().Activate();
					});

					parent.AddSubview(self);
					self.MakeEdgeConstraintsToSuperview().Activate();
				});

				parent.AddSubview(self);
				self.LeftAnchor.MakeConstraintTo(parent, 16).Activate();
				self.BottomAnchor.MakeConstraintTo(parent, -16).Activate();
			});
		}

		private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
		{
			var viewportBounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			Root.X1 = 0f;
			Root.Y1 = 0f;
			Root.Width = viewportBounds.Size.X;
			Root.Height = viewportBounds.Size.Y;
			Root.LayoutIfNeeded();

			surfaceView.Color = Color.White * (0.8f + 0.2f * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250));
			Root.DrawInParentContext(new(e.SpriteBatch));
		}
	}
}