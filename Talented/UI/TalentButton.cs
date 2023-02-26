using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Shockah.Talented.UI
{
	internal sealed class TalentButton : ClickableComponent
	{
		public enum DisplayStyleEnum
		{
			Inactive,
			Active,
			NotApplicable
		}

		private const float HorizontalPadding = 8f;
		private const float VerticalPadding = 8f;
		private const float IconToTextSpacing = 12f;
		private const float TextSpacing = 8f;
		private const float IconWidth = 32f;
		private const float IconHeight = 32f;

		public ITalent Talent { get; private init; }
		public DisplayStyleEnum DisplayStyle { get; set; } = DisplayStyleEnum.Inactive;
		public bool Hovered { get; set; } = false;

		public TalentButton(int x, int y, int width, ITalent talent) : base(new(x, y, width, 0), talent.Name)
		{
			this.Talent = talent;
		}

		public void UpdateHeight()
		{
			float nameHeight = Game1.dialogueFont.MeasureString(Talent.Name).Y;
			float descriptionHeight = Game1.smallFont.MeasureString(Talent.Description).Y;
			float totalHeight = nameHeight + descriptionHeight + VerticalPadding * 2f + TextSpacing;
			bounds = new(bounds.X, bounds.Y, bounds.Width, (int)totalHeight);
		}

		public void Draw(SpriteBatch b)
		{
			var icon = Talent.Icon;
			float iconScale = 1f;
			if (icon.Rectangle.Width * iconScale < IconWidth)
				iconScale = 1f * IconWidth / (icon.Rectangle.Width * iconScale);
			if (icon.Rectangle.Height * iconScale < IconHeight)
				iconScale = 1f * IconHeight / (icon.Rectangle.Height * iconScale);
			if (icon.Rectangle.Width * iconScale > IconWidth)
				iconScale = 1f * IconWidth / (icon.Rectangle.Width * iconScale);
			if (icon.Rectangle.Height * iconScale > IconHeight)
				iconScale = 1f * IconHeight / (icon.Rectangle.Height * iconScale);

			var iconPosition = new Vector2(bounds.X + HorizontalPadding + IconWidth / 2f, bounds.Y + bounds.Height / 2f);
			b.Draw(icon.Texture, iconPosition + new Vector2(-iconScale, iconScale), icon.Rectangle, Color.Black * 0.3f, 0f, new Vector2(icon.Rectangle.Width / 2f, icon.Rectangle.Height / 2f), iconScale, SpriteEffects.None, 4f);
			b.Draw(icon.Texture, iconPosition, icon.Rectangle, Color.White, 0f, new Vector2(icon.Rectangle.Width / 2f, icon.Rectangle.Height / 2f), iconScale, SpriteEffects.None, 4f);


			Utility.drawTextWithShadow(b, Talent.Name, Game1.dialogueFont, new Vector2(bounds.X + HorizontalPadding + IconWidth + IconToTextSpacing, bounds.Y + VerticalPadding), Color.Black);
			Utility.drawTextWithShadow(b, Talent.Description, Game1.smallFont, new Vector2(bounds.X + HorizontalPadding + IconWidth + IconToTextSpacing, bounds.Y + VerticalPadding + Game1.dialogueFont.MeasureString(Talent.Name).Y + TextSpacing), Color.Black);
		}
	}
}