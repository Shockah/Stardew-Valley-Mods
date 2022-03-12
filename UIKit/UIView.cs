using Cassowary;
using Shockah.CommonModCode;
using Shockah.UIKit.Geometry;
using Shockah.UIKit.Gesture;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit
{
	public class UIView: IConstrainable.Horizontal, IConstrainable.Vertical
	{
		public UIView ConstrainableOwnerView => this;

		public UIRootView? Root
		{
			get => _root;
			set
			{
				if (_root == value)
					return;
				var oldValue = _root;
				_root = value;

				if (value is null)
					RemovedFromRoot?.Invoke(oldValue!, this);
				else
					AddedToRoot?.Invoke(value, this);
			}
		}

		public UIView? Superview { get; private set; }
		public IReadOnlyList<UIView> Subviews => (IReadOnlyList<UIView>)_subviews;
		public IReadOnlySet<UILayoutConstraint> Constraints => (IReadOnlySet<UILayoutConstraint>)_constraints;
		public IReadOnlyList<UIGestureRecognizer> GestureRecognizers => (IReadOnlyList<UIGestureRecognizer>)_gestureRecognizers;

		public IEnumerable<UILayoutConstraint> AllConstraints
		{
			get
			{
				foreach (var constraint in _heldConstraints)
					yield return constraint;
				foreach (var subview in Subviews)
					foreach (var constraint in subview.AllConstraints)
						yield return constraint;
			}
		}

		public float X1 { get; set; } = 0f;
		public float Y1 { get; set; } = 0f;
		public float X2 { get; private set; } = 0f;
		public float Y2 { get; private set; } = 0f;

		public float Width
		{
			get => X2 - X1;
			set
			{
				if (value == Width)
					return;
				var oldValue = Width;
				X2 = X1 + value;
				SizeChanged?.Invoke(this, (oldValue, Height), (value, Height));
			}
		}

		public float Height
		{
			get => Y2 - Y1;
			set
			{
				if (value == Height)
					return;
				var oldValue = Height;
				Y2 = Y1 + value;
				SizeChanged?.Invoke(this, (Width, oldValue), (Width, value));
			}
		}

		public UIVector2 TopLeft => new(X1, Y1);
		public UIVector2 TopRight => new(X2, Y1);
		public UIVector2 BottomLeft => new(X1, Y2);
		public UIVector2 BottomRight => new(X2, Y2);

		public UIVector2 Size => new(Width, Height);

		public IUITypedAnchorWithOpposite<IConstrainable.Horizontal> LeftAnchor => LazyLeft.Value;
		public IUITypedAnchorWithOpposite<IConstrainable.Horizontal> RightAnchor => LazyRight.Value;
		public IUITypedAnchorWithOpposite<IConstrainable.Vertical> TopAnchor => LazyTop.Value;
		public IUITypedAnchorWithOpposite<IConstrainable.Vertical> BottomAnchor => LazyBottom.Value;
		public IUITypedAnchor<IConstrainable.Horizontal> WidthAnchor => LazyWidth.Value;
		public IUITypedAnchor<IConstrainable.Vertical> HeightAnchor => LazyHeight.Value;
		public IUITypedAnchor<IConstrainable.Horizontal> CenterXAnchor => LazyCenterX.Value;
		public IUITypedAnchor<IConstrainable.Vertical> CenterYAnchor => LazyCenterY.Value;

		public float? IntrinsicWidth
		{
			get => _intrinsicWidth;
			set
			{
				if (_intrinsicWidth == value)
					return;
				var oldValue = _intrinsicWidth;
				_intrinsicWidth = value;
				IntrinsicSizeChanged?.Invoke(this, (X: oldValue, Y: IntrinsicHeight), (X: value, Y: IntrinsicHeight));
			}
		}

		public float? IntrinsicHeight
		{
			get => _intrinsicWidth;
			set
			{
				if (_intrinsicHeight == value)
					return;
				var oldValue = _intrinsicHeight;
				_intrinsicHeight = value;
				IntrinsicSizeChanged?.Invoke(this, (X: IntrinsicWidth, Y: oldValue), (X: IntrinsicWidth, Y: value));
			}
		}

		public bool IsVisible
		{
			get => _isVisible;
			set
			{
				if (_isVisible == value)
					return;
				var oldValue = _isVisible;
				_isVisible = value;
				IsVisibleChanged?.Invoke(this, oldValue, value);
			}
		}

		public IReadOnlyList<UITouch> HoverPointers => (IReadOnlyList<UITouch>)_hoverPointers;
		public bool Hover => _hoverPointers.Count != 0;

		public UILayoutConstraintPriority HorizontalContentHuggingPriority { get; set; } = UILayoutConstraintPriority.Low;
		public UILayoutConstraintPriority HorizontalCompressionResistancePriority { get; set; } = UILayoutConstraintPriority.High;
		public UILayoutConstraintPriority VerticalContentHuggingPriority { get; set; } = UILayoutConstraintPriority.Low;
		public UILayoutConstraintPriority VerticalCompressionResistancePriority { get; set; } = UILayoutConstraintPriority.High;

		public bool ClipsSelfTouchesToBounds { get; set; } = true;
		public bool ClipsSubviewTouchesToBounds { get; set; } = false;
		public bool IsSelfTouchInteractionEnabled { get; set; } = false;
		public bool IsSubviewTouchInteractionEnabled { get; set; } = true;
		public bool IsTouchThrough { get; set; } = true;

		public event ParentChildEvent<UIRootView, UIView>? AddedToRoot;
		public event ParentChildEvent<UIRootView, UIView>? RemovedFromRoot;
		public event ParentChildEvent<UIView, UIView>? AddedToSuperview;
		public event ParentChildEvent<UIView, UIView>? RemovedFromSuperview;
		public event ParentChildEvent<UIView, UIView>? AddedSubview;
		public event ParentChildEvent<UIView, UIView>? RemovedSubview;
		public event OwnerValueChangeEvent<UIView, (float? X, float? Y)>? IntrinsicSizeChanged;
		public event OwnerValueChangeEvent<UIView, bool>? IsVisibleChanged;
		public event OwnerValueChangeEvent<UIView, IReadOnlyList<UITouch>>? HoverPointersChanged;
		public event OwnerValueChangeEvent<UIView, bool>? HoverChanged;
		public event OwnerValueChangeEvent<UIView, UIVector2>? SizeChanged;
		public event OwnerCollectionValueEvent<UIView, UILayoutConstraint>? ConstraintAdded;
		public event OwnerCollectionValueEvent<UIView, UILayoutConstraint>? ConstraintRemoved;

		internal readonly ISet<UILayoutConstraint> _heldConstraints = new HashSet<UILayoutConstraint>();

		private UIRootView? _root;
		private readonly IList<UIView> _subviews = new List<UIView>();
		private readonly ISet<UILayoutConstraint> _constraints = new HashSet<UILayoutConstraint>();
		private readonly IList<UIGestureRecognizer> _gestureRecognizers = new List<UIGestureRecognizer>();
		private bool _isVisible = true;
		private readonly IList<UITouch> _hoverPointers = new List<UITouch>();
		private float? _intrinsicWidth;
		private float? _intrinsicHeight;

		internal readonly Lazy<ClVariable> LeftVariable;
		internal readonly Lazy<ClVariable> RightVariable;
		internal readonly Lazy<ClVariable> TopVariable;
		internal readonly Lazy<ClVariable> BottomVariable;

		private readonly Lazy<UITypedAnchorWithOpposite<IConstrainable.Horizontal>> LazyLeft;
		private readonly Lazy<UITypedAnchorWithOpposite<IConstrainable.Horizontal>> LazyRight;
		private readonly Lazy<UITypedAnchorWithOpposite<IConstrainable.Vertical>> LazyTop;
		private readonly Lazy<UITypedAnchorWithOpposite<IConstrainable.Vertical>> LazyBottom;
		private readonly Lazy<UITypedAnchor<IConstrainable.Horizontal>> LazyWidth;
		private readonly Lazy<UITypedAnchor<IConstrainable.Vertical>> LazyHeight;
		private readonly Lazy<UITypedAnchor<IConstrainable.Horizontal>> LazyCenterX;
		private readonly Lazy<UITypedAnchor<IConstrainable.Vertical>> LazyCenterY;

		private readonly Lazy<UILayoutConstraint> RightAfterLeftConstraint;
		private readonly Lazy<UILayoutConstraint> BottomAfterTopConstraint;

		private IReadOnlyList<UILayoutConstraint> _intrinsicSizeConstraints = Array.Empty<UILayoutConstraint>();
		private IReadOnlyList<UILayoutConstraint> IntrinsicSizeConstraints
		{
			get => _intrinsicSizeConstraints;
			set
			{
				if (Root is not null)
				{
					foreach (var constraint in _intrinsicSizeConstraints)
						Root.QueueRemoveConstraint(constraint);
					foreach (var constraint in value)
						Root.QueueAddConstraint(constraint);
				}
				_intrinsicSizeConstraints = value;
			}
		}

		public UIView()
		{
			LeftVariable = new(() => new($"{this}.Left"));
			RightVariable = new(() => new($"{this}.Right"));
			TopVariable = new(() => new($"{this}.Top"));
			BottomVariable = new(() => new($"{this}.Bottom"));

			LazyLeft = new(() => new(this, new(LeftVariable.Value), "Left", c => c.LeftAnchor, c => c.RightAnchor));
			LazyRight = new(() => new(this, new(RightVariable.Value), "Right", c => c.RightAnchor, c => c.LeftAnchor));
			LazyTop = new(() => new(this, new(TopVariable.Value), "Top", c => c.TopAnchor, c => c.BottomAnchor));
			LazyBottom = new(() => new(this, new(BottomVariable.Value), "Bottom", c => c.BottomAnchor, c => c.TopAnchor));
			LazyWidth = new(() => new(this, new ClLinearExpression(RightVariable.Value).Minus(LeftVariable.Value), "Width", c => c.WidthAnchor));
			LazyHeight = new(() => new(this, new ClLinearExpression(BottomVariable.Value).Minus(TopVariable.Value), "Height", c => c.HeightAnchor));
			LazyCenterX = new(() => new(this, new ClLinearExpression(LeftVariable.Value).Plus(((IUIAnchor.Internal)WidthAnchor).Expression.Times(0.5)), "CenterX", c => c.CenterXAnchor));
			LazyCenterY = new(() => new(this, new ClLinearExpression(TopVariable.Value).Plus(((IUIAnchor.Internal)HeightAnchor).Expression.Times(0.5)), "CenterY", c => c.CenterYAnchor));

			RightAfterLeftConstraint = new(() => RightAnchor.MakeConstraintTo(LeftAnchor, relation: UILayoutConstraintRelation.GreaterThanOrEqual));
			BottomAfterTopConstraint = new(() => BottomAnchor.MakeConstraintTo(TopAnchor, relation: UILayoutConstraintRelation.GreaterThanOrEqual));

			AddedToRoot += (root, _) => OnAddedToRoot(root);
			RemovedFromRoot += (root, _) => OnRemovedFromRoot(root);
			IntrinsicSizeChanged += (_, _, newValue) => OnIntrinsicSizeChanged(newValue);
			HoverPointersChanged += (_, oldValue, newValue) => OnHoverPointersChanged(oldValue, newValue);
		}

		public void AddSubview(UIView subview)
		{
			InsertSubview(Subviews.Count, subview);
		}

		public void InsertSubview(int index, UIView subview)
		{
			if (subview.Superview is not null)
				throw new InvalidOperationException($"Cannot add subview {subview}, as it's already added to {subview.Superview}.");
			_subviews.Insert(index, subview);
			subview.Superview = this;
			subview.AddedToSuperview?.Invoke(this, subview);
			AddedSubview?.Invoke(this, subview);
			subview.Root = Root ?? this as UIRootView;
		}

		public void BringSubviewToFront(UIView subview)
		{
			if (subview.Superview != this)
				throw new InvalidOperationException($"View {subview} is not a subview of {this}.");
			_subviews.Remove(subview);
			_subviews.Add(subview);
		}

		public void SendSubviewToBack(UIView subview)
		{
			if (subview.Superview != this)
				throw new InvalidOperationException($"View {subview} is not a subview of {this}.");
			_subviews.Remove(subview);
			_subviews.Insert(0, subview);
		}

		public void PutSubviewAbove(UIView subview, UIView anotherSubview)
		{
			if (subview.Superview != this)
				throw new InvalidOperationException($"View {subview} is not a subview of {this}.");
			if (anotherSubview.Superview != this)
				throw new InvalidOperationException($"View {anotherSubview} is not a subview of {this}.");
			_subviews.Remove(subview);
			var index = _subviews.IndexOf(anotherSubview);
			if (index == -1)
				throw new InvalidOperationException($"View {anotherSubview} is not a subview of {this}.");
			_subviews.Insert(index + 1, subview);
		}

		public void PutSubviewBelow(UIView subview, UIView anotherSubview)
		{
			if (subview.Superview != this)
				throw new InvalidOperationException($"View {subview} is not a subview of {this}.");
			if (anotherSubview.Superview != this)
				throw new InvalidOperationException($"View {anotherSubview} is not a subview of {this}.");
			_subviews.Remove(subview);
			var index = _subviews.IndexOf(anotherSubview);
			if (index == -1)
				throw new InvalidOperationException($"View {anotherSubview} is not a subview of {this}.");
			_subviews.Insert(index, subview);
		}

		public void RemoveFromSuperview()
		{
			var superview = Superview;
			if (superview is null)
				return;
			foreach (var constraint in new List<UILayoutConstraint>(Constraints))
				constraint.Deactivate();
			superview._subviews.Remove(this);
			Superview = null;
			superview.RemovedSubview?.Invoke(superview, this);
			RemovedFromSuperview?.Invoke(superview, this);
			Root = null;
		}

		public void LayoutIfNeeded()
		{
			OnInternalLayoutIfNeeded();
		}

		internal virtual void OnInternalLayoutIfNeeded()
		{
			if (Root is null)
				return;

			var oldWidth = Width;
			var oldHeight = Height;
			Root.SolveLayout();
			X1 = (float)(LeftVariable.Value.Value - (Superview?.LeftVariable?.Value?.Value ?? 0));
			Y1 = (float)(TopVariable.Value.Value - (Superview?.TopVariable?.Value?.Value ?? 0));
			X2 = (float)(RightVariable.Value.Value - (Superview?.LeftVariable?.Value?.Value ?? 0));
			Y2 = (float)(BottomVariable.Value.Value - (Superview?.TopVariable?.Value?.Value ?? 0));
			if (oldWidth != Width || oldHeight != Height)
				SizeChanged?.Invoke(this, (oldWidth, oldHeight), (Width, Height));

			OnLayoutIfNeeded();
			foreach (var subview in Subviews)
				subview.LayoutIfNeeded();
		}

		public virtual void OnLayoutIfNeeded()
		{
		}

		public void UpdateConstraints()
		{
			foreach (var subview in Subviews)
				subview.UpdateConstraints();
			OnUpdateConstraints();
		}

		public virtual void OnUpdateConstraints()
		{
		}

		public void DrawInParentContext(RenderContext context)
		{
			if (!IsVisible)
				return;

			var newContext = context.GetTranslated(X1, Y1);
			DrawInSelfContext(newContext);
		}

		public void DrawInSelfContext(RenderContext context)
		{
			if (!IsVisible)
				return;

			(this as Drawable)?.DrawSelf(context);
			DrawChildren(context);
		}

		public virtual void DrawChildren(RenderContext context)
		{
			foreach (var subview in Subviews)
				subview.DrawInParentContext(context);
		}

		public void AddGestureRecognizer(UIGestureRecognizer recognizer)
		{
			if (recognizer.Owner is not null)
				throw new ArgumentException($"Gesture recognizer {recognizer} already has an owner.");
			if (_gestureRecognizers.Contains(recognizer))
				return;
			_gestureRecognizers.Add(recognizer);
			recognizer.Owner = this;
			Root?.GestureRecognizerManager?.AddGestureRecognizer(recognizer);
		}

		public void RemoveGestureRecognizer(UIGestureRecognizer recognizer)
		{
			if (recognizer.Owner != this)
				return;
			recognizer.Owner = null;
			_gestureRecognizers.Remove(recognizer);
			Root?.GestureRecognizerManager?.RemoveGestureRecognizer(recognizer);
		}

		public virtual bool IsTouchInBounds(UITouch touch)
		{
			return touch.LastPoint.X >= X1 && touch.LastPoint.Y >= Y1 && touch.LastPoint.X < X2 && touch.LastPoint.Y < Y2;
		}

		public void RemoveHover(UITouch touch)
		{
			var indexToRemove = _hoverPointers.FirstIndex(t => t.IsSame(touch));
			if (indexToRemove is not null)
				_hoverPointers.RemoveAt(indexToRemove.Value);
		}

		public IEnumerable<UIView> GetHoveredViewsAndUpdateHover(UITouch touch)
		{
			if (!IsVisible)
				yield break;

			var isTouchInBounds = this.IsTouchInBounds(touch);
			if (IsSubviewTouchInteractionEnabled && (!ClipsSubviewTouchesToBounds || isTouchInBounds))
			{
				var translatedTouch = touch.GetTranslated(X1, Y1);
				foreach (var subview in Subviews.Reverse().ToList())
					foreach (var hoveredView in subview.GetHoveredViewsAndUpdateHover(translatedTouch))
						yield return hoveredView;
			}
			if (isTouchInBounds)
			{
				var indexToUpdate = _hoverPointers.FirstIndex(t => t.IsSame(touch));
				if (indexToUpdate is null)
				{
					var oldValue = _hoverPointers.ToList();
					_hoverPointers.Add(touch);
					HoverPointersChanged?.Invoke(this, oldValue, (IReadOnlyList<UITouch>)_hoverPointers);
				}
				else
				{
					_hoverPointers[indexToUpdate.Value] = touch;
				}

				if (IsSelfTouchInteractionEnabled)
					yield return this;
			}
			else
			{
				var indexToRemove = _hoverPointers.FirstIndex(t => t.IsSame(touch));
				if (indexToRemove is not null)
				{
					var oldValue = _hoverPointers.ToList();
					_hoverPointers.RemoveAt(indexToRemove.Value);
					HoverPointersChanged?.Invoke(this, oldValue, (IReadOnlyList<UITouch>)_hoverPointers);
				}
			}
		}

		public virtual bool OnTouchDown(UITouch touch, bool isHandled = false)
		{
			if (!IsVisible)
				return isHandled;

			var isTouchInBounds = (ClipsSelfTouchesToBounds || ClipsSubviewTouchesToBounds) ? this.IsTouchInBounds(touch) : true;
			if (IsSubviewTouchInteractionEnabled && (!ClipsSubviewTouchesToBounds || isTouchInBounds))
			{
				var translatedTouch = touch.GetTranslated(X1, Y1);
				foreach (var subview in Subviews.Reverse().ToList())
					isHandled |= subview.OnTouchDown(translatedTouch, isHandled);
			}
			if (IsSelfTouchInteractionEnabled && !isHandled && (!ClipsSelfTouchesToBounds || isTouchInBounds))
			{
				foreach (var recognizer in GestureRecognizers)
				{
					recognizer.OnTouchDown(touch);
					if (touch.ActiveRecognizer == recognizer)
					{
						isHandled = true;
						break;
					}
				}
			}
			if (!IsTouchThrough && isTouchInBounds)
				isHandled = true;
			return isHandled;
		}

		public virtual void OnTouchChanged(UITouch touch)
		{
			if (!IsVisible)
				return;

			if (IsSubviewTouchInteractionEnabled)
			{
				var translatedTouch = touch.GetTranslated(X1, Y1);
				foreach (var subview in Subviews.Reverse().ToList())
					subview.OnTouchChanged(translatedTouch);
			}
			if (IsSelfTouchInteractionEnabled)
			{
				foreach (var recognizer in GestureRecognizers)
					recognizer.OnTouchChanged(touch);
			}
		}

		public virtual void OnTouchUp(UITouch touch)
		{
			if (!IsVisible)
				return;

			if (IsSubviewTouchInteractionEnabled)
			{
				var translatedTouch = touch.GetTranslated(X1, Y1);
				foreach (var subview in Subviews.Reverse().ToList())
					subview.OnTouchUp(translatedTouch);
			}
			if (IsSelfTouchInteractionEnabled)
			{
				foreach (var recognizer in GestureRecognizers)
					recognizer.OnTouchUp(touch);
			}
		}

		internal void AddConstraint(UILayoutConstraint constraint)
		{
			if (_constraints.Add(constraint))
				ConstraintAdded?.Invoke(this, constraint);
		}

		internal void RemoveConstraint(UILayoutConstraint constraint)
		{
			if (_constraints.Remove(constraint))
				ConstraintRemoved?.Invoke(this, constraint);
		}

		private void OnAddedToRoot(UIRootView root)
		{
			root.AddViewVariables(this);

			root.QueueAddConstraint(RightAfterLeftConstraint.Value);
			root.QueueAddConstraint(BottomAfterTopConstraint.Value);
			foreach (var constraint in IntrinsicSizeConstraints)
				root.QueueAddConstraint(constraint);
			foreach (var constraint in _heldConstraints)
				root.QueueAddConstraint(constraint);

			foreach (var recognizer in GestureRecognizers)
				root.GestureRecognizerManager.AddGestureRecognizer(recognizer);

			foreach (var subview in _subviews)
				subview.Root = root;
		}

		private void OnRemovedFromRoot(UIRootView root)
		{
			foreach (var recognizer in GestureRecognizers)
				root.GestureRecognizerManager.RemoveGestureRecognizer(recognizer);

			foreach (var constraint in _heldConstraints)
				root.QueueRemoveConstraint(constraint);
			foreach (var subview in _subviews)
				subview.Root = root;

			root.QueueRemoveConstraint(RightAfterLeftConstraint.Value);
			root.QueueRemoveConstraint(BottomAfterTopConstraint.Value);
			foreach (var constraint in IntrinsicSizeConstraints)
				root.QueueRemoveConstraint(constraint);

			root.RemoveViewVariables(this);
		}

		private void OnIntrinsicSizeChanged((float? X, float? Y) intrinsicSize)
		{
			var newConstraints = new List<UILayoutConstraint>();
			if (intrinsicSize.X is not null)
			{
				newConstraints.Add(((IUIAnchor.Internal)WidthAnchor).MakeConstraint(
					intrinsicSize.X.Value,
					UILayoutConstraintRelation.LessThanOrEqual,
					HorizontalContentHuggingPriority
				));
				newConstraints.Add(((IUIAnchor.Internal)WidthAnchor).MakeConstraint(
					intrinsicSize.X.Value,
					UILayoutConstraintRelation.GreaterThanOrEqual,
					HorizontalCompressionResistancePriority
				));
			}
			if (intrinsicSize.Y is not null)
			{
				newConstraints.Add(((IUIAnchor.Internal)HeightAnchor).MakeConstraint(
					intrinsicSize.Y.Value,
					UILayoutConstraintRelation.LessThanOrEqual,
					VerticalContentHuggingPriority
				));
				newConstraints.Add(((IUIAnchor.Internal)HeightAnchor).MakeConstraint(
					intrinsicSize.Y.Value,
					UILayoutConstraintRelation.GreaterThanOrEqual,
					VerticalCompressionResistancePriority
				));
			}
			IntrinsicSizeConstraints = newConstraints;
		}

		private void OnHoverPointersChanged(IReadOnlyList<UITouch> oldValue, IReadOnlyList<UITouch> newValue)
		{
			var oldEmpty = oldValue.Count == 0;
			var newEmpty = newValue.Count == 0;
			if (oldEmpty != newEmpty)
				HoverChanged?.Invoke(this, !oldEmpty, !newEmpty);
		}

		public abstract class Drawable: UIView
		{
			public abstract void DrawSelf(RenderContext context);
		}
	}
}