using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode;
using Shockah.CommonModCode.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit
{
	public class UIKit: Mod
	{
		private StardewRootView Root = null!;
		private UISurfaceView SurfaceView = null!;
		private readonly IDictionary<UIView, (string? title, string text)> Tooltips = new Dictionary<UIView, (string? title, string text)>();

		public override void Entry(IModHelper helper)
		{
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
			helper.Events.Display.RenderedHud += OnRenderedHud;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			Root = new(Helper.Input);
			Root.UnsatifiableConstraintEvent += (_, constraint) => Monitor.Log($"Could not satisfy constraint {constraint}.", LogLevel.Error);

			new UIScalableColorableLabel(new UISpriteFont(Game1.dialogueFont), "Top-left label.").With(Root, (self, parent) =>
			{
				self.Scale = 2f;
				self.NumberOfLines = 0;

				parent.AddSubview(self);
				self.LeftAnchor.MakeConstraintToSuperview().Activate();
				self.TopAnchor.MakeConstraintToSuperview().Activate();
				self.WidthAnchor.MakeConstraint(400).Activate();
			});

			SurfaceView = new UISurfaceView().With(Root, (self, parent) =>
			{
				new UINinePatch().With(self, (self, parent) =>
				{
					self.Texture = new(Game1.content.Load<Texture2D>("LooseSprites/DialogBoxGreen"), new(16, 16, 160, 160));
					self.NinePatchInsets = new(44);
					self.Color = Color.White * 0.75f;
					self.IsTouchThrough = false;

					new UIStackView(Orientation.Vertical).With(self, (self, parent) =>
					{
						self.ContentInsets = new(26);

						new UIStackView(Orientation.Horizontal).With(self, (self, parent) =>
						{
							self.Alignment = UIStackViewAlignment.Center;
							self.Spacing = 24f;
							Tooltips[self] = (title: null, text: "Checkbox stack view");

							new UICheckbox().With(self, (self, parent) =>
							{
								self.IsCheckedChanged += (_, _, newValue) => Monitor.Log($"Changed checkbox state: {newValue}", LogLevel.Info);
								parent.AddArrangedSubview(self);
							});

							new UIScalableColorableLabel(new UISpriteFont(Game1.dialogueFont)).With(self, (self, parent) =>
							{
								self.Text = "Check me out";
								self.NumberOfLines = 0;
								parent.AddArrangedSubview(self);
							});

							parent.AddArrangedSubview(self);
						});

						for (int i = 0; i < 4; i++)
						{
							new UILabel<UISpriteTextFont>(new UISpriteTextFont(color: UISpriteTextFontColor.White)).With(self, (self, parent) =>
							{
								self.TextAlignment = TextAlignment.Center;
								self.Text = $"Label no. {string.Concat(Enumerable.Repeat($"{i + 1}", i + 1))}";
								parent.AddArrangedSubview(self);
							});
						}

						new UIStackView(Orientation.Horizontal).With(self, (self, parent) =>
						{
							self.Distribution = UIStackViewDistribution.EqualSpacing;
							self.Spacing = 24f;

							var button1 = new UITextureButton(new(Game1.mouseCursors, new(128, 256, 64, 64))).With(self, (self, parent) =>
							{
								self.TapEvent += _ => Monitor.Log("Pressed OK button", LogLevel.Info);
								Tooltips[self] = (title: null, text: "Yay");
								parent.AddArrangedSubview(self);
								self.MakeAspectRatioConstraint().Activate();
							});

							var button2 = new UITextureButton(new(Game1.mouseCursors, new(192, 256, 64, 64))).With(self, (self, parent) =>
							{
								self.TapEvent += _ => Monitor.Log("Pressed Cancel button", LogLevel.Info);
								Tooltips[self] = (title: null, text: "Nay");
								parent.AddArrangedSubview(self);
								self.MakeAspectRatioConstraint().Activate();
							});

							parent.AddArrangedSubview(self);
							button1.WidthAnchor.MakeConstraintTo(button2).Activate();
						});

						parent.AddSubview(self);
						self.MakeEdgeConstraintsToSuperview().Activate();
					});

					parent.AddSubview(self);
					self.MakeEdgeConstraintsToSuperview().Activate();
				});

				parent.AddSubview(self);
				self.LeftAnchor.MakeConstraintToSuperview(16).Activate();
				self.BottomAnchor.MakeConstraintToSuperview(-16).Activate();
				self.WidthAnchor.MakeConstraint(300).Activate();
			});

			new UINinePatch().With(Root, (self, parent) =>
			{
				self.Texture = new(Game1.content.Load<Texture2D>("LooseSprites/DialogBoxGreen"), new(16, 16, 160, 160));
				self.NinePatchInsets = new(44);
				self.Color = Color.White * 0.75f;
				self.IsTouchThrough = false;

				new UIScrollView().With(self, (self, parent) =>
				{
					self.ScrollFactor *= 0.5f;
					self.ReverseVerticalDirection = true;

					new UIStackView(Orientation.Vertical).With(self, (self, parent) =>
					{
						var colors = new[] { Color.Red, Color.Orange, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta };
						foreach (var color in colors)
						{
							new UIQuad().With(self, (self, parent) =>
							{
								self.Color = color;

								parent.AddArrangedSubview(self);
								self.HeightAnchor.MakeConstraint(80).Activate();
							});
						}

						parent.AddSubview(self);
						self.MakeHorizontalEdgeConstraintsToSuperview().Activate();
						self.MakeEdgeConstraintsTo(parent.ContentFrame).Activate();
					});

					parent.AddSubview(self);
					self.MakeEdgeConstraintsToSuperview(20f).Activate();
				});

				parent.AddSubview(self);
				self.CenterYAnchor.MakeConstraintTo(parent).Activate();
				self.RightAnchor.MakeConstraintTo(parent, -160).Activate();
				self.WidthAnchor.MakeConstraint(200).Activate();
				self.HeightAnchor.MakeConstraint(200).Activate();
			});
		}

		private void OnUpdateTicking(object? sender, UpdateTickingEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;
			Root.Update();
		}

		private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
		{
			SurfaceView.Color = Color.White * (0.9f + 0.1f * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250));
			Root.Draw(e.SpriteBatch);

			var tooltip = Root.VisitAllViews(UIViewVisitingOrder.VisibleOrder)
				.FirstOrDefault(v => v.Hover == HoverState.Direct && Tooltips.ContainsKey(v))
				?.Let(v => Tooltips[v]);
			if (tooltip is not null)
				IClickableMenu.drawToolTip(e.SpriteBatch, tooltip.Value.text, tooltip.Value.title, null);
		}
	}
}