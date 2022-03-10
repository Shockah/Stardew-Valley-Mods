using System;
using System.Collections.Generic;

namespace Shockah.UIKit.Gesture
{
	public enum UIGestureRecognizerState { Possible, Detecting, Began, Changed, Failed, Cancelled, Ended }

	public abstract class UIGestureRecognizer
	{
		public UIGestureRecognizerState State
		{
			get => _state;
			set
			{
				if (_state == value)
					return;
				var oldValue = _state;
				_state = value;
				StateChanged?.Invoke(this, oldValue, value);
			}
		}

		public bool FailRequirementsSatisfied
		{
			get
			{
				foreach (var failRequirement in FailRequirements)
					if (failRequirement.State != UIGestureRecognizerState.Failed)
						return false;
				return true;
			}
		}

		public bool InProgress
			=> State is UIGestureRecognizerState.Began or UIGestureRecognizerState.Changed;

		public bool Finished
			=> State is UIGestureRecognizerState.Ended or UIGestureRecognizerState.Failed or UIGestureRecognizerState.Cancelled;

		public event OwnerValueChangeEvent<UIGestureRecognizer, UIGestureRecognizerState?>? StateChanged;

		protected IReadOnlySet<UIGestureRecognizer> FailRequirements
			=> (IReadOnlySet<UIGestureRecognizer>)_failRequirements;

		protected IReadOnlySet<UIGestureRecognizer> FailRequirees
			=> (IReadOnlySet<UIGestureRecognizer>)_failRequirees;

		private UIGestureRecognizerState _state = UIGestureRecognizerState.Possible;
		private readonly ISet<UIGestureRecognizer> _failRequirements = new HashSet<UIGestureRecognizer>();
		private readonly ISet<UIGestureRecognizer> _failRequirees = new HashSet<UIGestureRecognizer>();

		public UIGestureRecognizer()
		{
			StateChanged += (_, _, _) => OnStateChanged();
		}

		public virtual void Update(double currentTime)
		{
		}

		public void Cancel()
		{
			if (State != UIGestureRecognizerState.Possible)
				State = UIGestureRecognizerState.Cancelled;
		}

		public void AddFailRequirement(UIGestureRecognizer recognizer)
		{
			if (recognizer == this)
				throw new ArgumentException("Cannot require itself to fail.");
			if (recognizer.FailRequirements.Contains(this))
				throw new ArgumentException($"Detected circular failure {nameof(UIGestureRecognizer)} dependency between {this} and {recognizer}.");

			_failRequirements.Add(recognizer);
			recognizer._failRequirees.Add(this);
		}

		public void RemoveFailRequirement(UIGestureRecognizer recognizer)
		{
			if (recognizer == this)
				return;
			_failRequirements.Remove(recognizer);
			recognizer._failRequirees.Remove(this);
		}

		protected virtual void OnFailRequirementEnded(UIGestureRecognizer recognizer)
		{
			State = UIGestureRecognizerState.Failed;
		}

		protected virtual void OnFailRequirementFailed(UIGestureRecognizer recognizer)
		{
		}

		public virtual void OnTouchDown(UITouch touch)
		{
		}

		public virtual void OnTouchChanged(UITouch touch)
		{
		}

		public virtual void OnTouchUp(UITouch touch)
		{
		}

		private void OnStateChanged()
		{
			if (State == UIGestureRecognizerState.Ended)
			{
				foreach (var failRequiree in FailRequirees)
					failRequiree.OnFailRequirementEnded(this);
			}
			else if (State == UIGestureRecognizerState.Failed)
			{
				foreach (var failRequiree in FailRequirees)
					failRequiree.OnFailRequirementFailed(this);
			}
		}
	}
}
