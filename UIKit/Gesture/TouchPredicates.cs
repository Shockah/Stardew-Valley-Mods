using Shockah.CommonModCode.SMAPI;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit.Gesture
{
	public static class TouchPredicates
	{
		public static readonly Func<UITouch, bool> LeftButton = touch =>
			touch is UITouch<int, ISet<SButton>> typedTouch && typedTouch.Last.State.Contains(SButton.MouseLeft);
		public static readonly Func<UITouch, bool> RightButton = touch =>
			touch is UITouch<int, ISet<SButton>> typedTouch && typedTouch.Last.State.Contains(SButton.MouseRight);
		public static readonly Func<UITouch, bool> MiddleButton = touch =>
			touch is UITouch<int, ISet<SButton>> typedTouch && typedTouch.Last.State.Contains(SButton.MouseMiddle);
		public static readonly Func<UITouch, bool> X1Button = touch =>
			touch is UITouch<int, ISet<SButton>> typedTouch && typedTouch.Last.State.Contains(SButton.MouseX1);
		public static readonly Func<UITouch, bool> X2Button = touch =>
			touch is UITouch<int, ISet<SButton>> typedTouch && typedTouch.Last.State.Contains(SButton.MouseX2);
		public static readonly Func<UITouch, bool> AnyMouseButton = touch =>
			touch is UITouch<int, ISet<SButton>> typedTouch && typedTouch.Last.State.Any(b => b.GetButtonType() == InputExt.ButtonType.Mouse);

		public static readonly Func<UITouch, bool> LeftOrNonMouseButton = touch =>
			LeftButton(touch) || (touch is UITouch<int, ISet<SButton>> typedTouch && typedTouch.Last.State.Any(b => b.GetButtonType() != InputExt.ButtonType.Mouse));
		public static readonly Func<UITouch, bool> RightOrNonMouseButton = touch =>
			RightButton(touch) || (touch is UITouch<int, ISet<SButton>> typedTouch && typedTouch.Last.State.Any(b => b.GetButtonType() != InputExt.ButtonType.Mouse));
		public static readonly Func<UITouch, bool> MiddleOrNonMouseButton = touch =>
			MiddleButton(touch) || (touch is UITouch<int, ISet<SButton>> typedTouch && typedTouch.Last.State.Any(b => b.GetButtonType() != InputExt.ButtonType.Mouse));

		public static readonly Func<UITouch, bool> LeftOrRightButton = touch => LeftButton(touch) || RightButton(touch);
		public static readonly Func<UITouch, bool> LeftAndRightButton = touch => LeftButton(touch) && RightButton(touch);
		public static readonly Func<UITouch, bool> LeftOrNonMouseOrRightButton = touch => LeftOrNonMouseButton(touch) || RightButton(touch);
		public static readonly Func<UITouch, bool> LeftOrNonMouseAndRightButton = touch => LeftOrNonMouseButton(touch) && RightButton(touch);
	}
}
