using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit.Gesture
{
	public abstract class UITouch
	{
		public bool IsFinished { get; private set; } = false;

		public abstract IReadOnlyList<UIVector2> Points { get; }
		public virtual UIVector2 FirstPoint => Points[0];
		public virtual UIVector2 LastPoint => Points[^1];

		public UIGestureRecognizer? ActiveRecognizer
		{
			get => _activeRecognizer;
			set
			{
				if (_activeRecognizer == value)
					return;
				_activeRecognizer = value;
				foreach (var activeContinuousGestureRecognizer in GestureRecognizerManager.ContinuousGestureRecognizers)
					if (activeContinuousGestureRecognizer != value)
						activeContinuousGestureRecognizer.OnTouchUsedByRecognizer(this);
			}
		}

		protected readonly IGestureRecognizerManager GestureRecognizerManager;
		private UIGestureRecognizer? _activeRecognizer = null;

		public UITouch(IGestureRecognizerManager gestureRecognizerManager)
		{
			this.GestureRecognizerManager = gestureRecognizerManager;
		}

		public void Finish()
		{
			IsFinished = true;
		}

		public abstract bool IsSame(UITouch? touch);

		public abstract UITouch GetTranslated(UIVector2 translation);
	}
	
	public class UITouch<TPointerID, TPointerState>: UITouch
		where TPointerID : IEquatable<TPointerID>
	{
		public readonly struct Snapshot
		{
			public readonly UIVector2 Point { get; }
			public readonly TPointerState State { get; }
			public readonly UIVector2 Scroll { get; }

			public Snapshot(UIVector2 point, TPointerState state, UIVector2 scroll)
			{
				this.Point = point;
				this.State = state;
				this.Scroll = scroll;
			}
		}

		public TPointerID PointerID { get; private set; }
		public IReadOnlyList<Snapshot> Snapshots => (IReadOnlyList<Snapshot>)_snapshots;
		public Snapshot First => Snapshots[0];
		public Snapshot Last => Snapshots[^1];

		public override IReadOnlyList<UIVector2> Points => _snapshots.Select(s => s.Point).ToList();
		public override UIVector2 FirstPoint => First.Point;
		public override UIVector2 LastPoint => Last.Point;

		private readonly IList<Snapshot> _snapshots = new List<Snapshot>();

		public UITouch(
			IGestureRecognizerManager gestureRecognizerManager,
			TPointerID pointerID,
			UIVector2 initialPoint,
			TPointerState initialState,
			UIVector2? initialScroll = null
		) : base(gestureRecognizerManager)
		{
			this.PointerID = pointerID;
			_snapshots.Add(new(initialPoint, initialState, initialScroll ?? UIVector2.Zero));
		}

		public override string ToString()
			=> $"UITouch{{ID = {PointerID}, IsFinished = {IsFinished}, Snapshots = {Snapshots.Count}, Point = {LastPoint}, State = {Last.State}, Scroll = {Last.Scroll}, ActiveRecognizer = {ActiveRecognizer}}}";

		public void AddSnapshot(UIVector2 point, TPointerState state, UIVector2? scroll = null)
		{
			_snapshots.Add(new(point, state, scroll ?? UIVector2.Zero));
		}

		public override bool IsSame(UITouch? touch)
			=> touch is UITouch<TPointerID, TPointerState> other && PointerID.Equals(other.PointerID);

		public override UITouch GetTranslated(UIVector2 translation)
		{
			var touch = new UITouch<TPointerID, TPointerState>(GestureRecognizerManager, PointerID, FirstPoint - translation, First.State, First.Scroll);
			for (int i = 1; i < Snapshots.Count; i++)
				touch.AddSnapshot(Snapshots[i].Point - translation, Snapshots[i].State, Snapshots[i].Scroll);
			return touch;
		}
	}
}