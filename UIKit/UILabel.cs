using Microsoft.Xna.Framework;
using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shockah.UIKit
{
	public enum UILabelLineSegmenting { ByWordBoundary, ByAlphanumericWord, ByChar }
	public enum UILabelLineTruncating { None, Tail, Head }

	public class UILabel<FontType>: UIView.Drawable where FontType: class, IUIFont
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

		public UILabelLineTruncating LineTruncating
		{
			get => _lineTruncating;
			set
			{
				if (_lineTruncating == value)
					return;
				var oldValue = _lineTruncating;
				_lineTruncating = value;
				LineTruncatingChanged?.Invoke(this, oldValue, value);
			}
		}

		public UILabelLineSegmenting LineTruncatingSegmenting
		{
			get => _lineTruncatingSegmenting;
			set
			{
				if (_lineTruncatingSegmenting == value)
					return;
				var oldValue = _lineTruncatingSegmenting;
				_lineTruncatingSegmenting = value;
				LineTruncatingSegmentingChanged?.Invoke(this, oldValue, value);
			}
		}

		public UILabelLineSegmenting? LineBreakMode
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

		public bool IsDisplayingTruncatedText { get; private set; } = false;

		public event OwnerValueChangeEvent<UILabel<FontType>, string>? TextChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, FontType>? FontChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, TextAlignment>? TextAlignmentChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, int>? NumberOfLinesChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, UILabelLineTruncating>? LineTruncatingChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, UILabelLineSegmenting>? LineTruncatingSegmentingChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, UILabelLineSegmenting?>? LineBreakModeChanged;
		public event OwnerValueChangeEvent<UILabel<FontType>, bool>? IsDisplayingTruncatedTextChanged;

		private string _text;
		private FontType _font;
		private TextAlignment _textAlignment = TextAlignment.Left;
		private int _numberOfLines = 1;
		private UILabelLineTruncating _lineTruncating = UILabelLineTruncating.Tail;
		private UILabelLineSegmenting _lineTruncatingSegmenting = UILabelLineSegmenting.ByAlphanumericWord;
		private UILabelLineSegmenting? _lineBreakMode = UILabelLineSegmenting.ByAlphanumericWord;

		internal string CachedText;
		internal UIVector2 CachedTextSize = UIVector2.Zero;
		protected bool IsDirty { get; set; } = true;

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
			LineTruncatingChanged += (_, _, _) => IsDirty = true;
			LineTruncatingSegmentingChanged += (_, _, _) => IsDirty = true;
			LineBreakModeChanged += (_, _, _) => IsDirty = true;
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

		protected virtual UIVector2 Measure(string text)
		{
			return Font.Measure(text);
		}

		protected virtual void DrawSelf(RenderContext context, float xOffset)
		{
			Font.Draw(context.SpriteBatch, context.Offset + (xOffset, 0f), Size, CachedText);
		}

		private void UpdateCachedText()
		{
			bool wasDisplayingTruncatedText = IsDisplayingTruncatedText;
			if (Root is not null)
			{
				var (lines, isTruncated) = GetSplitLinesFitting();
				CachedText = string.Join("\n", lines.Select(l => l.text));
				IsDisplayingTruncatedText = isTruncated;
			}
			CachedTextSize = Measure(CachedText);
			IsDirty = false;
			if (wasDisplayingTruncatedText != IsDisplayingTruncatedText)
				IsDisplayingTruncatedTextChanged?.Invoke(this, wasDisplayingTruncatedText, IsDisplayingTruncatedText);
		}

		private (IReadOnlyList<(string text, UIVector2 size)> lines, bool isTruncated) GetSplitLinesFitting()
		{
			static IEnumerable<string> GetSegments(string text, UILabelLineSegmenting? segmenting)
			{
				return segmenting switch
				{
					UILabelLineSegmenting.ByWordBoundary => Regex.Split(text, "(\\s+)"),
					UILabelLineSegmenting.ByAlphanumericWord =>
						Regex.Split(text, "(\\s+)")
							.SelectMany(split => Regex.Split(split, "([^a-z\\d])", RegexOptions.IgnoreCase))
							.ToList(),
					UILabelLineSegmenting.ByChar => text.Select(c => $"{c}").ToList(),
					null => new[] { text },
					_ => throw new InvalidOperationException($"{nameof(UILabelLineSegmenting)} has an invalid value."),
				};
			}

			IDictionary<string, UIVector2> sizeCache = new Dictionary<string, UIVector2>();
			var existingLines = Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			var results = new List<(string text, UIVector2 size)>();

			UIVector2 GetMeasure(string text)
			{
				if (!sizeCache.TryGetValue(text, out var size))
				{
					size = Measure(text);
					sizeCache[text] = size;
				}
				return size;
			}

			var measuredSize = GetMeasure(string.Join("\n", existingLines));
			var optimalSize = GetOptimalSize(
				horizontalLength: UIOptimalSideLength.Expanded,
				verticalLength: UIOptimalSideLength.OfLength(measuredSize.Y)
			);

			// line breaking

			foreach (var existingLine in existingLines)
			{
				if (GetMeasure(existingLine).X <= optimalSize.X)
				{
					results.Add((existingLine, GetMeasure(existingLine)));
					continue;
				}

				IReadOnlyList<string> lineSegments = GetSegments(existingLine, LineBreakMode).ToList();
				if (lineSegments.Count == 0)
				{
					results.Add(("", GetMeasure(" ")));
				}
				else
				{
					var currentLine = "";
					var currentLength = 0f;
					var ignoreWhitespace = false;

					foreach (var segment in lineSegments)
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
			}

			// line limit

			var removeFromStart = LineTruncating == UILabelLineTruncating.Head;
			var removedAnyLines = false;
			while (NumberOfLines != 0 && results.Count > NumberOfLines)
			{
				results.RemoveAt(removeFromStart ? 0 : results.Count - 1);
				removedAnyLines = true;
			}

			// line truncating

			if (removedAnyLines && LineTruncating != UILabelLineTruncating.None)
			{
				string ellipsis = "...";
				var lineToTruncate = results[LineTruncating == UILabelLineTruncating.Head ? 0 : ^1].text;
				lineToTruncate = LineTruncating == UILabelLineTruncating.Head ? ellipsis + lineToTruncate : lineToTruncate + ellipsis;
				IEnumerable<string> lineSegments = GetSegments(lineToTruncate, LineTruncatingSegmenting);
				
				var currentLine = "";
				var ellipsisLength = GetMeasure(ellipsis).X;
				var currentLength = ellipsisLength;

				foreach (var segment in LineTruncating == UILabelLineTruncating.Head ? lineSegments.Reverse() : lineSegments)
				{
					var segmentLength = GetMeasure(segment).X;
					if (currentLength + segmentLength > optimalSize.X)
						break;
					currentLine = LineTruncating == UILabelLineTruncating.Head ? segment + currentLine : currentLine + segment;
					currentLength += segmentLength;
				}

				currentLine = LineTruncating == UILabelLineTruncating.Head ? ellipsis + currentLine : currentLine + ellipsis;
				results[LineTruncating == UILabelLineTruncating.Head ? 0 : ^1] = (currentLine, GetMeasure(currentLine));
			}

			return (lines: results, isTruncated: removedAnyLines);
		}
	}

	public class UIScalableLabel: UILabel<IUIFont.Scalable>
	{
		public UIVector2 Scale
		{
			get => _scale;
			set
			{
				if (_scale == value)
					return;
				var oldValue = _scale;
				_scale = value;
				ScaleChanged?.Invoke(this, oldValue, value);
			}
		}

		public event OwnerValueChangeEvent<UIScalableLabel, UIVector2>? ScaleChanged;

		private UIVector2 _scale = UIVector2.One;
		private Color _color = Color.White;

		public UIScalableLabel(IUIFont.Scalable font, string text = "") : base(font, text)
		{
		}

		protected override UIVector2 Measure(string text)
		{
			return base.Measure(text) * Scale;
		}

		protected override void DrawSelf(RenderContext context, float xOffset)
		{
			Font.Draw(context.SpriteBatch, context.Offset + (xOffset, 0f), Size, CachedText, Scale);
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

	public class UIScalableColorableLabel: UILabel<IUIFont.ScalableColorable>
	{
		public UIVector2 Scale
		{
			get => _scale;
			set
			{
				if (_scale == value)
					return;
				var oldValue = _scale;
				_scale = value;
				ScaleChanged?.Invoke(this, oldValue, value);
			}
		}

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

		public event OwnerValueChangeEvent<UIScalableColorableLabel, UIVector2>? ScaleChanged;
		public event OwnerValueChangeEvent<UIScalableColorableLabel, Color>? ColorChanged;

		private UIVector2 _scale = UIVector2.One;
		private Color _color = Color.White;

		public UIScalableColorableLabel(IUIFont.ScalableColorable font, string text = "") : base(font, text)
		{
			ScaleChanged += (_, _, _) => IsDirty = true;
		}

		protected override UIVector2 Measure(string text)
		{
			return base.Measure(text) * Scale;
		}

		protected override void DrawSelf(RenderContext context, float xOffset)
		{
			Font.Draw(context.SpriteBatch, context.Offset + (xOffset, 0f), Size, CachedText, Color, Scale);
		}
	}
}