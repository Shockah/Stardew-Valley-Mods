using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shockah.SeasonAffixes
{
	internal class AffixChoiceMenu : IClickableMenu
	{
		public const int BaseWidth = 768;
		public const int BaseHeight = 448;
		public static readonly int ChoiceWidth = (BaseWidth - borderWidth * 3) / 2;
		private const int IconWidth = 32;
		private const int IconHeight = 32;
		private const float IconToTextSpacing = 12f;
		private const int AffixSpacing = 4;
		private const int AffixHeight = 40;
		private const int AffixMargin = 8;

		private readonly IReadOnlyList<IReadOnlyList<ISeasonAffix>> Choices;

		private int TimerBeforeStart = 250;
		private int SelectedChoice = -1;

		public AffixChoiceMenu(IReadOnlyList<IReadOnlyList<ISeasonAffix>> choices)
			: base(Game1.uiViewport.Width / 2 - Math.Max(BaseWidth, ChoiceWidth * choices.Count + borderWidth * (1 + choices.Count)) / 2, Game1.uiViewport.Height / 2 - BaseHeight / 2, Math.Max(BaseWidth, ChoiceWidth * choices.Count + borderWidth * (1 + choices.Count)), BaseHeight)
		{
			this.Choices = choices;
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
			yPositionOnScreen = Game1.uiViewport.Height / 2 - height / 2;
		}

		public override void receiveKeyPress(Keys key)
		{
			if (!Game1.options.doesInputListContain(Game1.options.cancelButton, key) && !Game1.options.doesInputListContain(Game1.options.menuButton, key))
				base.receiveKeyPress(key);
		}

		public override void update(GameTime time)
		{
			//if (!isActive)
			//{
			//	exitThisMenu();
			//	return;
			//}

			SelectedChoice = -1;

			int choiceHeight = height - 192;
			for (int i = 0; i < Choices.Count; i++)
			{
				int left = xPositionOnScreen + borderWidth + (ChoiceWidth + borderWidth) * i;
				int top = yPositionOnScreen + 192;
				if (Game1.getMouseX() >= left && Game1.getMouseY() >= top && Game1.getMouseX() < left + ChoiceWidth && Game1.getMouseY() < top + choiceHeight)
				{
					SelectedChoice = i;
					break;
				}
			}
		}

		public override void draw(SpriteBatch b)
		{
			//if (TimerBeforeStart > 0)
			//	return;

			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
			Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, speaker: false, drawOnlyBox: true);

			{
				var text = SeasonAffixes.Instance.Helper.Translation.Get("season.title");
				Utility.drawTextWithShadow(b, text, Game1.dialogueFont, new Vector2(xPositionOnScreen + width / 2f - Game1.dialogueFont.MeasureString(text).X / 2f, yPositionOnScreen + 116), Color.Black);
			}

			{
				var text = SeasonAffixes.Instance.Helper.Translation.Get(SeasonAffixes.Instance.Config.Incremental ? "season.incremental.description" : "season.replacement.description");
				Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(xPositionOnScreen + width / 2f - Game1.smallFont.MeasureString(text).X / 2f, yPositionOnScreen + 164), Color.Black);
			}

			drawHorizontalPartition(b, yPositionOnScreen + 192);

			int choiceHeight = height - 192;
			for (int i = 0; i < Choices.Count; i++)
			{
				if (i != 0)
					drawVerticalIntersectingPartition(b, xPositionOnScreen + (ChoiceWidth + borderWidth) * i, yPositionOnScreen + 192);

				int totalAffixesHeight = Choices[i].Count * AffixHeight + (Choices[i].Count - 1) * AffixSpacing;
				int topAffixPosition = yPositionOnScreen + 196 + choiceHeight / 2 - totalAffixesHeight / 2;

				for (int j = 0; j < Choices[i].Count; j++)
					DrawAffix(b, new(xPositionOnScreen + borderWidth + (ChoiceWidth + borderWidth) * i + AffixMargin, topAffixPosition + j * (AffixHeight + AffixSpacing), ChoiceWidth - AffixMargin * 2, AffixHeight), Choices[i][j], SelectedChoice == i);
			}

			if (SelectedChoice >= 0)
				drawToolTip(b, GetSeasonDescription(Choices[SelectedChoice]), GetSeasonName(Choices[SelectedChoice]), null);

			if (!Game1.options.SnappyMenus)
			{
				Game1.mouseCursorTransparency = 1f;
				drawMouse(b);
			}
		}

		private static void DrawAffix(SpriteBatch b, Rectangle bounds, ISeasonAffix affix, bool selected)
		{
			var icon = affix.Icon;
			float iconScale = 1f;
			if (icon.Rectangle.Width * iconScale < IconWidth)
				iconScale = 1f * IconWidth / icon.Rectangle.Width;
			if (icon.Rectangle.Height * iconScale < IconHeight)
				iconScale = 1f * IconHeight / icon.Rectangle.Height;
			if (icon.Rectangle.Width * iconScale > IconWidth)
				iconScale = 1f * IconWidth / icon.Rectangle.Width;
			if (icon.Rectangle.Height * iconScale > IconHeight)
				iconScale = 1f * IconHeight / icon.Rectangle.Height;

			var iconPosition = new Vector2(bounds.X + IconWidth / 2f, bounds.Y + bounds.Height / 2f);
			b.Draw(icon.Texture, iconPosition + new Vector2(-iconScale, iconScale), icon.Rectangle, Color.Black * 0.3f, 0f, new Vector2(icon.Rectangle.Width / 2f, icon.Rectangle.Height / 2f), iconScale, SpriteEffects.None, 4f);
			b.Draw(icon.Texture, iconPosition, icon.Rectangle, Color.White, 0f, new Vector2(icon.Rectangle.Width / 2f, icon.Rectangle.Height / 2f), iconScale, SpriteEffects.None, 4f);

			Utility.drawTextWithShadow(b, affix.LocalizedName, Game1.dialogueFont, new Vector2(bounds.X + IconWidth + IconToTextSpacing, bounds.Y - 4), selected ? Color.Green : Game1.textColor);
		}

		private static string GetSeasonName(IReadOnlyList<ISeasonAffix> affixes)
		{
			StringBuilder sb = new();
			for (int i = 0; i < affixes.Count; i++)
			{
				if (i != 0)
					sb.Append(SeasonAffixes.Instance.Helper.Translation.Get(i == affixes.Count - 1 ? "season.separator.last" : "season.separator.other"));
				sb.Append(affixes[i].LocalizedName);
			}
			return sb.ToString();
		}

		private static string GetSeasonDescription(IReadOnlyList<ISeasonAffix> affixes)
			=> string.Join("\n", affixes.Select(a => a.LocalizedDescription));
	}
}