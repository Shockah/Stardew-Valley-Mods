using System;

namespace Shockah.UIKit.Gesture
{
	public class UITapGestureRecognizer: UIGestureRecognizer
	{
		public int TapsRequired { get; private set; }
		public float Delay { get; private set; }
		public Func<UITouch, bool>? TouchPredicate { get; private set; }

		public event Action<UITapGestureRecognizer, UITouch>? TapEvent;

		private UITouch? Touch = null;
		private int Taps = 0;
		private double CurrentTime;
		private double? TapTime = null;

		public UITapGestureRecognizer(
			int tapsRequired = 1,
			float delay = 0f,
			Func<UITouch, bool>? touchPredicate = null,
			Action<UITapGestureRecognizer, UITouch>? onTap = null
		)
		{
			this.TapsRequired = tapsRequired;
			this.Delay = delay;
			this.TouchPredicate = touchPredicate;

			if (onTap is not null)
				TapEvent += onTap;

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
				if (TouchPredicate?.Invoke(touch) == false)
					return;

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
				touch.ActiveRecognizer = this;
				TapTime = Delay > 0f ? CurrentTime : null;
			}
		}

		public override void OnTouchChanged(UITouch touch)
		{
			base.OnTouchChanged(touch);
			if (touch.IsSame(this.Touch) && TouchPredicate?.Invoke(touch) == false)
				TryFinish();
		}

		public override void OnTouchUp(UITouch touch)
		{
			base.OnTouchUp(touch);
			if (touch.IsSame(this.Touch))
				TryFinish();
		}

		private void TryFinish()
		{
			if (Touch is null)
				return;

			if (InProgress && Taps >= TapsRequired)
			{
				if (!FailRequirementsSatisfied)
					return;
				if (Touch.ActiveRecognizer is not null && Touch.ActiveRecognizer != this)
				{
					State = UIGestureRecognizerState.Failed;
					return;
				}
				Touch.ActiveRecognizer = this;
				State = UIGestureRecognizerState.Ended;
				TapEvent?.Invoke(this, Touch);
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