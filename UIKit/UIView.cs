using Cassowary;
using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;

namespace Shockah.UIKit
{
	public class UIView: IConstrainable.Horizontal, IConstrainable.Vertical
	{
		public UIView ConstrainableOwnerView => this;

		public UIRoot? Root
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

		public UILayoutConstraintPriority HorizontalContentHuggingPriority { get; set; } = UILayoutConstraintPriority.Weak;
		public UILayoutConstraintPriority HorizontalCompressionResistancePriority { get; set; } = UILayoutConstraintPriority.Strong;
		public UILayoutConstraintPriority VerticalContentHuggingPriority { get; set; } = UILayoutConstraintPriority.Weak;
		public UILayoutConstraintPriority VerticalCompressionResistancePriority { get; set; } = UILayoutConstraintPriority.Strong;

		public bool IsVisible { get; set; } = true;

		public event ParentChildEvent<UIRoot, UIView>? AddedToRoot;
		public event ParentChildEvent<UIRoot, UIView>? RemovedFromRoot;
		public event ParentChildEvent<UIView, UIView>? AddedToSuperview;
		public event ParentChildEvent<UIView, UIView>? RemovedFromSuperview;
		public event ParentChildEvent<UIView, UIView>? AddedSubview;
		public event ParentChildEvent<UIView, UIView>? RemovedSubview;
		public event OwnerValueChangeEvent<UIView, (float? X, float? Y)>? IntrinsicSizeChanged;
		public event OwnerValueChangeEvent<UIView, UIVector2>? SizeChanged;
		public event OwnerCollectionValueEvent<UIView, UILayoutConstraint>? ConstraintAdded;
		public event OwnerCollectionValueEvent<UIView, UILayoutConstraint>? ConstraintRemoved;

		internal readonly ISet<UILayoutConstraint> _heldConstraints = new HashSet<UILayoutConstraint>();

		private UIRoot? _root;
		private readonly IList<UIView> _subviews = new List<UIView>();
		private float? _intrinsicWidth;
		private float? _intrinsicHeight;
		private readonly ISet<UILayoutConstraint> _constraints = new HashSet<UILayoutConstraint>();

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
		}

		public void AddSubview(UIView subview)
		{
			if (subview.Superview is not null)
				throw new InvalidOperationException($"Cannot add subview {subview}, as it's already added to {subview.Superview}.");
			_subviews.Add(subview);
			subview.Superview = this;
			subview.AddedToSuperview?.Invoke(this, subview);
			AddedSubview?.Invoke(this, subview);
			subview.Root = Root ?? this as UIRoot;
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

		private void OnAddedToRoot(UIRoot root)
		{
			root.AddViewVariables(this);

			root.QueueAddConstraint(RightAfterLeftConstraint.Value);
			root.QueueAddConstraint(BottomAfterTopConstraint.Value);
			foreach (var constraint in IntrinsicSizeConstraints)
				root.QueueAddConstraint(constraint);

			foreach (var constraint in _heldConstraints)
				root.QueueAddConstraint(constraint);
			foreach (var subview in _subviews)
				subview.Root = root;
		}

		private void OnRemovedFromRoot(UIRoot root)
		{
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

		public abstract class Drawable: UIView
		{
			public abstract void DrawSelf(RenderContext context);
		}
	}
}