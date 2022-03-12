using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.CommonModCode;
using Shockah.UIKit.Geometry;
using Shockah.UIKit.Gesture;
using StardewValley;
using System;

namespace Shockah.UIKit
{
	public interface IUIButtonBehavior
	{
		public event OwnerNoValueEvent<IUIButtonBehavior>? TapEvent;
	}

	public class UITextureButton: UIView, IUIButtonBehavior
	{
		public UITextureRect NormalTexture
		{
			get => _normalTexture;
			set
			{
				if (_normalTexture == value)
					return;
				var oldValue = _normalTexture;
				_normalTexture = value;
				NormalTextureChanged?.Invoke(this, oldValue, value);
			}
		}

		public UITextureRect? HoverTexture
		{
			get => _hoverTexture;
			set
			{
				if (_hoverTexture == value)
					return;
				var oldValue = _hoverTexture;
				_hoverTexture = value;
				HoverTextureChanged?.Invoke(this, oldValue, value);
			}
		}

		public UITextureRect? PressedTexture
		{
			get => _pressedTexture;
			set
			{
				if (_pressedTexture == value)
					return;
				var oldValue = _pressedTexture;
				_pressedTexture = value;
				PressedTextureChanged?.Invoke(this, oldValue, value);
			}
		}

		public Color NormalColor
		{
			get => _normalColor;
			set
			{
				if (_normalColor == value)
					return;
				var oldValue = _normalColor;
				_normalColor = value;
				NormalColorChanged?.Invoke(this, oldValue, value);
			}
		}

		public Color HoverColor
		{
			get => _hoverColor;
			set
			{
				if (_hoverColor == value)
					return;
				var oldValue = _hoverColor;
				_hoverColor = value;
				HoverColorChanged?.Invoke(this, oldValue, value);
			}
		}

		public Color PressedColor
		{
			get => _pressedColor;
			set
			{
				if (_pressedColor == value)
					return;
				var oldValue = _pressedColor;
				_pressedColor = value;
				PressedColorChanged?.Invoke(this, oldValue, value);
			}
		}

		public string? HoverSoundName
		{
			get => _hoverSoundName;
			set
			{
				if (_hoverSoundName == value)
					return;
				var oldValue = _hoverSoundName;
				_hoverSoundName = value;
				HoverSoundNameChanged?.Invoke(this, oldValue, value);
			}
		}

		public string? UnhoverSoundName
		{
			get => _unhoverSoundName;
			set
			{
				if (_unhoverSoundName == value)
					return;
				var oldValue = _unhoverSoundName;
				_unhoverSoundName = value;
				UnhoverSoundNameChanged?.Invoke(this, oldValue, value);
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

		public event OwnerValueChangeEvent<UITextureButton, UITextureRect>? NormalTextureChanged;
		public event OwnerValueChangeEvent<UITextureButton, UITextureRect?>? HoverTextureChanged;
		public event OwnerValueChangeEvent<UITextureButton, UITextureRect?>? PressedTextureChanged;
		public event OwnerValueChangeEvent<UITextureButton, Color>? NormalColorChanged;
		public event OwnerValueChangeEvent<UITextureButton, Color>? HoverColorChanged;
		public event OwnerValueChangeEvent<UITextureButton, Color>? PressedColorChanged;
		public event OwnerValueChangeEvent<UITextureButton, string?>? HoverSoundNameChanged;
		public event OwnerValueChangeEvent<UITextureButton, string?>? UnhoverSoundNameChanged;
		public event OwnerValueChangeEvent<UITextureButton, string?>? TapSoundNameChanged;
		public event OwnerNoValueEvent<UITextureButton>? TapEvent;

		event OwnerNoValueEvent<IUIButtonBehavior>? IUIButtonBehavior.TapEvent
		{
			add
			{
				if (value is null)
					return;
				TapEvent += ConvertHandler(value);
			}
			remove
			{
				if (value is null)
					return;
				TapEvent -= ConvertHandler(value);
			}
		}

		private UITextureRect _normalTexture;
		private UITextureRect? _hoverTexture = null;
		private UITextureRect? _pressedTexture = null;
		private Color _normalColor = Color.White;
		private Color _pressedColor = Color.Gray;
		private Color _hoverColor = Color.LightGray;
		private string? _hoverSoundName = "shiny4";
		private string? _unhoverSoundName = null;
		private string? _tapSoundName = "drumkit6";

		private bool IsPressed = false;

		private readonly UIQuad quad;

		public UITextureButton(UITextureRect normalTexture, UITextureRect? hoverTexture = null, UITextureRect? pressedTexture = null, Func<UITouch, bool>? touchPredicate = null)
		{
			this._normalTexture = normalTexture;
			this._hoverTexture = hoverTexture;
			this._pressedTexture = pressedTexture;

			IsSelfTouchInteractionEnabled = true;
			IsSubviewTouchInteractionEnabled = false;

			HoverChanged += (_, _, newValue) =>
			{
				var soundName = newValue ? HoverSoundName : UnhoverSoundName;
				if (soundName is not null)
					Game1.playSound(soundName);
			};

			AddGestureRecognizer(new UITapGestureRecognizer(touchPredicate: touchPredicate ?? TouchPredicates.LeftOrNonMouseButton, onTap: (_, _) =>
			{
				if (TapSoundName is not null)
					Game1.playSound(TapSoundName);
				TapEvent?.Invoke(this);
			}).With(r => r.StateChanged += (recognizer, _, _) => OnGestureRecognizerStateChanged(recognizer)));

			quad = new UIQuad().With(this, (self, parent) =>
			{
				parent.AddSubview(self);
				self.MakeEdgeConstraintsToSuperview().Activate();
			});
		}

		private static OwnerNoValueEvent<UITextureButton> ConvertHandler(OwnerNoValueEvent<IUIButtonBehavior> handler)
			=> owner => handler(owner);

		public override void OnUpdateConstraints()
		{
			base.OnUpdateConstraints();

			var texture = NormalTexture;
			var color = NormalColor;

			if (Hover)
			{
				if (HoverTexture is not null)
					texture = HoverTexture.Value;
				color = HoverColor;
			}
			if (IsPressed)
			{
				if (PressedTexture is not null)
					texture = PressedTexture.Value;
				color = PressedColor;
			}

			quad.Texture = texture;
			quad.Color = color;
		}

		private void OnGestureRecognizerStateChanged(UIGestureRecognizer recognizer)
		{
			IsPressed = recognizer.InProgress;
		}
	}
}