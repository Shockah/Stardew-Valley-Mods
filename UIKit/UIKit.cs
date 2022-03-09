using Microsoft.Xna.Framework;
using Shockah.CommonModCode.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Shockah.UIKit
{
	public class UIKit: Mod
	{
		private readonly UIRoot Root = new();

		public override void Entry(IModHelper helper)
		{
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.Display.RenderedHud += OnRenderedHud;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			{
				var label = new UIColorableLabel(new UIDialogueFont(2f), "Top-left label.");
				Root.AddSubview(label);
				label.LeftAnchor.ConstraintTo(Root).Activate();
				label.TopAnchor.ConstraintTo(Root).Activate();
			}

			{
				var background = new UIRectangle();
				background.Color = Color.DarkSalmon * 0.5f;
				Root.AddSubview(background);

				var stackView = new UIStackView(Orientation.Vertical);
				stackView.Spacing = 16;
				Root.AddSubview(stackView);
				stackView.LeftAnchor.ConstraintTo(Root).Activate();
				stackView.BottomAnchor.ConstraintTo(Root).Activate();

				background.TopAnchor.ConstraintTo(stackView, 16).Activate();
				background.BottomAnchor.ConstraintTo(stackView, -16).Activate();
				background.LeftAnchor.ConstraintTo(stackView, 16).Activate();
				background.RightAnchor.ConstraintTo(stackView, -16).Activate();

				for (int i = 0; i < 3; i++)
				{
					var label = new UIColorableLabel(new UIDialogueFont(2f), $"Label #{i + 1}");
					stackView.AddArrangedSubview(label);
				}
			}
		}

		private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
		{
			var viewportBounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
			Root.X1 = 0f;
			Root.Y1 = 0f;
			Root.Width = viewportBounds.Size.X;
			Root.Height = viewportBounds.Size.Y;

			Root.LayoutIfNeeded();
			Root.Draw(e.SpriteBatch);
		}
	}
}