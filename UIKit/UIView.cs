using Cassowary;
using Shockah.CommonModCode;
using Shockah.UIKit.Geometry;
using Shockah.UIKit.Gesture;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Shockah.UIKit
{
	public enum HoverState { None, Obscured, Direct }

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
		public IReadOnlySet<IUILayoutConstraint> Constraints => (IReadOnlySet<IUILayoutConstraint>)_constraints;
		public IReadOnlyList<UIGestureRecognizer> GestureRecognizers => (IReadOnlyList<UIGestureRecognizer>)_gestureRecognizers;

		public IEnumerable<IUILayoutConstraint> AllDownstreamConstraints
		{
			get
			{
				foreach (var constraint in HeldConstraints)
					yield return constraint;
				foreach (var subview in Subviews)
					foreach (var constraint in subview.AllDownstreamConstraints)
						yield return constraint;
			}
		}

		public IEnumerable<IUILayoutConstraint> AllUpstreamConstraints
		{
			get
			{
				foreach (var constraint in HeldConstraints)
					yield return constraint;
				if (Superview is not null)
					foreach (var constraint in Superview.AllUpstreamConstraints)
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

		public IUIAnchor.Typed<IConstrainable.Horizontal>.Positional.WithOpposite LeftAnchor => LazyLeft.Value;
		public IUIAnchor.Typed<IConstrainable.Horizontal>.Positional.WithOpposite RightAnchor => LazyRight.Value;
		public IUIAnchor.Typed<IConstrainable.Vertical>.Positional.WithOpposite TopAnchor => LazyTop.Value;
		public IUIAnchor.Typed<IConstrainable.Vertical>.Positional.WithOpposite BottomAnchor => LazyBottom.Value;
		public IUIAnchor.Typed<IConstrainable.Horizontal>.Length WidthAnchor => LazyWidth.Value;
		public IUIAnchor.Typed<IConstrainable.Vertical>.Length HeightAnchor => LazyHeight.Value;
		public IUIAnchor.Typed<IConstrainable.Horizontal>.Positional CenterXAnchor => LazyCenterX.Value;
		public IUIAnchor.Typed<IConstrainable.Vertical>.Positional CenterYAnchor => LazyCenterY.Value;

		public float? IntrinsicWidth
		{
			get => _intrinsicWidth;
			protected set
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
			get => _intrinsicHeight;
			protected set
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

		public IReadOnlyList<(UITouch touch, HoverState hover)> HoverPointers => (IReadOnlyList<(UITouch touch, HoverState hover)>)_hoverPointers;
		public HoverState Hover => _hoverPointers.Count == 0 ? HoverState.None : (HoverState)_hoverPointers.Max(p => (int)p.hover);

		public UILayoutConstraintPriority HorizontalContentHuggingPriority
		{
			get => _horizontalContentHuggingPriority;
			set
			{
				if (_horizontalContentHuggingPriority == value)
					return;
				_horizontalContentHuggingPriority = value;
				UpdateIntrinsicSizeConstraints();
			}
		}

		public UILayoutConstraintPriority HorizontalCompressionResistancePriority
		{
			get => _horizontalCompressionResistancePriority;
			set
			{
				if (_horizontalCompressionResistancePriority == value)
					return;
				_horizontalCompressionResistancePriority = value;
				UpdateIntrinsicSizeConstraints();
			}
		}

		public UILayoutConstraintPriority VerticalContentHuggingPriority
		{
			get => _verticalContentHuggingPriority;
			set
			{
				if (_verticalContentHuggingPriority == value)
					return;
				_verticalContentHuggingPriority = value;
				UpdateIntrinsicSizeConstraints();
			}
		}

		public UILayoutConstraintPriority VerticalCompressionResistancePriority
		{
			get => _verticalCompressionResistancePriority;
			set
			{
				if (_verticalCompressionResistancePriority == value)
					return;
				_verticalCompressionResistancePriority = value;
				UpdateIntrinsicSizeConstraints();
			}
		}

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
		public event OwnerValueChangeEvent<UIView, IReadOnlyList<(UITouch touch, HoverState hover)>>? HoverPointersChanged;
		public event OwnerValueChangeEvent<UIView, HoverState>? HoverChanged;
		public event OwnerValueChangeEvent<UIView, UIVector2>? SizeChanged;
		public event OwnerCollectionValueEvent<UIView, IUILayoutConstraint>? ConstraintAdded;
		public event OwnerCollectionValueEvent<UIView, IUILayoutConstraint>? ConstraintRemoved;

		public ISet<IUILayoutConstraint> HeldConstraints { get; set; } = new HashSet<IUILayoutConstraint>();

		private UIRootView? _root;
		private readonly IList<UIView> _subviews = new List<UIView>();
		private readonly ISet<IUILayoutConstraint> _constraints = new HashSet<IUILayoutConstraint>();
		private readonly IList<UIGestureRecognizer> _gestureRecognizers = new List<UIGestureRecognizer>();
		private bool _isVisible = true;
		private readonly IList<(UITouch touch, HoverState hover)> _hoverPointers = new List<(UITouch touch, HoverState hover)>();
		private float? _intrinsicWidth;
		private float? _intrinsicHeight;

		internal readonly Lazy<ClVariable> LeftVariable;
		internal readonly Lazy<ClVariable> RightVariable;
		internal readonly Lazy<ClVariable> TopVariable;
		internal readonly Lazy<ClVariable> BottomVariable;

		private readonly Lazy<UIEdgeAnchor<IConstrainable.Horizontal>> LazyLeft;
		private readonly Lazy<UIEdgeAnchor<IConstrainable.Horizontal>> LazyRight;
		private readonly Lazy<UIEdgeAnchor<IConstrainable.Vertical>> LazyTop;
		private readonly Lazy<UIEdgeAnchor<IConstrainable.Vertical>> LazyBottom;
		private readonly Lazy<UILengthAnchor<IConstrainable.Horizontal>> LazyWidth;
		private readonly Lazy<UILengthAnchor<IConstrainable.Vertical>> LazyHeight;
		private readonly Lazy<UICenterAnchor<IConstrainable.Horizontal>> LazyCenterX;
		private readonly Lazy<UICenterAnchor<IConstrainable.Vertical>> LazyCenterY;

		private readonly Lazy<UILayoutConstraint> RightAfterLeftConstraint;
		private readonly Lazy<UILayoutConstraint> BottomAfterTopConstraint;

		private UILayoutConstraintPriority _horizontalContentHuggingPriority = UILayoutConstraintPriority.Low;
		private UILayoutConstraintPriority _horizontalCompressionResistancePriority = UILayoutConstraintPriority.High;
		private UILayoutConstraintPriority _verticalContentHuggingPriority = UILayoutConstraintPriority.Low;
		private UILayoutConstraintPriority _verticalCompressionResistancePriority = UILayoutConstraintPriority.High;

		private IReadOnlyList<UILayoutConstraint> _intrinsicSizeConstraints = Array.Empty<UILayoutConstraint>();
		private IReadOnlyList<UILayoutConstraint> IntrinsicSizeConstraints
		{
			get => _intrinsicSizeConstraints;
			set
			{
				_intrinsicSizeConstraints.Deactivate();
				value.Activate();
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
			LazyCenterX = new(() => new(this, new ClLinearExpression(LeftVariable.Value).Plus(WidthAnchor.Expression.Times(0.5)), "CenterX", c => c.CenterXAnchor));
			LazyCenterY = new(() => new(this, new ClLinearExpression(TopVariable.Value).Plus(HeightAnchor.Expression.Times(0.5)), "CenterY", c => c.CenterYAnchor));

			RightAfterLeftConstraint = new(() => RightAnchor.MakeConstraintTo(LeftAnchor, relation: UILayoutConstraintRelation.GreaterThanOrEqual));
			BottomAfterTopConstraint = new(() => BottomAnchor.MakeConstraintTo(TopAnchor, relation: UILayoutConstraintRelation.GreaterThanOrEqual));

			AddedToRoot += (root, _) => OnAddedToRoot(root);
			RemovedFromRoot += (root, _) => OnRemovedFromRoot(root);
			IntrinsicSizeChanged += (_, _, newValue) => UpdateIntrinsicSizeConstraints();
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
			foreach (var constraint in new List<IUILayoutConstraint>(Constraints))
				constraint.Deactivate();
			superview._subviews.Remove(this);
			Superview = null;
			superview.RemovedSubview?.Invoke(superview, this);
			RemovedFromSuperview?.Invoke(superview, this);
			Root = null;
		}

		public UIVector2 GetOptimalSize(
			UIOptimalSideLength horizontalLength,
			UIOptimalSideLength verticalLength,
			UILayoutConstraintPriority? horizontalPriority = null,
			UILayoutConstraintPriority? verticalPriority = null,
			bool ignoringIntrinsicWidth = true,
			bool ignoringIntrinsicHeight = true
		)
		{
			if (Root is null)
				throw new NotImplementedException($"{nameof(GetOptimalSize)} is not currently supported without a root view.");
			horizontalPriority ??= UILayoutConstraintPriority.OptimalCalculations;
			verticalPriority ??= UILayoutConstraintPriority.OptimalCalculations;

			UILayoutConstraint horizontalConstraint;
			{
				if (horizontalLength is UIOptimalSideLength.LengthType typed)
					horizontalConstraint = WidthAnchor.MakeConstraint("optimalCalculations-width-const", typed.Value, priority: horizontalPriority);
				else if (horizontalLength is UIOptimalSideLength.CompressedType)
					horizontalConstraint = WidthAnchor.MakeConstraint("optimalCalculations-width-compressed", 0f, priority: horizontalPriority);
				else if (horizontalLength is UIOptimalSideLength.ExpandedType)
					horizontalConstraint = WidthAnchor.MakeConstraint("optimalCalculations-width-expanded", 1_000_000_000f, priority: horizontalPriority);
				else
					throw new ArgumentException($"{nameof(UIOptimalSideLength)} has an invalid value.");
			}

			UILayoutConstraint verticalConstraint;
			{
				if (verticalLength is UIOptimalSideLength.LengthType typed)
					verticalConstraint = HeightAnchor.MakeConstraint("optimalCalculations-height-const", typed.Value, priority: horizontalPriority);
				else if (verticalLength is UIOptimalSideLength.CompressedType)
					verticalConstraint = HeightAnchor.MakeConstraint("optimalCalculations-height-compressed", 0f, priority: verticalPriority);
				else if (verticalLength is UIOptimalSideLength.ExpandedType)
					verticalConstraint = HeightAnchor.MakeConstraint("optimalCalculations-height-expanded", 1_000_000_000f, priority: verticalPriority);
				else
					throw new ArgumentException($"{nameof(UIOptimalSideLength)} has an invalid value.");
			}

			if (ignoringIntrinsicWidth)
				IntrinsicSizeConstraints.Where(c => c.Anchor1 == WidthAnchor).Deactivate();
			if (ignoringIntrinsicHeight)
				IntrinsicSizeConstraints.Where(c => c.Anchor1 == HeightAnchor).Deactivate();

			horizontalConstraint.Activate();
			verticalConstraint.Activate();
			Root.SolveLayout();
			UIVector2 result = new(
				(float)(RightVariable.Value.Value - LeftVariable.Value.Value),
				(float)(BottomVariable.Value.Value - TopVariable.Value.Value)
			);

			horizontalConstraint.Deactivate();
			verticalConstraint.Deactivate();

			if (ignoringIntrinsicWidth)
				IntrinsicSizeConstraints.Where(c => c.Anchor1 == WidthAnchor).Activate();
			if (ignoringIntrinsicHeight)
				IntrinsicSizeConstraints.Where(c => c.Anchor1 == HeightAnchor).Activate();

			Root.SolveLayout();
			return result;
		}

		public void LayoutIfNeeded()
		{
			OnLayoutIfNeeded();
			foreach (var subview in Subviews)
				subview.LayoutIfNeeded();
		}

		protected virtual void OnLayoutIfNeeded()
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
		}

		public void UpdateConstraints()
		{
			foreach (var subview in Subviews)
				subview.UpdateConstraints();
			OnUpdateConstraints();
		}

		protected virtual void OnUpdateConstraints()
		{
		}

		public void Draw(RenderContext context)
		{
			if (!IsVisible)
				return;

			(this as Drawable)?.DrawSelf(context);
			Root?.FireRenderedViewEvent(this, context);
			DrawChildren(context);
		}

		public virtual void DrawChildren(RenderContext context)
		{
			foreach (var subview in Subviews)
				subview.Draw(context.GetTranslated(GetSubviewTranslation(subview)));
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

		[Pure]
		public virtual bool IsTouchInBounds(UITouch touch)
		{
			return touch.LastPoint.X >= 0f && touch.LastPoint.Y >= 0f && touch.LastPoint.X < Width && touch.LastPoint.Y < Height;
		}

		public void RemoveHover(UITouch touch)
		{
			var indexToRemove = _hoverPointers.FirstIndex(t => t.touch.IsSame(touch));
			if (indexToRemove is not null)
				_hoverPointers.RemoveAt(indexToRemove.Value);
		}

		public virtual UIVector2 GetSubviewTranslation(UIView subview)
			=> subview.TopLeft;

		public virtual bool OnUpdateHover(UITouch touch, bool isHandled = false, bool isTouchInParentBounds = true)
		{
			if (!IsVisible)
				return isHandled;

			var isTouchInBounds = this.IsTouchInBounds(touch);
			if (IsSubviewTouchInteractionEnabled)
			{
				var newIsTouchInParentBounds = ClipsSubviewTouchesToBounds ? isTouchInParentBounds && isTouchInBounds : isTouchInParentBounds;
				foreach (var subview in Subviews.Reverse().ToList())
					isHandled |= subview.OnUpdateHover(touch.GetTranslated(GetSubviewTranslation(subview)), isHandled, newIsTouchInParentBounds);
			}
			if (isTouchInBounds && isTouchInParentBounds)
			{
				var indexToUpdate = _hoverPointers.FirstIndex(t => t.touch.IsSame(touch));
				if (indexToUpdate is null)
				{
					var oldValue = _hoverPointers.ToList();
					_hoverPointers.Add((touch: touch, hover: isHandled ? HoverState.Obscured : HoverState.Direct));
					HoverPointersChanged?.Invoke(this, oldValue, (IReadOnlyList<(UITouch touch, HoverState hover)>)_hoverPointers);
				}
				else
				{
					_hoverPointers[indexToUpdate.Value] = (touch: touch, hover: isHandled ? HoverState.Obscured : HoverState.Direct);
				}
				isHandled = OnSelfHover(touch);
			}
			else
			{
				var indexToRemove = _hoverPointers.FirstIndex(t => t.touch.IsSame(touch));
				if (indexToRemove is not null)
				{
					var oldValue = _hoverPointers.ToList();
					_hoverPointers.RemoveAt(indexToRemove.Value);
					HoverPointersChanged?.Invoke(this, oldValue, (IReadOnlyList<(UITouch touch, HoverState hover)>)_hoverPointers);
				}
			}
			return isHandled;
		}

		public virtual bool OnSelfHover(UITouch touch)
		{
			return false;
		}

		public virtual bool OnTouchDown(UITouch touch, bool isHandled = false)
		{
			if (!IsVisible)
				return isHandled;

#pragma warning disable IDE0075 // Simplify conditional expression
			var isTouchInBounds = (ClipsSelfTouchesToBounds || ClipsSubviewTouchesToBounds) ? this.IsTouchInBounds(touch) : true;
#pragma warning restore IDE0075 // Simplify conditional expression
			if (IsSubviewTouchInteractionEnabled && (!ClipsSubviewTouchesToBounds || isTouchInBounds))
			{
				foreach (var subview in Subviews.Reverse().ToList())
					isHandled |= subview.OnTouchDown(touch.GetTranslated(GetSubviewTranslation(subview)), isHandled);
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
				foreach (var subview in Subviews.Reverse().ToList())
					subview.OnTouchChanged(touch.GetTranslated(GetSubviewTranslation(subview)));
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
				foreach (var subview in Subviews.Reverse().ToList())
					subview.OnTouchUp(touch.GetTranslated(GetSubviewTranslation(subview)));
			}
			if (IsSelfTouchInteractionEnabled)
			{
				foreach (var recognizer in GestureRecognizers)
					recognizer.OnTouchUp(touch);
			}
		}

		internal void AddConstraint(IUILayoutConstraint constraint)
		{
			if (_constraints.Add(constraint))
				ConstraintAdded?.Invoke(this, constraint);
		}

		internal void RemoveConstraint(IUILayoutConstraint constraint)
		{
			if (_constraints.Remove(constraint))
				ConstraintRemoved?.Invoke(this, constraint);
		}

		private void OnAddedToRoot(UIRootView root)
		{
			root.AddVariables(LeftVariable.Value, RightVariable.Value, TopVariable.Value, BottomVariable.Value);

			root.QueueAddConstraint(RightAfterLeftConstraint.Value);
			root.QueueAddConstraint(BottomAfterTopConstraint.Value);
			foreach (var constraint in HeldConstraints)
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

			foreach (var constraint in HeldConstraints)
				root.QueueRemoveConstraint(constraint);
			foreach (var subview in _subviews)
				subview.Root = root;

			root.QueueRemoveConstraint(RightAfterLeftConstraint.Value);
			root.QueueRemoveConstraint(BottomAfterTopConstraint.Value);

			root.RemoveVariables(LeftVariable.Value, RightVariable.Value, TopVariable.Value, BottomVariable.Value);
		}

		private void UpdateIntrinsicSizeConstraints()
		{
			var newConstraints = new List<UILayoutConstraint>();
			if (IntrinsicWidth is not null)
			{
				newConstraints.Add(WidthAnchor.MakeConstraint(
					"intrinsicWidth-contentHugging",
					IntrinsicWidth.Value,
					UILayoutConstraintRelation.LessThanOrEqual,
					HorizontalContentHuggingPriority
				));
				newConstraints.Add(WidthAnchor.MakeConstraint(
					"intrinsicWidth-compressionResistance",
					IntrinsicWidth.Value,
					UILayoutConstraintRelation.GreaterThanOrEqual,
					HorizontalCompressionResistancePriority
				));
			}
			if (IntrinsicHeight is not null)
			{
				newConstraints.Add(HeightAnchor.MakeConstraint(
					"intrinsicHeight-contentHugging",
					IntrinsicHeight.Value,
					UILayoutConstraintRelation.LessThanOrEqual,
					VerticalContentHuggingPriority
				));
				newConstraints.Add(HeightAnchor.MakeConstraint(
					"intrinsicHeight-compressionResistance",
					IntrinsicHeight.Value,
					UILayoutConstraintRelation.GreaterThanOrEqual,
					VerticalCompressionResistancePriority
				));
			}
			IntrinsicSizeConstraints = newConstraints;
		}

		private void OnHoverPointersChanged(IReadOnlyList<(UITouch touch, HoverState hover)> oldValue, IReadOnlyList<(UITouch touch, HoverState hover)> newValue)
		{
			var oldState = oldValue.Count == 0 ? HoverState.None : (HoverState)oldValue.Max(p => (int)p.hover);
			var newState = newValue.Count == 0 ? HoverState.None : (HoverState)newValue.Max(p => (int)p.hover);
			if (oldState != newState)
				HoverChanged?.Invoke(this, oldState, newState);
		}

		public abstract class Drawable: UIView
		{
			public abstract void DrawSelf(RenderContext context);
		}
	}
}