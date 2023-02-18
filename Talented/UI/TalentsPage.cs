using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Talented.UI
{
	internal class TalentsPage : IClickableMenu
	{
		private static Talented Instance
			=> Talented.Instance;

		private readonly ClickableTextureComponent UpButton;
		private readonly ClickableTextureComponent DownButton;
		private readonly ClickableTextureComponent ScrollBar;
		private readonly List<TalentTagButton> RootTalentTagButtons = new();

		private readonly Rectangle ScrollBarRunner;
		private int SlotPosition = 0;
		private bool Scrolling = false;

		private string? HoverText;
		private ITalentTag? HoveredTag;
		private ITalentTag? SelectedTag;

		public TalentsPage(int x, int y, int width, int height) : base(x, y, width, height)
		{
			UpButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
			DownButton = new ClickableTextureComponent(new Rectangle(xPositionOnScreen + width + 16, yPositionOnScreen + height - 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
			ScrollBar = new ClickableTextureComponent(new Rectangle(UpButton.bounds.X + 12, UpButton.bounds.Y + UpButton.bounds.Height + 4, 24, 40), Game1.mouseCursors, new Rectangle(435, 463, 6, 10), 4f);
			ScrollBarRunner = new Rectangle(ScrollBar.bounds.X, UpButton.bounds.Y + UpButton.bounds.Height + 4, ScrollBar.bounds.Width, height - 128 - UpButton.bounds.Height - 8);

			RecreateComponents();
			populateClickableComponentList();
		}

		private new void populateClickableComponentList()
		{
			base.populateClickableComponentList();
			allClickableComponents.AddRange(RootTalentTagButtons);
		}

		private void RecreateComponents()
		{
			RootTalentTagButtons.Clear();

			foreach (var rootTalentTag in Instance.RootTalentTags)
			{
				int xOffset = RootTalentTagButtons.Count == 0 ? (xPositionOnScreen + borderWidth) : RootTalentTagButtons.Last().bounds.Right + 8;
				var button = new TalentTagButton(new Rectangle(xOffset, yPositionOnScreen + borderWidth + 64, 40, 40), rootTalentTag);
				RootTalentTagButtons.Add(button);
			}

			populateClickableComponentList();
		}

		public override void draw(SpriteBatch b)
		{
			UpButton.draw(b);
			DownButton.draw(b);
			drawTextureBox(b, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), ScrollBarRunner.X, ScrollBarRunner.Y, ScrollBarRunner.Width, ScrollBarRunner.Height, Color.White, 4f);
			ScrollBar.draw(b);

			foreach (var button in RootTalentTagButtons)
			{
				if (button.Tag == HoveredTag)
					button.DisplayStyle = TalentTagButton.DisplayStyleEnum.Hovered;
				else if (SelectedTag is not null && button.Tag != SelectedTag)
					button.DisplayStyle = TalentTagButton.DisplayStyleEnum.Deselected;
				else
					button.DisplayStyle = TalentTagButton.DisplayStyleEnum.Normal;
				button.Draw(b);
			}

			drawHorizontalPartition(b, yPositionOnScreen + borderWidth + 88, small: true);

			if (!string.IsNullOrEmpty(HoverText))
				drawHoverText(b, HoverText, Game1.smallFont);
		}

		public override void performHoverAction(int x, int y)
		{
			HoverText = null;
			HoveredTag = null;

			foreach (var button in RootTalentTagButtons)
			{
				if (button.containsPoint(x, y))
				{
					HoverText = button.Tag.Name;
					HoveredTag = button.Tag;
					break;
				}
			}

			UpButton.tryHover(x, y);
			DownButton.tryHover(x, y);
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			if (UpButton.containsPoint(x, y) && SlotPosition > 0)
			{
				UpArrowPressed();
				Game1.playSound("shwip");
				return;
			}
			if (DownButton.containsPoint(x, y) && SlotPosition < 0)
			{
				DownArrowPressed();
				Game1.playSound("shwip");
				return;
			}

			foreach (var button in RootTalentTagButtons)
			{
				if (button.containsPoint(x, y))
				{
					SelectedTag = SelectedTag == button.Tag ? null : button.Tag;
					Game1.playSound("smallSelect");
					return;
				}
			}

			if (ScrollBar.containsPoint(x, y))
			{
				Scrolling = true;
				return;
			}
			if (!DownButton.containsPoint(x, y) && x > xPositionOnScreen + width && x < xPositionOnScreen + width + 128 && y > yPositionOnScreen && y < yPositionOnScreen + height)
			{
				Scrolling = true;
				this.leftClickHeld(x, y);
				this.releaseLeftClick(x, y);
				return;
			}
		}

		public override void leftClickHeld(int x, int y)
		{
			base.leftClickHeld(x, y);
			if (Scrolling)
			{
				int y2 = ScrollBar.bounds.Y;
				ScrollBar.bounds.Y = Math.Min(base.yPositionOnScreen + base.height - 64 - 12 - ScrollBar.bounds.Height, Math.Max(y, base.yPositionOnScreen + UpButton.bounds.Height + 20));
				float percentage = (float)(y - ScrollBarRunner.Y) / (float)ScrollBarRunner.Height;
				//SlotPosition = Math.Min(this.sprites.Count - 5, Math.Max(0, (int)((float)this.sprites.Count * percentage)));
				SetScrollBarToCurrentIndex();
				if (y2 != ScrollBar.bounds.Y)
					Game1.playSound("shiny4");
			}
		}

		public override void receiveScrollWheelAction(int direction)
		{
			base.receiveScrollWheelAction(direction);
			if (direction > 0 && SlotPosition > 0)
			{
				UpArrowPressed();
				//this.ConstrainSelectionToVisibleSlots();
				Game1.playSound("shiny4");
			}
			//else if (direction < 0 && SlotPosition < Math.Max(0, this.sprites.Count - 5))
			//{
			//	this.downArrowPressed();
			//	this.ConstrainSelectionToVisibleSlots();
			//	Game1.playSound("shiny4");
			//}
		}

		private void SetScrollBarToCurrentIndex()
		{
			//if (this.sprites.Count > 0)
			//{
			//	this.scrollBar.bounds.Y = this.scrollBarRunner.Height / Math.Max(1, this.sprites.Count - 5 + 1) * this.slotPosition + this.upButton.bounds.Bottom + 4;
			//	if (this.slotPosition == this.sprites.Count - 5)
			//	{
			//		this.scrollBar.bounds.Y = this.downButton.bounds.Y - this.scrollBar.bounds.Height - 4;
			//	}
			//}
			//this.updateSlots();
		}

		private void UpArrowPressed()
		{
			//this.slotPosition--;
			//this.updateSlots();
			UpButton.scale = 3.5f;
			SetScrollBarToCurrentIndex();
		}

		private void DownArrowPressed()
		{
			//this.slotPosition++;
			//this.updateSlots();
			DownButton.scale = 3.5f;
			SetScrollBarToCurrentIndex();
		}
	}
}