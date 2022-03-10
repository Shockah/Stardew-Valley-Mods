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

			public IEnumerable<UIGestureRecognizer> GestureRecognizers => Owner.GestureRecognizers;
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

		private readonly IMonitor Monitor;
		private readonly IList<UIGestureRecognizer> _gestureRecognizers = new List<UIGestureRecognizer>();
		private UITouch<int, ISet<SButton>>? CurrentTouch;

		public StardewRootView(IMonitor monitor) : base(new PrivateGestureRecognizerManager())
		{
			this.Monitor = monitor;
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
			foreach (var recognizer in GestureRecognizers)
			{
				if (recognizer.State == UIGestureRecognizerState.Ended)
					recognizer.State = UIGestureRecognizerState.Possible;
			}

			if (CurrentTouch is null && ((IGestureRecognizerManager)this).ContinuousGestureRecognizers.Any(r => r.InProgress || r.State == UIGestureRecognizerState.Detecting))
			{
				foreach (var recognizer in GestureRecognizers)
				{
					if (recognizer.State == UIGestureRecognizerState.Detecting)
						recognizer.State = UIGestureRecognizerState.Failed;
					else if (recognizer.InProgress)
						recognizer.State = UIGestureRecognizerState.Ended;

					if (recognizer.Finished)
						recognizer.State = UIGestureRecognizerState.Possible;
				}
			}

			var currentMouseState = Game1.input.GetMouseState();
			var mouseButtons = new[] { SButton.MouseLeft, SButton.MouseRight, SButton.MouseMiddle, SButton.MouseX1, SButton.MouseX2 };
			var oldDown = mouseButtons.Where(b => Game1.game1.IsActive && InputHelper.IsPressed(b, mouseState: Game1.oldMouseState)).ToHashSet();
			var newDown = mouseButtons.Where(b => Game1.game1.IsActive && InputHelper.IsPressed(b, mouseState: currentMouseState)).ToHashSet();

			if (CurrentTouch is null)
			{
				if (newDown.Count != 0)
				{
					UIVector2 point = new(currentMouseState.X, currentMouseState.Y);
					UITouch<int, ISet<SButton>> touch = new(this, 0, point, newDown);
					CurrentTouch = touch;
					OnTouchDown(touch);
				}
			}
			else
			{
				if (newDown.Count == 0)
				{
					CurrentTouch.Finish();
					OnTouchUp(CurrentTouch);
					CurrentTouch = null;
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
		}

		public void UpdateAndDraw(SpriteBatch b)
		{
			if (ScreenEdgeInsets is not null)
			{
				var viewportBounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
				X1 = ScreenEdgeInsets.Value.Left;
				Y1 = ScreenEdgeInsets.Value.Top;
				Width = viewportBounds.Size.X - ScreenEdgeInsets.Value.Horizontal;
				Height = viewportBounds.Size.Y - ScreenEdgeInsets.Value.Vertical;
			}

			UpdateConstraints();
			LayoutIfNeeded();

			((IGestureRecognizerManager)this).Update();

			DrawInParentContext(new(b));
		}
	}
}