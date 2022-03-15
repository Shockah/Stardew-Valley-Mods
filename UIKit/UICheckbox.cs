﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.UIKit.Geometry;
using Shockah.UIKit.Gesture;
using StardewValley;
using StardewValley.Menus;
using System;

namespace Shockah.UIKit
{
	public interface IUICheckboxBehavior
	{
		public bool IsChecked { get; set; }

		public event OwnerValueChangeEvent<UICheckbox, bool>? IsCheckedChanged;
	}

	public class UICheckbox: UIView.Drawable, IUICheckboxBehavior
	{
		public bool IsChecked
		{
			get => _isChecked;
			set
			{
				if (_isChecked == value)
					return;
				var oldValue = _isChecked;
				_isChecked = value;
				IsCheckedChanged?.Invoke(this, oldValue, value);
			}
		}

		public UITextureRect CheckedTexture
		{
			get => LazyCheckedTexture.Value;
			set
			{
				if (LazyCheckedTexture.Value == value)
					return;
				var oldValue = LazyCheckedTexture.Value;
				LazyCheckedTexture = new(value);
				CheckedTextureChanged?.Invoke(this, oldValue, value);
			}
		}

		public UITextureRect UncheckedTexture
		{
			get => LazyUncheckedTexture.Value;
			set
			{
				if (LazyUncheckedTexture.Value == value)
					return;
				var oldValue = LazyUncheckedTexture.Value;
				LazyUncheckedTexture = new(value);
				UncheckedTextureChanged?.Invoke(this, oldValue, value);
			}
		}

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

		public string? TapSoundName
		{
			get => _tapSoundName;
			set
			{
				if (_tapSoundName == value)
					return;
				var oldValue = _tapSoundName;
				_tapSoundName = value;
				TapSoundNameChanged?.Invoke(this, oldValue, value);
			}
		}

		public UITextureRect CurrentTexture
			=> IsChecked ? CheckedTexture : UncheckedTexture;

		public event OwnerValueChangeEvent<UICheckbox, bool>? IsCheckedChanged;
		public event OwnerValueChangeEvent<UICheckbox, UITextureRect>? CheckedTextureChanged;
		public event OwnerValueChangeEvent<UICheckbox, UITextureRect>? UncheckedTextureChanged;
		public event OwnerValueChangeEvent<UICheckbox, UIVector2>? ScaleChanged;
		public event OwnerValueChangeEvent<UICheckbox, Color>? ColorChanged;
		public event OwnerValueChangeEvent<UICheckbox, string?>? TapSoundNameChanged;

		public UITapGestureRecognizer TapGestureRecognizer { get; private set; }

		private bool _isChecked = false;
		private Lazy<UITextureRect> LazyCheckedTexture = new(() => new(Game1.mouseCursors, OptionsCheckbox.sourceRectChecked));
		private Lazy<UITextureRect> LazyUncheckedTexture = new(() => new(Game1.mouseCursors, OptionsCheckbox.sourceRectUnchecked));
		private UIVector2 _scale = new(4);
		private Color _color = Color.White;
		private string? _tapSoundName = "drumkit6";

		public UICheckbox(Func<UITouch, bool>? touchPredicate = null)
		{
			IsSelfTouchInteractionEnabled = true;
			HorizontalContentHuggingPriority = UILayoutConstraintPriority.High;
			VerticalContentHuggingPriority = UILayoutConstraintPriority.High;

			AddGestureRecognizer(TapGestureRecognizer = new UITapGestureRecognizer(touchPredicate: touchPredicate ?? TouchPredicates.LeftOrNonMouseButton, onTap: (_, _) =>
			{
				if (TapSoundName is not null)
					Game1.playSound(TapSoundName);
				IsChecked = !IsChecked;
			}));
		}

		protected override void OnUpdateConstraints()
		{
			base.OnUpdateConstraints();
			IntrinsicWidth = CurrentTexture.SourceRect.Width * Scale.X;
			IntrinsicHeight = CurrentTexture.SourceRect.Height * Scale.Y;
		}

		public override void DrawSelf(RenderContext context)
		{
			var actualScale = Size / CurrentTexture.SourceRect.Size;
			context.SpriteBatch.Draw(CurrentTexture.Texture, context.Offset, CurrentTexture.SourceRect, Color, 0f, Vector2.Zero, actualScale, SpriteEffects.None, 0f);
		}
	}
}