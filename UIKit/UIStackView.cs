using Shockah.CommonModCode.UI;
using System;
using System.Collections.Generic;

namespace Shockah.UIKit
{
	public enum UIStackViewDistribution { Fill, FillEqually, EqualSpacing }
	public enum UIStackViewAlignment { Fill, Center, Leading, Trailing }

	public class UIStackView: UIView
	{
		public Orientation Orientation
		{
			get => _orientation;
			set
			{
				if (_orientation == value)
					return;
				var oldValue = _orientation;
				_orientation = value;
				OrientationChanged?.Invoke(this, oldValue, value);
			}
		}

		public UIStackViewDistribution Distribution
		{
			get => _distribution;
			set
			{
				if (_distribution == value)
					return;
				var oldValue = _distribution;
				_distribution = value;
				DistributionChanged?.Invoke(this, oldValue, value);
			}
		}

		public UIStackViewAlignment Alignment
		{
			get => _alignment;
			set
			{
				if (_alignment == value)
					return;
				var oldValue = _alignment;
				_alignment = value;
				AlignmentChanged?.Invoke(this, oldValue, value);
			}
		}

		public float Spacing
		{
			get => _spacing;
			set
			{
				if (_spacing == value)
					return;
				var oldValue = _spacing;
				_spacing = value;
				SpacingChanged?.Invoke(this, oldValue, value);
			}
		}

		public IReadOnlyList<UIView> ArrangedSubviews => (IReadOnlyList<UIView>)_arrangedSubviews;

		public event OwnerValueChangeEvent<UIStackView, Orientation>? OrientationChanged;
		public event OwnerValueChangeEvent<UIStackView, UIStackViewDistribution>? DistributionChanged;
		public event OwnerValueChangeEvent<UIStackView, UIStackViewAlignment>? AlignmentChanged;
		public event OwnerValueChangeEvent<UIStackView, float>? SpacingChanged;

		private Orientation _orientation;
		private UIStackViewDistribution _distribution = UIStackViewDistribution.Fill;
		private UIStackViewAlignment _alignment = UIStackViewAlignment.Fill;
		private float _spacing = 0f;

		private readonly IList<UIView> _arrangedSubviews = new List<UIView>();
		private readonly IList<UIView> LayoutHelperViews = new List<UIView>();
		private readonly IList<UILayoutConstraint> StackViewConstraints = new List<UILayoutConstraint>();
		private bool IsDirty { get; set; } = false;

		public UIStackView(Orientation orientation)
		{
			this._orientation = orientation;

			RemovedSubview += (_, subview) => OnRemovedSubview(subview);
			OrientationChanged += (_, _, _) => IsDirty = true;
			DistributionChanged += (_, _, _) => IsDirty = true;
			AlignmentChanged += (_, _, _) => IsDirty = true;
			SpacingChanged += (_, _, _) => IsDirty = true;
		}

		public void AddArrangedSubview(UIView subview)
		{
			_arrangedSubviews.Add(subview);
			AddSubview(subview);
			IsDirty = true;
		}

		public void StopArrangingSubview(UIView subview)
		{
			if (subview.Superview != this)
				throw new InvalidOperationException($"View {subview} is not a subview of {this}.");
			_arrangedSubviews.Remove(subview);
			IsDirty = true;
		}

		public override void LayoutIfNeeded()
		{
			if (IsDirty)
				UpdateConstraints();
			base.LayoutIfNeeded();
		}

		private void OnRemovedSubview(UIView subview)
		{
			if (_arrangedSubviews.Contains(subview))
			{
				_arrangedSubviews.Remove(subview);
				IsDirty = true;
			}
			else if (LayoutHelperViews.Contains(subview))
			{
				IsDirty = true;
			}
		}

