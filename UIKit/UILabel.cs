using Microsoft.Xna.Framework;
using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shockah.UIKit
{
	public enum UILabelLineBreakMode { ByWrapping, ByTruncatingTail, ByTruncatingHead }
	public enum UILabelLineBreakSplitMode { ByWord, ByChar }

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

		public int NumberOfLines
		{
			get => _numberOfLines;
			set
			{
				value = Math.Max(value, 0);
				if (_numberOfLines == value)
					return;
				var oldValue = _numberOfLines;
				_numberOfLines = value;
				NumberOfLinesChanged?.Invoke(this, oldValue, value);
			}
		}

		public UILabelLineBreakMode LineBreakMode
		{
			get => _lineBreakMode;
			set
			{
				if (_lineBreakMode == value)
					return;
				var oldValue = _lineBreakMode;
				_lineBreakMode = value;
				LineBreakModeChanged?.Invoke(this, oldValue, value);
			}
		}

		public UILabelLineBreakSplitMode LineBreakSplitMode
		{
			get => _lineBreakSplitMode;
			set
			{
				if (_lineBreakSplitMode == value)
					return;
				var oldValue = _lineBreakSplitMode;
				_lineBreakSplitMode = value;
				LineBreakSplitModeChanged?.Invoke(this, oldValue, value);
			}
		}

		public event OwnerValueChangeEvent<UILabel<FontType>, string>? TextChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, FontType>? FontChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, TextAlignment>? TextAlignmentChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, int>? NumberOfLinesChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, UILabelLineBreakMode>? LineBreakModeChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, UILabelLineBreakSplitMode>? LineBreakSplitModeChanged;

		private string _text;
		private FontType _font;
		private TextAlignment _textAlignment = TextAlignment.Left;
		private int _numberOfLines = 1;
		private UILabelLineBreakMode _lineBreakMode = UILabelLineBreakMode.ByTruncatingTail;
		private UILabelLineBreakSplitMode _lineBreakSplitMode = UILabelLineBreakSplitMode.ByWord;

		internal string CachedText;
		internal UIVector2 CachedTextSize = UIVector2.Zero;
		private bool IsDirty = true;

		public UILabel(FontType font, string text = "")
		{
			this._text = text;
			this.CachedText = text;
			this._font = font;

			HorizontalContentHuggingPriority = new(UILayoutConstraintPriority.Low.Value + 1f);
			VerticalContentHuggingPriority = new(UILayoutConstraintPriority.Low.Value + 1f);

			AddedToRoot += (_, _) => IsDirty = true;
			TextChanged += (_, _, _) => IsDirty = true;
			FontChanged += (_, _, _) => IsDirty = true;
			NumberOfLinesChanged += (_, _, _) => IsDirty = true;
			LineBreakModeChanged += (_, _, _) => IsDirty = true;
			LineBreakSplitModeChanged += (_, _, _) => IsDirty = true;
			SizeChanged += (_, _, _) => IsDirty = true;
		}

		protected override void OnUpdateConstraints()
		{
			base.OnUpdateConstraints();
			if (IsDirty)
				UpdateCachedText();
			IntrinsicWidth = CachedTextSize.X;
			IntrinsicHeight = CachedTextSize.Y;
		}

		public sealed override void DrawSelf(RenderContext context)
		{
			var viewWidth = Width;
			var contentWidth = IntrinsicWidth is null ? 0f : IntrinsicWidth.Value;
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

		private void UpdateCachedText()
		{
			if (Root is not null)
			{
				var lines = GetSplitLinesFitting();
				CachedText = string.Join("\n", lines.Select(l => l.text));
			}
			CachedTextSize = Font.Measure(CachedText);
			IsDirty = false;
		}

		private IReadOnlyList<(string text, UIVector2 size)> GetSplitLinesFitting()
		{
			string ellipsis = "...";
			IDictionary<string, UIVector2> sizeCache = new Dictionary<string, UIVector2>();
			var existingLines = Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			var results = new List<(string text, UIVector2 size)>();

			UIVector2 GetMeasure(string text)
			{
				if (!sizeCache.TryGetValue(text, out var size))
				{
					size = Font.Measure(text);
					sizeCache[text] = size;
				}
				return size;
			}

			var measuredSize = Font.Measure(string.Join("\n", existingLines));
			var optimalSize = GetOptimalSize(
				horizontalLength: UIOptimalSideLength.Expanded,
				verticalLength: UIOptimalSideLength.OfLength(measuredSize.Y)
			);

			foreach (var existingLine in existingLines)
			{
				if (GetMeasure(existingLine).X <= optimalSize.X)
				{
					results.Add((existingLine, GetMeasure(existingLine)));
					continue;
				}

				IList<string> segments = LineBreakSplitMode switch
				{
					UILabelLineBreakSplitMode.ByWord => Regex.Split(existingLine, "(\\s+)"),
					UILabelLineBreakSplitMode.ByChar => existingLine.Select(c => $"{c}").ToList(),
					_ => throw new InvalidOperationException($"{nameof(UILabelLineBreakSplitMode)} has an invalid value."),
				};

				if (segments.Count == 0)
				{
					results.Add(("", GetMeasure(" ")));
					continue;
				}

				switch (LineBreakMode)
				{
					case UILabelLineBreakMode.ByWrapping:
						{
							var currentLine = "";
							var currentLength = 0f;
							var ignoreWhitespace = false;

							foreach (var segment in segments)
							{
								var newLength = currentLength + GetMeasure(segment).X;
								if (newLength > optimalSize.X && currentLine != "")
								{
									results.Add((currentLine, GetMeasure(currentLine)));
									currentLine = "";
									currentLength = 0f;
									ignoreWhitespace = true;
								}

								if (ignoreWhitespace && string.IsNullOrWhiteSpace(segment))
									continue;
								ignoreWhitespace = false;

								currentLine += segment;
								currentLength += GetMeasure(segment).X;
							}

							if (currentLine != "")
								results.Add((currentLine, GetMeasure(currentLine)));
						}
						break;
					case UILabelLineBreakMode.ByTruncatingTail:
						{
							var currentLine = "";
							var ellipsisLength = GetMeasure(ellipsis).X;
							var currentLength = ellipsisLength;

							foreach (var segment in segments)
							{
								var segmentLength = GetMeasure(segment).X;
								if (currentLength + segmentLength <= optimalSize.X)
								{
									currentLine += segment;
									currentLength += segmentLength;
								}
								else
								{
									currentLine = currentLine.TrimEnd() + ellipsis;
									results.Add((currentLine, GetMeasure(currentLine)));
									currentLine = "";
									break;
								}
							}

							if (currentLine != "")
							{
								currentLine = currentLine.TrimEnd() + ellipsis;
								results.Add((currentLine, GetMeasure(currentLine)));
							}
						}
						break;
					case UILabelLineBreakMode.ByTruncatingHead:
						{
							var currentLine = "";
							var ellipsisLength = GetMeasure(ellipsis).X;
							var currentLength = ellipsisLength;

							foreach (var segment in segments.Reverse())
							{
								var segmentLength = GetMeasure(segment).X;
								if (currentLength + segmentLength <= optimalSize.X)
								{
									currentLine = segment + currentLine;
									currentLength += segmentLength;
								}
								else
								{
									currentLine = ellipsis + currentLine.TrimStart();
									results.Add((currentLine, GetMeasure(currentLine)));
									currentLine = "";
									break;
								}
							}

							if (currentLine != "")
							{
								currentLine = ellipsis + currentLine.TrimStart();
								results.Add((currentLine, GetMeasure(currentLine)));
							}
						}
						break;
					default:
						throw new InvalidOperationException($"{nameof(UILabelLineBreakMode)} has an invalid value.");
				}
			}

			while (NumberOfLines != 0 && results.Count > NumberOfLines)
				results.RemoveAt(NumberOfLines);
			return results;
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
			Font.Draw(context.SpriteBatch, context.Offset + (xOffset, 0f), Size, CachedText, Color);
		}
	}

	public class UIUncolorableLabel: UILabel<IUIFont.Uncolorable>
	{
		public UIUncolorableLabel(IUIFont.Uncolorable font, string text = "") : base(font, text)
		{
		}

		protected override void DrawSelf(RenderContext context, float xOffset)
		{
			Font.Draw(context.SpriteBatch, context.Offset + (xOffset, 0f), Size, CachedText);
		}
	}
}