using Shockah.CommonModCode.UI;
using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit
{
	public enum UIStackViewDistribution { Fill, FillEqually, EqualSpacing }
	public enum UIStackViewAlignment { Fill, Center, Leading, Trailing }

	public class UIStackView: UIView
	{
		public delegate float? CustomSpacingDelegate(UIView precedingView, UIView succeedingView);

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

		public CustomSpacingDelegate? CustomSpacing
		{
			get => _customSpacing;
			set
			{
				if (_customSpacing == value)
					return;
				var oldValue = _customSpacing;
				_customSpacing = value;
				CustomSpacingChanged?.Invoke(this, oldValue, value);
			}
		}

		public bool IgnoreInvisibleViews
		{
			get => _ignoreInvisibleViews;
			set
			{
				if (_ignoreInvisibleViews == value)
					return;
				var oldValue = _ignoreInvisibleViews;
				_ignoreInvisibleViews = value;
				IgnoreInvisibleViewsChanged?.Invoke(this, oldValue, value);
			}
		}

		public UIEdgeInsets ContentInsets
		{
			get => _contentInsets;
			set
			{
				if (_contentInsets == value)
					return;
				var oldValue = _contentInsets;
				_contentInsets = value;
				ContentInsetsChanged?.Invoke(this, oldValue, value);
			}
		}

		public IReadOnlyList<UIView> ArrangedSubviews => (IReadOnlyList<UIView>)_arrangedSubviews;

		public event OwnerValueChangeEvent<UIStackView, Orientation>? OrientationChanged;
		public event OwnerValueChangeEvent<UIStackView, UIStackViewDistribution>? DistributionChanged;
		public event OwnerValueChangeEvent<UIStackView, UIStackViewAlignment>? AlignmentChanged;
		public event OwnerValueChangeEvent<UIStackView, float>? SpacingChanged;
		public event OwnerValueChangeEvent<UIStackView, CustomSpacingDelegate?>? CustomSpacingChanged;
		public event OwnerValueChangeEvent<UIStackView, bool>? IgnoreInvisibleViewsChanged;
		public event OwnerValueChangeEvent<UIStackView, UIEdgeInsets>? ContentInsetsChanged;

		private Orientation _orientation;
		private UIStackViewDistribution _distribution = UIStackViewDistribution.Fill;
		private UIStackViewAlignment _alignment = UIStackViewAlignment.Fill;
		private float _spacing = 0f;
		private CustomSpacingDelegate? _customSpacing = null;
		private bool _ignoreInvisibleViews = true;
		private UIEdgeInsets _contentInsets = new();

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
			CustomSpacingChanged += (_, _, _) => IsDirty = true;
			ContentInsetsChanged += (_, _, _) => IsDirty = true;

			IgnoreInvisibleViewsChanged += (_, _, newValue) =>
			{
				if (newValue)
				{
					foreach (var subview in ArrangedSubviews)
						subview.IsVisibleChanged += OnArrangedSubviewIsVisibleChanged;
				}
				else
				{
					foreach (var subview in ArrangedSubviews)
						subview.IsVisibleChanged -= OnArrangedSubviewIsVisibleChanged;
				}
				IsDirty = true;
			};
		}

		public void AddArrangedSubview(UIView subview)
		{
			AddSubview(subview);
			StartArrangingSubview(subview);
		}

		public void StartArrangingSubview(UIView subview)
		{
			if (subview.Superview != this)
				throw new InvalidOperationException($"View {subview} is not a subview of {this}.");
			_arrangedSubviews.Add(subview);
			if (IgnoreInvisibleViews)
				subview.IsVisibleChanged += OnArrangedSubviewIsVisibleChanged;
			IsDirty = true;
		}

		public void StopArrangingSubview(UIView subview)
		{
			if (subview.Superview != this)
				throw new InvalidOperationException($"View {subview} is not a subview of {this}.");
			_arrangedSubviews.Remove(subview);
			if (IgnoreInvisibleViews)
				subview.IsVisibleChanged -= OnArrangedSubviewIsVisibleChanged;
			IsDirty = true;
		}

		private void OnRemovedSubview(UIView subview)
		{
			if (_arrangedSubviews.Contains(subview))
			{
				_arrangedSubviews.Remove(subview);
				if (IgnoreInvisibleViews)
					subview.IsVisibleChanged -= OnArrangedSubviewIsVisibleChanged;
				IsDirty = true;
			}
			else if (LayoutHelperViews.Contains(subview))
			{
				IsDirty = true;
			}
		}

		protected override void OnUpdateConstraints()
		{
			if (!IsDirty)
				return;

			foreach (var layoutHelperView in LayoutHelperViews)
				layoutHelperView.RemoveFromSuperview();
			LayoutHelperViews.Clear();

			StackViewConstraints.Deactivate();
			StackViewConstraints.Clear();

			var consideredSubviews = IgnoreInvisibleViews ? ArrangedSubviews.Where(v => v.IsVisible).ToList() : ArrangedSubviews;
			if (consideredSubviews.Count == 0)
				return;

			float GetSpacing(UIView precedingView, UIView succeedingView)
				=> CustomSpacing?.Invoke(precedingView, succeedingView) ?? Spacing;

			void UpdateOrientationalDistributionConstraints<ConstrainableType>(
				Func<UIView, IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite> leadingAnchor,
				Func<UIView, IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite> trailingAnchor,
				Func<UIView, IUIAnchor.Typed<ConstrainableType>.Length> lengthAnchor,
				float leadingInset,
				float trailingInset
			) where ConstrainableType : IConstrainable
			{
				StackViewConstraints.Add(leadingAnchor(consideredSubviews[0]).MakeConstraintTo(
					"UISV-canvasConnection",
					leadingAnchor(this),
					leadingInset
				));
				StackViewConstraints.Add(trailingAnchor(consideredSubviews[^1]).MakeConstraintTo(
					"UISV-canvasConnection",
					trailingAnchor(this),
					-trailingInset
				));
				switch (Distribution)
				{
					case UIStackViewDistribution.Fill:
					case UIStackViewDistribution.FillEqually:
						for (int i = 1; i < consideredSubviews.Count; i++)
						{
							StackViewConstraints.Add(leadingAnchor(consideredSubviews[i]).MakeConstraintTo(
								"UISV-distribution-fill",
								trailingAnchor(consideredSubviews[i - 1]),
								GetSpacing(consideredSubviews[i - 1], consideredSubviews[i])
							));
							if (Distribution == UIStackViewDistribution.FillEqually)
								StackViewConstraints.Add(lengthAnchor(consideredSubviews[i]).MakeConstraintTo(
									"UISV-distribution-fill-equally",
									lengthAnchor(consideredSubviews[0])
								));
						}
						break;
					case UIStackViewDistribution.EqualSpacing:
						for (int i = 1; i < consideredSubviews.Count; i++)
						{
							var layoutHelperView = new UIView();
							AddSubview(layoutHelperView);
							LayoutHelperViews.Add(layoutHelperView);

							StackViewConstraints.Add(leadingAnchor(layoutHelperView).MakeConstraintTo(
								"UISV-distribution-equalSpacing",
								trailingAnchor(consideredSubviews[i - 1])
							));
							StackViewConstraints.Add(trailingAnchor(layoutHelperView).MakeConstraintTo(
								"UISV-distribution-equalSpacing",
								leadingAnchor(consideredSubviews[i])
							));
						}
						if (LayoutHelperViews.Count != 0)
						{
							StackViewConstraints.Add(lengthAnchor(LayoutHelperViews[0]).MakeConstraint(
								"UISV-distribution-equalSpacing",
								Spacing,
								UILayoutConstraintRelation.GreaterThanOrEqual
							));
							StackViewConstraints.Add(lengthAnchor(LayoutHelperViews[0]).MakeConstraint(
								"UISV-distribution-equalSpacing",
								Spacing,
								priority: UILayoutConstraintPriority.Low
							));
						}
						for (int i = 1; i < LayoutHelperViews.Count; i++)
							StackViewConstraints.Add(lengthAnchor(LayoutHelperViews[i]).MakeConstraintTo(
								"UISV-distribution-equalSpacing",
								lengthAnchor(LayoutHelperViews[0])
							));
						break;
					default:
						throw new InvalidOperationException($"{nameof(UIStackViewDistribution)} has an invalid value.");
				}
			}

			void UpdateOrientationalAlignmentConstraints<ConstrainableType>(
				Func<UIView, IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite> leadingAnchor,
				Func<UIView, IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite> trailingAnchor,
				Func<UIView, IUIAnchor.Typed<ConstrainableType>.Length> lengthAnchor,
				Func<UIView, IUIAnchor.Typed<ConstrainableType>.Positional> centerAnchor,
				float leadingInset,
				float trailingInset
			) where ConstrainableType : IConstrainable
			{
				switch (Alignment)
				{
					case UIStackViewAlignment.Fill:
						foreach (var arrangedSubview in consideredSubviews)
						{
							StackViewConstraints.Add(leadingAnchor(arrangedSubview).MakeConstraintTo(
								"UISV-alignment-fill",
								leadingAnchor(this),
								leadingInset
							));
							StackViewConstraints.Add(trailingAnchor(arrangedSubview).MakeConstraintTo(
								"UISV-alignment-fill",
								trailingAnchor(this),
								-trailingInset
							));
						}
						break;
					case UIStackViewAlignment.Center:
						foreach (var arrangedSubview in consideredSubviews)
						{
							StackViewConstraints.Add(centerAnchor(arrangedSubview).MakeConstraintTo(
								"UISV-alignment-center",
								centerAnchor(this),
								(leadingInset - trailingInset) / 2
							));
							StackViewConstraints.Add(lengthAnchor(arrangedSubview).MakeConstraint(
								"UISV-alignment-center",
								0f,
								priority: new(25f)
							));
							StackViewConstraints.Add(lengthAnchor(arrangedSubview).MakeConstraintTo(
								"UISV-alignment-center",
								lengthAnchor(this),
								-(leadingInset + trailingInset),
								relation: UILayoutConstraintRelation.LessThanOrEqual,
								priority: UILayoutConstraintPriority.High
							));
						}
						StackViewConstraints.Add(lengthAnchor(this).MakeConstraint(
							"UISV-alignment-center",
							0f,
							priority: new(24f)
						));
						break;
					case UIStackViewAlignment.Leading:
						foreach (var arrangedSubview in consideredSubviews)
						{
							StackViewConstraints.Add(leadingAnchor(arrangedSubview).MakeConstraintTo(
								"UISV-alignment-leading",
								leadingAnchor(this),
								leadingInset
							));
							StackViewConstraints.Add(lengthAnchor(arrangedSubview).MakeConstraint(
								"UISV-alignment-leading",
								0f,
								priority: new(25f)
							));
							StackViewConstraints.Add(lengthAnchor(arrangedSubview).MakeConstraintTo(
								"UISV-alignment-leading",
								lengthAnchor(this),
								-(leadingInset + trailingInset),
								relation: UILayoutConstraintRelation.LessThanOrEqual,
								priority: UILayoutConstraintPriority.High
							));
						}
						StackViewConstraints.Add(lengthAnchor(this).MakeConstraint(
							"UISV-alignment-leading",
							0f,
							priority: new(24f)
						));
						break;
					case UIStackViewAlignment.Trailing:
						foreach (var arrangedSubview in consideredSubviews)
						{
							StackViewConstraints.Add(trailingAnchor(arrangedSubview).MakeConstraintTo(
								"UISV-alignment-trailing",
								trailingAnchor(this),
								-trailingInset
							));
							StackViewConstraints.Add(lengthAnchor(arrangedSubview).MakeConstraint(
								"UISV-alignment-trailing",
								0f,
								priority: new(25f)
							));
							StackViewConstraints.Add(lengthAnchor(arrangedSubview).MakeConstraintTo(
								"UISV-alignment-trailing",
								lengthAnchor(this),
								-(leadingInset + trailingInset),
								relation: UILayoutConstraintRelation.LessThanOrEqual,
								priority: UILayoutConstraintPriority.High
							));
						}
						StackViewConstraints.Add(lengthAnchor(this).MakeConstraint(
							"UISV-alignment-trailing",
							0f,
							priority: new(24f)
						));
						break;
					default:
						throw new InvalidOperationException($"{nameof(UIStackViewAlignment)} has an invalid value.");
				}
			}

			switch (Orientation)
			{
				case Orientation.Horizontal:
					UpdateOrientationalDistributionConstraints(
						leadingAnchor: v => v.LeftAnchor,
						trailingAnchor: v => v.RightAnchor,
						lengthAnchor: v => v.WidthAnchor,
						leadingInset: ContentInsets.Left,
						trailingInset: ContentInsets.Right
					);
					UpdateOrientationalAlignmentConstraints(
						leadingAnchor: v => v.TopAnchor,
						trailingAnchor: v => v.BottomAnchor,
						lengthAnchor: v => v.HeightAnchor,
						centerAnchor: v => v.CenterYAnchor,
						leadingInset: ContentInsets.Top,
						trailingInset: ContentInsets.Bottom
					);
					break;
				case Orientation.Vertical:
					UpdateOrientationalDistributionConstraints(
						leadingAnchor: v => v.TopAnchor,
						trailingAnchor: v => v.BottomAnchor,
						lengthAnchor: v => v.HeightAnchor,
						leadingInset: ContentInsets.Top,
						trailingInset: ContentInsets.Bottom
					);
					UpdateOrientationalAlignmentConstraints(
						leadingAnchor: v => v.LeftAnchor,
						trailingAnchor: v => v.RightAnchor,
						lengthAnchor: v => v.WidthAnchor,
						centerAnchor: v => v.CenterXAnchor,
						leadingInset: ContentInsets.Left,
						trailingInset: ContentInsets.Right
					);
					break;
			}

			StackViewConstraints.Activate();
			IsDirty = false;
		}

		private void OnArrangedSubviewIsVisibleChanged(UIView subview, bool oldValue, bool newValue)
		{
			IsDirty = true;
		}
	}
}