		private void UpdateConstraints()
		{
			foreach (var layoutHelperView in LayoutHelperViews)
				layoutHelperView.RemoveFromSuperview();
			LayoutHelperViews.Clear();

			StackViewConstraints.Deactivate();
			StackViewConstraints.Clear();

			if (ArrangedSubviews.Count == 0)
				return;

			void UpdateOrientationalDistributionConstraints<ConstrainableType>(
				Func<UIView, UITypedAnchorWithOpposite<ConstrainableType>> leadingAnchor,
				Func<UIView, UITypedAnchorWithOpposite<ConstrainableType>> trailingAnchor,
				Func<UIView, UITypedAnchor<ConstrainableType>> lengthAnchor
			) where ConstrainableType : IConstrainable
			{
				StackViewConstraints.Add(leadingAnchor(ArrangedSubviews[0]).ConstraintTo(leadingAnchor(this)));
				StackViewConstraints.Add(trailingAnchor(ArrangedSubviews[^1]).ConstraintTo(trailingAnchor(this)));
				switch (Distribution)
				{
					case UIStackViewDistribution.Fill:
					case UIStackViewDistribution.FillEqually:
						for (int i = 1; i < ArrangedSubviews.Count; i++)
						{
							StackViewConstraints.Add(leadingAnchor(ArrangedSubviews[i]).ConstraintTo(trailingAnchor(ArrangedSubviews[i - 1]), Spacing));
							if (Distribution == UIStackViewDistribution.FillEqually)
								StackViewConstraints.Add(lengthAnchor(ArrangedSubviews[i]).ConstraintTo(lengthAnchor(ArrangedSubviews[0])));
						}
						break;
					case UIStackViewDistribution.EqualSpacing:
						var layoutHelperView = new UIView();
						StackViewConstraints.Add(lengthAnchor(layoutHelperView).Constraint(Spacing, UILayoutConstraintRelation.GreaterThanOrEqual));
						AddSubview(layoutHelperView);
						LayoutHelperViews.Add(layoutHelperView);

						for (int i = 1; i < ArrangedSubviews.Count; i++)
						{
							StackViewConstraints.Add(leadingAnchor(layoutHelperView).ConstraintTo(trailingAnchor(ArrangedSubviews[i - 1]), Spacing));
							StackViewConstraints.Add(trailingAnchor(layoutHelperView).ConstraintTo(leadingAnchor(ArrangedSubviews[i]), Spacing));
						}
						for (int i = 1; i < LayoutHelperViews.Count; i++)
							StackViewConstraints.Add(lengthAnchor(LayoutHelperViews[i]).ConstraintTo(lengthAnchor(LayoutHelperViews[0])));
						break;
				}
			}

			void UpdateOrientationalAlignmentConstraints<ConstrainableType>(
				Func<UIView, UITypedAnchorWithOpposite<ConstrainableType>> leadingAnchor,
				Func<UIView, UITypedAnchorWithOpposite<ConstrainableType>> trailingAnchor,
				Func<UIView, UITypedAnchor<ConstrainableType>> lengthAnchor,
				Func<UIView, UITypedAnchor<ConstrainableType>> centerAnchor
			) where ConstrainableType : IConstrainable
			{
				foreach (var arrangedSubview in ArrangedSubviews)
				{
					switch (Alignment)
					{
						case UIStackViewAlignment.Fill:
							StackViewConstraints.Add(leadingAnchor(arrangedSubview).ConstraintTo(leadingAnchor(this)));
							StackViewConstraints.Add(trailingAnchor(arrangedSubview).ConstraintTo(trailingAnchor(this)));
							break;
						case UIStackViewAlignment.Center:
							StackViewConstraints.Add(centerAnchor(arrangedSubview).ConstraintTo(centerAnchor(this)));
							StackViewConstraints.Add(lengthAnchor(arrangedSubview).ConstraintTo(trailingAnchor(this), relation: UILayoutConstraintRelation.LessThanOrEqual));
							break;
						case UIStackViewAlignment.Leading:
							StackViewConstraints.Add(leadingAnchor(arrangedSubview).ConstraintTo(leadingAnchor(this)));
							StackViewConstraints.Add(lengthAnchor(arrangedSubview).ConstraintTo(trailingAnchor(this), relation: UILayoutConstraintRelation.LessThanOrEqual));
							break;
						case UIStackViewAlignment.Trailing:
							StackViewConstraints.Add(trailingAnchor(arrangedSubview).ConstraintTo(trailingAnchor(this)));
							StackViewConstraints.Add(lengthAnchor(arrangedSubview).ConstraintTo(trailingAnchor(this), relation: UILayoutConstraintRelation.LessThanOrEqual));
							break;
					}
				}
			}

			switch (Orientation)
			{
				case Orientation.Horizontal:
					UpdateOrientationalDistributionConstraints(
						leadingAnchor: v => v.LeftAnchor,
						trailingAnchor: v => v.RightAnchor,
						lengthAnchor: v => v.WidthAnchor
					);
					UpdateOrientationalAlignmentConstraints(
						leadingAnchor: v => v.TopAnchor,
						trailingAnchor: v => v.BottomAnchor,
						lengthAnchor: v => v.HeightAnchor,
						centerAnchor: v => v.CenterYAnchor
					);
					break;
				case Orientation.Vertical:
					UpdateOrientationalDistributionConstraints(
						leadingAnchor: v => v.TopAnchor,
						trailingAnchor: v => v.BottomAnchor,
						lengthAnchor: v => v.HeightAnchor
					);
					UpdateOrientationalAlignmentConstraints(
						leadingAnchor: v => v.LeftAnchor,
						trailingAnchor: v => v.RightAnchor,
						lengthAnchor: v => v.WidthAnchor,
						centerAnchor: v => v.CenterXAnchor
					);
					break;
			}

			StackViewConstraints.Activate();
			IsDirty = false;
		}
	}
}
