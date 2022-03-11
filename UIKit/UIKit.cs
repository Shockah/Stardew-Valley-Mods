using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
		private StardewRootView Root = null!;
		private UISurfaceView surfaceView = null!;

		public override void Entry(IModHelper helper)
		{
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.Display.RenderedHud += OnRenderedHud;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			Root = new();
			Root.UnsatifiableConstraintEvent += (_, constraint) => Monitor.Log($"Could not satisfy constraint {constraint}.", LogLevel.Error);

			new UIColorableLabel(new UIDialogueFont(2f), "Top-left label.").With(Root, (self, parent) =>
			{
				parent.AddSubview(self);
				self.LeftAnchor.MakeConstraintTo(parent).Activate();
				self.TopAnchor.MakeConstraintTo(parent).Activate();
			});

			surfaceView = new UISurfaceView().With(Root, (self, parent) =>
			{
				new UINinePatch().With(self, (self, parent) =>
				{
					self.Texture = new(Game1.content.Load<Texture2D>("LooseSprites/DialogBoxGreen"), new(16, 16, 160, 160));
					self.NinePatchInsets = new(44);
					self.Color = Color.White * 0.75f;

					new UIStackView(Orientation.Vertical).With(self, (self, parent) =>
					{
						self.ContentInsets = new(16);
						self.Alignment = UIStackViewAlignment.Center;

						new UIStackView(Orientation.Horizontal).With(self, (self, parent) =>
						{
							//self.Alignment = UIStackViewAlignment.Center;
							self.Spacing = 24f;

							new UICheckbox().With(self, (self, parent) =>
							{
								parent.AddArrangedSubview(self);
							});

							new UIColorableLabel(new UIDialogueFont()).With(self, (self, parent) =>
							{
								self.Text = "Check me out";
								parent.AddArrangedSubview(self);
							});

							parent.AddArrangedSubview(self);
						});

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
			surfaceView.Color = Color.White * (0.8f + 0.2f * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250));
			Root.UpdateAndDraw(e.SpriteBatch);
		}
	}
}