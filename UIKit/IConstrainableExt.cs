using Cassowary;
using System;
using System.Collections.Generic;

namespace Shockah.UIKit
{
	public enum UILayoutConstraintMultipleEdgeRelation { Equal, Inside, Outside }

	public static class IConstrainableExt
	{
		public static IEnumerable<UILayoutConstraint> HorizontalEdgeConstraintsTo(
			this IConstrainable.Horizontal self,
			IConstrainable.Horizontal other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		) => self.HorizontalEdgeConstraintsTo(other, ClStrength.Required, insets, relation);

		public static IEnumerable<UILayoutConstraint> HorizontalEdgeConstraintsTo(
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
			yield return self.LeftAnchor.ConstraintTo(other, strength, insets, relation: singleEdgeLeftRelation);
			yield return self.RightAnchor.ConstraintTo(other, strength, -insets, relation: singleEdgeRightRelation);
		}

		public static IEnumerable<UILayoutConstraint> VerticalEdgeConstraintsTo(
			this IConstrainable.Vertical self,
			IConstrainable.Vertical other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		) => self.VerticalEdgeConstraintsTo(other, ClStrength.Required, insets, relation);

		public static IEnumerable<UILayoutConstraint> VerticalEdgeConstraintsTo(
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
			yield return self.TopAnchor.ConstraintTo(other, strength, insets, relation: singleEdgeTopRelation);
			yield return self.BottomAnchor.ConstraintTo(other, strength, -insets, relation: singleEdgeBottomRelation);
		}

		public static IEnumerable<UILayoutConstraint> EdgeConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			ConstrainableTypeB other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
			=> self.EdgeConstraintsTo(other, ClStrength.Required, insets, relation);

		public static IEnumerable<UILayoutConstraint> EdgeConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			ConstrainableTypeB other,
			ClStrength strength,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
		{
			foreach (var constraint in self.HorizontalEdgeConstraintsTo(other, strength, insets, relation))
				yield return constraint;
			foreach (var constraint in self.VerticalEdgeConstraintsTo(other, strength, insets, relation))
				yield return constraint;
		}
	}
}
