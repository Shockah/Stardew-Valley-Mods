using Cassowary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

		public float AbsoluteX1
		{
			get => X1 + (Superview?.AbsoluteX1 ?? 0f);
			set => X1 = value - (Superview?.AbsoluteX1 ?? 0f);
		}

		public float AbsoluteY1
		{
			get => Y1 + (Superview?.AbsoluteY1 ?? 0f);
			set => Y1 = value - (Superview?.AbsoluteY1 ?? 0f);
		}

		public float AbsoluteX2
		{
			get => X2 + (Superview?.AbsoluteX1 ?? 0f);
			set => X2 = value - (Superview?.AbsoluteX1 ?? 0f);
		}

		public float AbsoluteY2
		{
			get => Y2 + (Superview?.AbsoluteY1 ?? 0f);
			set => Y2 = value - (Superview?.AbsoluteY1 ?? 0f);
		}

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

		public Vector2 TopLeft => new(X1, Y1);
		public Vector2 TopRight => new(X2, Y1);
		public Vector2 BottomLeft => new(X1, Y2);
		public Vector2 BottomRight => new(X2, Y2);

		public Vector2 AbsoluteTopLeft => new(AbsoluteX1, AbsoluteY1);
		public Vector2 AbsoluteTopRight => new(AbsoluteX2, AbsoluteY1);
		public Vector2 AbsoluteBottomLeft => new(AbsoluteX1, AbsoluteY2);
		public Vector2 AbsoluteBottomRight => new(AbsoluteX2, AbsoluteY2);

		public Vector2 Size => new(Width, Height);

		public UITypedAnchorWithOpposite<IConstrainable.Horizontal> LeftAnchor => LazyLeft.Value;
		public UITypedAnchorWithOpposite<IConstrainable.Horizontal> RightAnchor => LazyRight.Value;
		public UITypedAnchorWithOpposite<IConstrainable.Vertical> TopAnchor => LazyTop.Value;
		public UITypedAnchorWithOpposite<IConstrainable.Vertical> BottomAnchor => LazyBottom.Value;
		public UITypedAnchor<IConstrainable.Horizontal> WidthAnchor => LazyWidth.Value;
		public UITypedAnchor<IConstrainable.Vertical> HeightAnchor => LazyHeight.Value;
		public UITypedAnchor<IConstrainable.Horizontal> CenterXAnchor => LazyCenterX.Value;
		public UITypedAnchor<IConstrainable.Vertical> CenterYAnchor => LazyCenterY.Value;

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

		public ClStrength HorizontalContentHuggingStrength { get; set; } = ClStrength.Medium;
		public ClStrength HorizontalCompressionResistanceStrength { get; set; } = ClStrength.Strong;
		public ClStrength VerticalContentHuggingStrength { get; set; } = ClStrength.Medium;
		public ClStrength VerticalCompressionResistanceStrength { get; set; } = ClStrength.Strong;

		public bool IsVisible { get; set; } = true;

		public event ParentChildEvent<UIRoot, UIView>? AddedToRoot;
		public event ParentChildEvent<UIRoot, UIView>? RemovedFromRoot;
		public event ParentChildEvent<UIView, UIView>? AddedToSuperview;
		public event ParentChildEvent<UIView, UIView>? RemovedFromSuperview;
		public event ParentChildEvent<UIView, UIView>? AddedSubview;
		public event ParentChildEvent<UIView, UIView>? RemovedSubview;
		public event OwnerValueChangeEvent<UIView, (float? X, float? Y)>? IntrinsicSizeChanged;
		public event OwnerValueChangeEvent<UIView, (float X, float Y)>? SizeChanged;
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

		private readonly Lazy<ClConstraint> RightAfterLeftConstraint;
		private readonly Lazy<ClConstraint> BottomAfterTopConstraint;

		private IReadOnlyList<ClConstraint> _intrinsicSizeConstraints = Array.Empty<ClConstraint>();
		private IReadOnlyList<ClConstraint> IntrinsicSizeConstraints
		{
			get => _intrinsicSizeConstraints;
			set
			{
				if (Root is not null)
				{
					foreach (var constraint in _intrinsicSizeConstraints)
						Root.ConstraintSolver.RemoveConstraint(constraint);
					foreach (var constraint in value)
						Root.ConstraintSolver.TryAddConstraint(constraint);
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
			LazyCenterX = new(() => new(this, new ClLinearExpression(LeftVariable.Value).Plus(WidthAnchor.Expression.Times(0.5)), "CenterX", c => c.CenterXAnchor));
			LazyCenterY = new(() => new(this, new ClLinearExpression(TopVariable.Value).Plus(HeightAnchor.Expression.Times(0.5)), "CenterY", c => c.CenterYAnchor));

			RightAfterLeftConstraint = new(() => new ClLinearInequality(RightVariable.Value, Cl.Operator.GreaterThanOrEqualTo, LeftVariable.Value));
			BottomAfterTopConstraint = new(() => new ClLinearInequality(BottomVariable.Value, Cl.Operator.GreaterThanOrEqualTo, TopVariable.Value));

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

		public virtual void LayoutIfNeeded()
		{
			if (Root is null)
				return;

			foreach (var subview in Subviews)
				subview.LayoutIfNeeded();

			var oldWidth = Width;
			var oldHeight = Height;
			Root.ConstraintSolver.Solve();
			AbsoluteX1 = (float)LeftVariable.Value.Value;
			AbsoluteY1 = (float)TopVariable.Value.Value;
			AbsoluteX2 = (float)RightVariable.Value.Value;
			AbsoluteY2 = (float)BottomVariable.Value.Value;
			if (oldWidth != Width || oldHeight != Height)
				SizeChanged?.Invoke(this, (oldWidth, oldHeight), (Width, Height));
		}

		public void Draw(SpriteBatch b)
		{
			if (!IsVisible)
				return;
			DrawSelf(b);
			DrawChildren(b);
		}

		public virtual void DrawSelf(SpriteBatch b)
		{
		}

		public virtual void DrawChildren(SpriteBatch b)
		{
			foreach (var subview in Subviews)
				subview.Draw(b);
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
			root.ConstraintSolver.TryAddConstraint(RightAfterLeftConstraint.Value);
			root.ConstraintSolver.TryAddConstraint(BottomAfterTopConstraint.Value);
			foreach (var constraint in IntrinsicSizeConstraints)
				root.ConstraintSolver.TryAddConstraint(constraint);

			foreach (var constraint in _heldConstraints)
				root.ConstraintSolver.TryAddConstraint(constraint.CassowaryConstraint.Value);
			foreach (var subview in _subviews)
				subview.Root = root;

			var variables = new[] { LeftVariable, RightVariable, TopVariable, BottomVariable };
			foreach (var variable in variables)
				root.ConstraintSolver.AddVar(variable.Value);
		}

		private void OnRemovedFromRoot(UIRoot root)
		{
			foreach (var constraint in _heldConstraints)
				root.ConstraintSolver.RemoveConstraint(constraint.CassowaryConstraint.Value);
			foreach (var subview in _subviews)
				subview.Root = root;

			root.ConstraintSolver.RemoveConstraint(RightAfterLeftConstraint.Value);
			root.ConstraintSolver.RemoveConstraint(BottomAfterTopConstraint.Value);
			foreach (var constraint in IntrinsicSizeConstraints)
				root.ConstraintSolver.RemoveConstraint(constraint);

			var variables = new[] { LeftVariable, RightVariable, TopVariable, BottomVariable };
			foreach (var variable in variables)
				root.ConstraintSolver.NoteRemovedVariable(variable.Value, variable.Value);
		}

		private void OnIntrinsicSizeChanged((float? X, float? Y) intrinsicSize)
		{
			var newConstraints = new List<ClConstraint>();
			if (intrinsicSize.X is not null)
			{
				newConstraints.Add(new ClLinearInequality(
					WidthAnchor.Expression,
					Cl.Operator.LessThanOrEqualTo,
					new ClLinearExpression(intrinsicSize.X.Value),
					HorizontalContentHuggingStrength)
				);
				newConstraints.Add(new ClLinearInequality(
					WidthAnchor.Expression,
					Cl.Operator.GreaterThanOrEqualTo,
					new ClLinearExpression(intrinsicSize.X.Value),
					HorizontalCompressionResistanceStrength)
				);
			}
			if (intrinsicSize.Y is not null)
			{
				newConstraints.Add(new ClLinearInequality(
					HeightAnchor.Expression,
					Cl.Operator.LessThanOrEqualTo,
					new ClLinearExpression(intrinsicSize.Y.Value),
					VerticalContentHuggingStrength)
				);
				newConstraints.Add(new ClLinearInequality(
					HeightAnchor.Expression,
					Cl.Operator.GreaterThanOrEqualTo,
					new ClLinearExpression(intrinsicSize.Y.Value),
					VerticalCompressionResistanceStrength)
				);
			}
			IntrinsicSizeConstraints = newConstraints;
		}
	}
}