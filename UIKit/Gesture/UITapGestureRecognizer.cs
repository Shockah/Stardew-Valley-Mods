using System;

namespace Shockah.UIKit.Gesture
{
	public class UITapGestureRecognizer: UIGestureRecognizer
	{
		public int TapsRequired { get; private set; }
		public float Delay { get; private set; }

		public event Action<UITapGestureRecognizer, UITouch>? TapEvent;

		private UITouch? Touch = null;
		private int Taps = 0;
		private double CurrentTime;
		private double? TapTime = null;

		public UITapGestureRecognizer(int tapsRequired = 1, float delay = 0f)
		{
			this.TapsRequired = tapsRequired;
			this.Delay = delay;

			StateChanged += (_, _, _) => OnStateChanged();
		}

		public override void Update(double currentTime)
		{
			base.Update(currentTime);
			CurrentTime = currentTime;

			if (TapTime is not null && (TapTime.Value - currentTime) >= Delay)
			{
				Touch = null;
				State = UIGestureRecognizerState.Failed;
				Taps = 0;
			}
		}

		protected override void OnFailRequirementFailed(UIGestureRecognizer recognizer)
		{
			base.OnFailRequirementFailed(recognizer);

			if (Touch is not null && Taps >= TapsRequired)
			{
				if (!FailRequirementsSatisfied)
					return;
				if (Touch.ActiveRecognizer is not null)
				{
					State = UIGestureRecognizerState.Failed;
					return;
				}
				Touch.ActiveRecognizer = this;
				State = UIGestureRecognizerState.Ended;
				TapEvent?.Invoke(this, Touch);
			}
		}

		public override void OnTouchDown(UITouch touch)
		{
			base.OnTouchDown(touch);

			if (State == UIGestureRecognizerState.Possible || InProgress)
			{
				if (InProgress)
				{
					State = UIGestureRecognizerState.Changed;
					Taps++;
				}
				else
				{
					State = UIGestureRecognizerState.Began;
					Taps++;
				}
				this.Touch = touch;
				TapTime = Delay > 0f ? CurrentTime : null;
			}
		}

		public override void OnTouchUp(UITouch touch)
		{
			base.OnTouchUp(touch);

			if (InProgress && this.Touch == touch && Taps >= TapsRequired)
			{
				if (!FailRequirementsSatisfied)
					return;
				if (touch.ActiveRecognizer is not null)
				{
					State = UIGestureRecognizerState.Failed;
					return;
				}
				touch.ActiveRecognizer = this;
				State = UIGestureRecognizerState.Ended;
				TapEvent?.Invoke(this, touch);
			}
		}

		private void OnStateChanged()
		{
			if (State == UIGestureRecognizerState.Failed)
			{
				Taps = 0;
				TapTime = null;
			}
		}
	}
}