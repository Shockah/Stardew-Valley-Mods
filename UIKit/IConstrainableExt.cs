using Cassowary;
using System;
using System.Collections.Generic;

namespace Shockah.UIKit
{
	public enum UILayoutConstraintMultipleEdgeRelation { Equal, Inside, Outside }

	public static class IConstrainableExt
	{
		public static IEnumerable<UILayoutConstraint> MakeHorizontalEdgeConstraintsTo(
			this IConstrainable.Horizontal self,
			IConstrainable.Horizontal other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		) => self.MakeHorizontalEdgeConstraintsTo(other, ClStrength.Required, insets, relation);

		public static IEnumerable<UILayoutConstraint> MakeHorizontalEdgeConstraintsTo(
			this IConstrainable.Horizontal self,
			IConstrainable.Horizontal other,
			ClStrength strength,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
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
			yield return self.LeftAnchor.MakeConstraintTo(other, strength, insets, relation: singleEdgeLeftRelation);
			yield return self.RightAnchor.MakeConstraintTo(other, strength, -insets, relation: singleEdgeRightRelation);
		}

		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsTo(
			this IConstrainable.Vertical self,
			IConstrainable.Vertical other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		) => self.MakeVerticalEdgeConstraintsTo(other, ClStrength.Required, insets, relation);

		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsTo(
			this IConstrainable.Vertical self,
			IConstrainable.Vertical other,
			ClStrength strength,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
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
			yield return self.TopAnchor.MakeConstraintTo(other, strength, insets, relation: singleEdgeTopRelation);
			yield return self.BottomAnchor.MakeConstraintTo(other, strength, -insets, relation: singleEdgeBottomRelation);
		}

		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			ConstrainableTypeB other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
			=> self.MakeEdgeConstraintsTo(other, ClStrength.Required, insets, relation);

		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			ConstrainableTypeB other,
			ClStrength strength,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
		{
			foreach (var constraint in self.MakeHorizontalEdgeConstraintsTo(other, strength, insets, relation))
				yield return constraint;
			foreach (var constraint in self.MakeVerticalEdgeConstraintsTo(other, strength, insets, relation))
				yield return constraint;
		}

		public static IEnumerable<UILayoutConstraint> MakeHorizontalEdgeConstraintsToSuperview(
			this IConstrainable.Horizontal self,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		) => self.HorizontalEdgeConstraintsToSuperview(ClStrength.Required, insets, relation);

		public static IEnumerable<UILayoutConstraint> HorizontalEdgeConstraintsToSuperview(
			this IConstrainable.Horizontal self,
			ClStrength strength,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		)
		{
			var superview = self.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.ConstrainableOwnerView} does not have a superview.");
			return self.MakeHorizontalEdgeConstraintsTo(superview, strength, insets, relation);
		}

		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsToSuperview(
			this IConstrainable.Vertical self,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		) => self.MakeVerticalEdgeConstraintsToSuperview(ClStrength.Required, insets, relation);

		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsToSuperview(
			this IConstrainable.Vertical self,
			ClStrength strength,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		)
		{
			var superview = self.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.ConstrainableOwnerView} does not have a superview.");
			return self.MakeVerticalEdgeConstraintsTo(superview, strength, insets, relation);
		}

		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsToSuperview<ConstrainableType>(
			this ConstrainableType self,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		) where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
			=> self.MakeEdgeConstraintsToSuperview(ClStrength.Required, insets, relation);

		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsToSuperview<ConstrainableType>(
			this ConstrainableType self,
			ClStrength strength,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		) where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
		{
			var superview = self.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.ConstrainableOwnerView} does not have a superview.");
			return self.MakeEdgeConstraintsTo(superview, strength, insets, relation);
		}
	}
}
