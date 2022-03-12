using Microsoft.Xna.Framework;
using System;

namespace Shockah.UIKit
{
	public abstract class UILabel<FontType>: UIView.Drawable where FontType: class, IUIFont
	{
		public string Text
		{
			get => _text;
			set
			{
				if (_text == value)
					return;
				var oldValue = _text;
				_text = value;
				TextChanged?.Invoke(this, oldValue, value);
			}
		}

		public FontType Font
		{
			get => _font;
			set
			{
				if (_font == value)
					return;
				var oldValue = _font;
				_font = value;
				FontChanged?.Invoke(this, oldValue, value);
			}
		}

		public TextAlignment TextAlignment
		{
			get => _textAlignment;
			set
			{
				if (_textAlignment == value)
					return;
				var oldValue = _textAlignment;
				_textAlignment = value;
				TextAlignmentChanged?.Invoke(this, oldValue, value);
			}
		}

		public event OwnerValueChangeEvent<UILabel<FontType>, string>? TextChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, FontType>? FontChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, TextAlignment>? TextAlignmentChanged;

		private string _text;
		private FontType _font;
		private TextAlignment _textAlignment = TextAlignment.Left;

		public UILabel(FontType font, string text = "")
		{
			this._text = text;
			this._font = font;

			HorizontalContentHuggingPriority = new(UILayoutConstraintPriority.Low.Value + 1f);
			VerticalContentHuggingPriority = new(UILayoutConstraintPriority.Low.Value + 1f);

			TextChanged += (_, _, _) => UpdateIntrinsicSize();
			FontChanged += (_, _, _) => UpdateIntrinsicSize();

			UpdateIntrinsicSize();
		}

		public sealed override void DrawSelf(RenderContext context)
		{
			var viewWidth = Width;
			var contentWidth = IntrinsicWidth!.Value;
			var leftOverWidth = Math.Max(viewWidth - contentWidth, 0f);
			var offset = TextAlignment switch
			{
				TextAlignment.Left => 0f,
				TextAlignment.Center => leftOverWidth * 0.5f,
				TextAlignment.Right => leftOverWidth,
				_ => throw new InvalidOperationException($"{nameof(TextAlignment)} has an invalid value."),
			};
			DrawSelf(context, offset);
		}

		protected abstract void DrawSelf(RenderContext context, float xOffset);

		private void UpdateIntrinsicSize()
		{
			var measure = Font.Measure(Text);
			IntrinsicWidth = measure.X;
			IntrinsicHeight = measure.Y;
		}
	}

	public class UIColorableLabel: UILabel<IUIFont.Colorable>
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

		public event OwnerValueChangeEvent<UIColorableLabel, Color>? ColorChanged;

		private Color _color = Color.White;

		public UIColorableLabel(IUIFont.Colorable font, string text = "") : base(font, text)
		{
		}

		protected override void DrawSelf(RenderContext context, float xOffset)
		{
			Font.Draw(context.SpriteBatch, new(context.X + xOffset, context.Y), Size, Text, Color);
		}
	}

	public class UIUncolorableLabel: UILabel<IUIFont.Uncolorable>
	{
		public UIUncolorableLabel(IUIFont.Uncolorable font, string text = "") : base(font, text)
		{
		}

		protected override void DrawSelf(RenderContext context, float xOffset)
		{
			Font.Draw(context.SpriteBatch, new(context.X + xOffset, context.Y), Size, Text);
		}
	}
}