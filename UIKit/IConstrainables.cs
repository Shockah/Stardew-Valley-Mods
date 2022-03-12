using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Shockah.UIKit
{
	public enum UILayoutConstraintMultipleEdgeRelation { Equal, Inside, Outside }

	public static class IConstrainables
	{
		#region Self

		[Pure]
		public static UILayoutConstraint MakeAspectRatioConstraint<ConstrainableType>(
			this ConstrainableType self,
			UIVector2 ratio,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
			=> MakeAspectRatioConstraint(self, ratio.X / ratio.Y, relation, priority);

		[Pure]
		public static UILayoutConstraint MakeAspectRatioConstraint<ConstrainableType>(
			this ConstrainableType self,
			float ratio = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
			=> new(self.WidthAnchor, 0f, ratio, self.HeightAnchor, relation, priority);

		#endregion

		#region Any other constrainable

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeHorizontalEdgeConstraintsTo(
			this IConstrainable.Horizontal self,
			IConstrainable.Horizontal other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var singleEdgeLeftRelation = relation switch
			{
				UILayoutConstraintMultipleEdgeRelation.Equal => UILayoutConstraintRelation.Equal,
				UILayoutConstraintMultipleEdgeRelation.Inside => UILayoutConstraintRelation.GreaterThanOrEqual,
				UILayoutConstraintMultipleEdgeRelation.Outside => UILayoutConstraintRelation.LessThanOrEqual,
				_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
			};
			var singleEdgeRightRelation = singleEdgeLeftRelation.GetReverse();
			yield return self.LeftAnchor.MakeConstraintTo(other, insets, relation: singleEdgeLeftRelation, priority: priority);
			yield return self.RightAnchor.MakeConstraintTo(other, -insets, relation: singleEdgeRightRelation, priority: priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsTo(
			this IConstrainable.Vertical self,
			IConstrainable.Vertical other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var singleEdgeTopRelation = relation switch
			{
				UILayoutConstraintMultipleEdgeRelation.Equal => UILayoutConstraintRelation.Equal,
				UILayoutConstraintMultipleEdgeRelation.Inside => UILayoutConstraintRelation.GreaterThanOrEqual,
				UILayoutConstraintMultipleEdgeRelation.Outside => UILayoutConstraintRelation.LessThanOrEqual,
				_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
			};
			var singleEdgeBottomRelation = singleEdgeTopRelation.GetReverse();
			yield return self.TopAnchor.MakeConstraintTo(other, insets, relation: singleEdgeTopRelation, priority: priority);
			yield return self.BottomAnchor.MakeConstraintTo(other, -insets, relation: singleEdgeBottomRelation, priority: priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			ConstrainableTypeB other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
		{
			foreach (var constraint in self.MakeHorizontalEdgeConstraintsTo(other, insets, relation, priority))
				yield return constraint;
			foreach (var constraint in self.MakeVerticalEdgeConstraintsTo(other, insets, relation, priority))
				yield return constraint;
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeCenterConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			ConstrainableTypeB other,
			UIVector2? offset = null,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
		{
			yield return self.CenterXAnchor.MakeConstraintTo(other, offset?.X ?? 0f, relation: relation, priority: priority);
			yield return self.CenterYAnchor.MakeConstraintTo(other, offset?.Y ?? 0f, relation: relation, priority: priority);
		}

		#endregion

		#region Superview

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeHorizontalEdgeConstraintsToSuperview(
			this IConstrainable.Horizontal self,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.ConstrainableOwnerView} does not have a superview.");
			return self.MakeHorizontalEdgeConstraintsTo(superview, insets, relation, priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsToSuperview(
			this IConstrainable.Vertical self,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.ConstrainableOwnerView} does not have a superview.");
			return self.MakeVerticalEdgeConstraintsTo(superview, insets, relation, priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsToSuperview<ConstrainableType>(
			this ConstrainableType self,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		) where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
		{
			var superview = self.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.ConstrainableOwnerView} does not have a superview.");
			return self.MakeEdgeConstraintsTo(superview, insets, relation, priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeCenterConstraintsToSuperview<ConstrainableType>(
			this ConstrainableType self,
			UIVector2? offset = null,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
		{
			var superview = self.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.ConstrainableOwnerView} does not have a superview.");
			return self.MakeCenterConstraintsTo(superview, offset, relation, priority);
		}

		#endregion
	}
}
