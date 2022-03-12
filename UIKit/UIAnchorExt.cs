using System;

namespace Shockah.UIKit
{
	public static class UIAnchorExt
	{
		public static UILayoutConstraint MakeConstraint(
			this IUIAnchor self,
			float constant,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			=> new(self, constant, relation: relation, priority: priority);

		public static UILayoutConstraint MakeConstraintTo(
			this IUIAnchor self,
			IUIAnchor other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			=> new(self, constant, multiplier, other, relation, priority);

		public static UILayoutConstraint MakeConstraintTo<ConstrainableType>(
			this IUITypedAnchor<ConstrainableType> self,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		) where ConstrainableType : IConstrainable
			=> new(self, constant, multiplier, self.GetSameAnchorInConstrainable(other), relation, priority);

		public static UILayoutConstraint MakeConstraintToOpposite<ConstrainableType>(
			this IUITypedAnchorWithOpposite<ConstrainableType> self,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		) where ConstrainableType : IConstrainable
			=> new(self, constant, multiplier, self.GetOppositeAnchorInConstrainable(other), relation, priority);

		public static UILayoutConstraint MakeConstraintToSuperview(
			this IUITypedAnchor<UIView> self,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.Owner.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.Owner.ConstrainableOwnerView} does not have a superview.");
			return self.MakeConstraintTo(self.GetSameAnchorInConstrainable(superview), constant, multiplier, relation, priority);
		}

		public static UILayoutConstraint MakeConstraintToSuperviewOpposite(
			this IUITypedAnchorWithOpposite<UIView> self,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.Owner.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.Owner.ConstrainableOwnerView} does not have a superview.");
			return self.MakeConstraintTo(self.GetOppositeAnchorInConstrainable(superview), constant, multiplier, relation, priority);
		}
	}
}