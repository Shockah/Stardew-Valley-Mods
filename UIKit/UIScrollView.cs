using Cassowary;
using Shockah.UIKit.Geometry;
using Shockah.UIKit.Gesture;
using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace Shockah.UIKit
{
	public class UIScrollViewContentFrame: IConstrainable.Horizontal, IConstrainable.Vertical
	{
		public UIView ConstrainableOwnerView => Owner;

		public IUITypedAnchorWithOpposite<IConstrainable.Horizontal> LeftAnchor => LazyLeft.Value;
		public IUITypedAnchorWithOpposite<IConstrainable.Horizontal> RightAnchor => LazyRight.Value;
		public IUITypedAnchorWithOpposite<IConstrainable.Vertical> TopAnchor => LazyTop.Value;
		public IUITypedAnchorWithOpposite<IConstrainable.Vertical> BottomAnchor => LazyBottom.Value;
		public IUITypedAnchor<IConstrainable.Horizontal> WidthAnchor => LazyWidth.Value;
		public IUITypedAnchor<IConstrainable.Vertical> HeightAnchor => LazyHeight.Value;
		public IUITypedAnchor<IConstrainable.Horizontal> CenterXAnchor => LazyCenterX.Value;
		public IUITypedAnchor<IConstrainable.Vertical> CenterYAnchor => LazyCenterY.Value;

		internal readonly Lazy<ClVariable> LeftVariable;
		internal readonly Lazy<ClVariable> RightVariable;
		internal readonly Lazy<ClVariable> TopVariable;
		internal readonly Lazy<ClVariable> BottomVariable;

		internal readonly Lazy<UITypedAnchorWithOpposite<IConstrainable.Horizontal>> LazyLeft;
		internal readonly Lazy<UITypedAnchorWithOpposite<IConstrainable.Horizontal>> LazyRight;
		internal readonly Lazy<UITypedAnchorWithOpposite<IConstrainable.Vertical>> LazyTop;
		internal readonly Lazy<UITypedAnchorWithOpposite<IConstrainable.Vertical>> LazyBottom;
		internal readonly Lazy<UITypedAnchor<IConstrainable.Horizontal>> LazyWidth;
		internal readonly Lazy<UITypedAnchor<IConstrainable.Vertical>> LazyHeight;
		internal readonly Lazy<UITypedAnchor<IConstrainable.Horizontal>> LazyCenterX;
		internal readonly Lazy<UITypedAnchor<IConstrainable.Vertical>> LazyCenterY;

		internal readonly Lazy<UILayoutConstraint> RightAfterLeftConstraint;
		internal readonly Lazy<UILayoutConstraint> BottomAfterTopConstraint;

		private readonly UIScrollView Owner;

		internal UIScrollViewContentFrame(UIScrollView owner)
		{
			this.Owner = owner;

			LeftVariable = new(() => new($"{owner}.ContentFrame.Left"));
			RightVariable = new(() => new($"{owner}.ContentFrame.Right"));
			TopVariable = new(() => new($"{owner}.ContentFrame.Top"));
			BottomVariable = new(() => new($"{owner}.ContentFrame.Bottom"));

			LazyLeft = new(() => new(this, new(LeftVariable.Value), "ContentFrame.Left", c => c.LeftAnchor, c => c.RightAnchor));
			LazyRight = new(() => new(this, new(RightVariable.Value), "ContentFrame.Right", c => c.RightAnchor, c => c.LeftAnchor));
			LazyTop = new(() => new(this, new(TopVariable.Value), "ContentFrame.Top", c => c.TopAnchor, c => c.BottomAnchor));
			LazyBottom = new(() => new(this, new(BottomVariable.Value), "ContentFrame.Bottom", c => c.BottomAnchor, c => c.TopAnchor));
			LazyWidth = new(() => new(this, new ClLinearExpression(RightVariable.Value).Minus(LeftVariable.Value), "ContentFrame.Width", c => c.WidthAnchor));
			LazyHeight = new(() => new(this, new ClLinearExpression(BottomVariable.Value).Minus(TopVariable.Value), "ContentFrame.Height", c => c.HeightAnchor));
			LazyCenterX = new(() => new(this, new ClLinearExpression(LeftVariable.Value).Plus(((IUIAnchor.Internal)WidthAnchor).Expression.Times(0.5)), "ContentFrame.CenterX", c => c.CenterXAnchor));
			LazyCenterY = new(() => new(this, new ClLinearExpression(TopVariable.Value).Plus(((IUIAnchor.Internal)HeightAnchor).Expression.Times(0.5)), "ContentFrame.CenterY", c => c.CenterYAnchor));

			RightAfterLeftConstraint = new(() => RightAnchor.MakeConstraintTo(LeftAnchor, relation: UILayoutConstraintRelation.GreaterThanOrEqual));
			BottomAfterTopConstraint = new(() => BottomAnchor.MakeConstraintTo(TopAnchor, relation: UILayoutConstraintRelation.GreaterThanOrEqual));
		}
	}

	public class UIScrollView: UISurfaceView
	{
		public UIScrollViewContentFrame ContentFrame { get; private set; }

		public UIVector2 ContentSize { get; private set; } = UIVector2.Zero;

		public UIVector2 ContentOffset
		{
			get => _contentOffset;
			set
			{
				var maxContentOffset = MaxContentOffset;
				if (ClampsVerticalContentOffset)
					value = new(value.X, Math.Clamp(value.Y, 0f, maxContentOffset.Y));
				if (ClampsHorizontalContentOffset)
					value = new(Math.Clamp(value.X, 0f, maxContentOffset.X), value.Y);
				if (_contentOffset == value)
					return;
				var oldValue = _contentOffset;
				_contentOffset = value;
				ContentOffsetChanged?.Invoke(this, oldValue, value);
			}
		}

		public UIVector2 MaxContentOffset
			=> new(Math.Max(ContentSize.X - Width, 0), Math.Max(ContentSize.Y - Height, 0));

		public event OwnerValueChangeEvent<UIScrollView, UIVector2>? ContentSizeChanged;
		public event OwnerValueChangeEvent<UIScrollView, UIVector2>? ContentOffsetChanged;

		public UIVector2 ScrollFactor { get; set; } = UIVector2.One;

		public bool ClampsVerticalContentOffset { get; set; } = true;
		public bool ClampsHorizontalContentOffset { get; set; } = true;
		public bool AllowsVerticalScrolling { get; set; } = true;
		public bool AllowsHorizontalScrolling { get; set; } = true;
		public bool ReverseVerticalDirection { get; set; } = false;
		public bool ReverseHorizontalDirection { get; set; } = false;

		private UIVector2 _contentOffset = UIVector2.Zero;

		private IReadOnlyList<UILayoutConstraint> _contentFrameConstraints = Array.Empty<UILayoutConstraint>();
		private IReadOnlyList<UILayoutConstraint> ContentFrameConstraints
		{
			get => _contentFrameConstraints;
			set
			{
				if (Root is not null)
				{
					foreach (var constraint in _contentFrameConstraints)
						Root.QueueRemoveConstraint(constraint);
					foreach (var constraint in value)
						Root.QueueAddConstraint(constraint);
				}
				_contentFrameConstraints = value;
			}
		}

		public UIScrollView()
		{
			ContentFrame = new(this);
			UpdateContentFrameConstraints();

			AddedToRoot += (root, _) => OnAddedToRoot(root);
			RemovedFromRoot += (root, _) => OnRemovedFromRoot(root);
			ContentOffsetChanged += (_, _, _) => UpdateContentFrameConstraints();
		}

		public override bool OnSelfHover(UITouch touch)
		{
			var isHandled = base.OnSelfHover(touch);
			if (touch is UITouch<int, ISet<SButton>> typedTouch)
			{
				var toScroll = typedTouch.Last.Scroll * ScrollFactor;
				if (ReverseVerticalDirection)
					toScroll *= new UIVector2(1, -1);
				else if (!AllowsVerticalScrolling)
					toScroll *= UIVector2.UnitX;
				if (ReverseHorizontalDirection)
					toScroll *= new UIVector2(-1, 1);
				else if (!AllowsHorizontalScrolling)
					toScroll *= UIVector2.UnitY;

				var oldContentOffset = ContentOffset;
				ContentOffset += toScroll;
				if (oldContentOffset != ContentOffset)
					isHandled = true;
			}
			return isHandled;
		}

		protected override void OnLayoutIfNeeded()
		{
			base.OnLayoutIfNeeded();
			if (Root is null)
				return;

			var oldContentSize = ContentSize;
			var contentX1 = (float)(ContentFrame.LeftVariable.Value.Value - LeftVariable.Value.Value);
			var contentY1 = (float)(ContentFrame.TopVariable.Value.Value - TopVariable.Value.Value);
			var contentX2 = (float)(ContentFrame.RightVariable.Value.Value - LeftVariable.Value.Value);
			var contentY2 = (float)(ContentFrame.BottomVariable.Value.Value - TopVariable.Value.Value);
			UIVector2 contentSize = new(contentX2 - contentX1, contentY2 - contentY1);

			if (contentSize != oldContentSize)
			{
				ContentSize = contentSize;
				ContentSizeChanged?.Invoke(this, oldContentSize, contentSize);
			}
		}

		private void UpdateContentFrameConstraints()
		{
			ContentFrameConstraints = new[]
			{
				ReverseVerticalDirection
					? ContentFrame.BottomAnchor.MakeConstraintTo(BottomAnchor, ContentOffset.Y)
					: ContentFrame.TopAnchor.MakeConstraintTo(TopAnchor, -ContentOffset.Y),
				ReverseHorizontalDirection
					? ContentFrame.RightAnchor.MakeConstraintTo(RightAnchor, ContentOffset.X)
					: ContentFrame.LeftAnchor.MakeConstraintTo(LeftAnchor, -ContentOffset.X)
			};
		}

		private void OnAddedToRoot(UIRootView root)
		{
			UpdateContentFrameConstraints();

			root.AddVariables(ContentFrame.LeftVariable.Value, ContentFrame.RightVariable.Value, ContentFrame.TopVariable.Value, ContentFrame.BottomVariable.Value);
			root.QueueAddConstraint(ContentFrame.RightAfterLeftConstraint.Value);
			root.QueueAddConstraint(ContentFrame.BottomAfterTopConstraint.Value);

			foreach (var constraint in ContentFrameConstraints)
				root.QueueAddConstraint(constraint);
		}

		private void OnRemovedFromRoot(UIRootView root)
		{
			foreach (var constraint in ContentFrameConstraints)
				root.QueueRemoveConstraint(constraint);

			root.QueueRemoveConstraint(ContentFrame.RightAfterLeftConstraint.Value);
			root.QueueRemoveConstraint(ContentFrame.BottomAfterTopConstraint.Value);
			root.RemoveVariables(ContentFrame.LeftVariable.Value, ContentFrame.RightVariable.Value, ContentFrame.TopVariable.Value, ContentFrame.BottomVariable.Value);
		}
	}
}