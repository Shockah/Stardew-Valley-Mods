using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit.Gesture
{
	public interface IGestureRecognizerManager
	{
		IEnumerable<UIGestureRecognizer> GestureRecognizers { get; }
		IEnumerable<UIContinuousGestureRecognizer> ContinuousGestureRecognizers { get; }

		void AddGestureRecognizer(UIGestureRecognizer recognizer);
		void RemoveGestureRecognizer(UIGestureRecognizer recognizer);
		void Update();
	}

	public class GestureRecognizerManager<TPointerID, TPointerState>: IGestureRecognizerManager, ITouchProcessor<TPointerID, TPointerState>
		where TPointerID : IEquatable<TPointerID>
	{
		public Action<UITouch>? OnTouchDownDelegate;
		public Action<UITouch>? OnTouchChangedDelegate;
		public Action<UITouch>? OnTouchUpDelegate;

		public IEnumerable<UIGestureRecognizer> GestureRecognizers => _gestureRecognizers;
		public IEnumerable<UIContinuousGestureRecognizer> ContinuousGestureRecognizers
			=> _gestureRecognizers.OfType<UIContinuousGestureRecognizer>();

		private readonly IList<UIGestureRecognizer> _gestureRecognizers = new List<UIGestureRecognizer>();
		private readonly IDictionary<TPointerID, UITouch<TPointerID, TPointerState>> Touches = new Dictionary<TPointerID, UITouch<TPointerID, TPointerState>>();

		public void AddGestureRecognizer(UIGestureRecognizer recognizer)
		{
			if (!_gestureRecognizers.Contains(recognizer))
				_gestureRecognizers.Add(recognizer);
		}

		public void RemoveGestureRecognizer(UIGestureRecognizer recognizer)
		{
			_gestureRecognizers.Remove(recognizer);
		}

		public void Update()
		{
			foreach (var recognizer in GestureRecognizers)
			{
				if (recognizer.State == UIGestureRecognizerState.Ended)
					recognizer.State = UIGestureRecognizerState.Possible;
			}

			if (Touches.Count == 0 && ContinuousGestureRecognizers.Any(r => r.InProgress || r.State == UIGestureRecognizerState.Detecting))
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
		}

		public void OnTouchDown(TPointerID pointerID, UIVector2 point, TPointerState pointerState)
		{
			var touch = new UITouch<TPointerID, TPointerState>(this, pointerID, point, pointerState);
			Touches[pointerID] = touch;
			OnTouchUpDelegate?.Invoke(touch);
		}

		public void OnTouchChanged(TPointerID pointerID, UIVector2 point, TPointerState pointerState)
		{
			if (!Touches.TryGetValue(pointerID, out var touch))
				throw new InvalidOperationException("Received touch change for an unknown touch.");
			touch.AddSnapshot(point, pointerState);
			OnTouchChangedDelegate?.Invoke(touch);
		}

		public void OnTouchUp(TPointerID pointerID)
		{
			if (!Touches.TryGetValue(pointerID, out var touch))
				throw new InvalidOperationException("Received touch change for an unknown touch.");
			touch.Finish();
			Touches.Remove(pointerID);
			OnTouchUpDelegate?.Invoke(touch);
		}
	}
}
