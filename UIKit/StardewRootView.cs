using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shockah.CommonModCode.SMAPI;
using Shockah.UIKit.Geometry;
using Shockah.UIKit.Gesture;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit
{
	public class StardewRootView: UIRootView, IGestureRecognizerManager
	{
		private class PrivateGestureRecognizerManager : IGestureRecognizerManager
		{
			internal StardewRootView Owner = null!;

			public IEnumerable<UIGestureRecognizer> GestureRecognizers => ((IGestureRecognizerManager)Owner).GestureRecognizers;
			public IEnumerable<UIContinuousGestureRecognizer> ContinuousGestureRecognizers => ((IGestureRecognizerManager)Owner).ContinuousGestureRecognizers;

			public void AddGestureRecognizer(UIGestureRecognizer recognizer)
				=> ((IGestureRecognizerManager)Owner).AddGestureRecognizer(recognizer);

			public void RemoveGestureRecognizer(UIGestureRecognizer recognizer)
				=> ((IGestureRecognizerManager)Owner).RemoveGestureRecognizer(recognizer);

			public void Update()
				=> ((IGestureRecognizerManager)Owner).Update();
		}

		public UIEdgeInsets? ScreenEdgeInsets { get; set; } = new UIEdgeInsets();

		IEnumerable<UIGestureRecognizer> IGestureRecognizerManager.GestureRecognizers => _gestureRecognizers;
		IEnumerable<UIContinuousGestureRecognizer> IGestureRecognizerManager.ContinuousGestureRecognizers
			=> _gestureRecognizers.OfType<UIContinuousGestureRecognizer>();

		private readonly IInputHelper InputHelper;
		private readonly IList<UIGestureRecognizer> _gestureRecognizers = new List<UIGestureRecognizer>();
		private UITouch<int, ISet<SButton>>? CurrentTouch;
		private bool SuppressTouch = false;
		private UIVector2? LastMouseScroll = null;

		public StardewRootView(IInputHelper inputHelper) : base(new PrivateGestureRecognizerManager())
		{
			this.InputHelper = inputHelper;
			((PrivateGestureRecognizerManager)this.GestureRecognizerManager).Owner = this;
		}

		void IGestureRecognizerManager.AddGestureRecognizer(UIGestureRecognizer recognizer)
		{
			if (!_gestureRecognizers.Contains(recognizer))
				_gestureRecognizers.Add(recognizer);
		}

		void IGestureRecognizerManager.RemoveGestureRecognizer(UIGestureRecognizer recognizer)
		{
			_gestureRecognizers.Remove(recognizer);
		}

		void IGestureRecognizerManager.Update()
		{
			foreach (var recognizer in ((IGestureRecognizerManager)this).GestureRecognizers)
			{
				if (recognizer.State == UIGestureRecognizerState.Ended)
					recognizer.State = UIGestureRecognizerState.Possible;
			}

			if (CurrentTouch is null && ((IGestureRecognizerManager)this).ContinuousGestureRecognizers.Any(r => r.InProgress || r.State == UIGestureRecognizerState.Detecting))
			{
				foreach (var recognizer in ((IGestureRecognizerManager)this).GestureRecognizers)
				{
					if (recognizer.State == UIGestureRecognizerState.Detecting)
						recognizer.State = UIGestureRecognizerState.Failed;
					else if (recognizer.InProgress)
						recognizer.State = UIGestureRecognizerState.Ended;

					if (recognizer.Finished)
						recognizer.State = UIGestureRecognizerState.Possible;
				}
			}

			var totalSeconds = Game1.currentGameTime?.TotalGameTime.TotalSeconds;
			if (totalSeconds is not null)
				foreach (var recognizer in ((IGestureRecognizerManager)this).GestureRecognizers)
					recognizer.Update(totalSeconds.Value);

			var currentMouseState = Mouse.GetState();
			var mouseButtons = new[] { SButton.MouseLeft, SButton.MouseRight, SButton.MouseMiddle, SButton.MouseX1, SButton.MouseX2 };
			var oldDown = mouseButtons.Where(b => Game1.game1.IsActive && b.IsPressed(mouseState: Game1.oldMouseState)).ToHashSet();
			var newDown = mouseButtons.Where(b => Game1.game1.IsActive && b.IsPressed(mouseState: currentMouseState)).ToHashSet();

			UIVector2 currentMouseScroll = new(
				-currentMouseState.HorizontalScrollWheelValue,
				-currentMouseState.ScrollWheelValue
			);
			if (LastMouseScroll is null)
				LastMouseScroll = currentMouseScroll;

			UIVector2 newTouchPoint = new(Game1.getMouseX(true), Game1.getMouseY(true));
			UITouch<int, ISet<SButton>> newTouch = new(
				this,
				0,
				newTouchPoint,
				newDown,
				currentMouseScroll - LastMouseScroll.Value
			);

			OnUpdateHover(newTouch);

			if (CurrentTouch is null)
			{
				if (newDown.Count != 0)
				{
					CurrentTouch = newTouch;
					var handled = OnTouchDown(newTouch);
					SuppressTouch = handled;
				}
			}
			else
			{
				if (newDown.Count == 0)
				{
					CurrentTouch.Finish();
					OnTouchUp(CurrentTouch);
					CurrentTouch = null;
					SuppressTouch = false;
				}
				else
				{
					UIVector2 point = new(currentMouseState.X, currentMouseState.Y);
					if (point != CurrentTouch.LastPoint || !newDown.SetEquals(CurrentTouch.Last.State))
					{
						CurrentTouch.AddSnapshot(point, newDown);
						OnTouchChanged(CurrentTouch);
					}
				}
			}

			LastMouseScroll = currentMouseScroll;

			if (SuppressTouch && CurrentTouch is not null)
				foreach (var button in CurrentTouch.Last.State)
					InputHelper.Suppress(button);
		}

		public void Update()
		{
			if (ScreenEdgeInsets is not null)
			{
				X1 = ScreenEdgeInsets.Value.Left;
				Y1 = ScreenEdgeInsets.Value.Top;
				Width = Game1.viewport.Width * Game1.options.zoomLevel / Game1.options.uiScale - ScreenEdgeInsets.Value.Horizontal;
				Height = Game1.viewport.Height * Game1.options.zoomLevel / Game1.options.uiScale - ScreenEdgeInsets.Value.Vertical;
			}

			UpdateConstraints();
			LayoutIfNeeded();

			((IGestureRecognizerManager)this).Update();
		}

		public void Draw(SpriteBatch b)
		{
			Draw(new RenderContext(b, TopLeft));
		}
	}
}