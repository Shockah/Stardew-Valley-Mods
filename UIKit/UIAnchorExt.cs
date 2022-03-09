using Cassowary;

namespace Shockah.UIKit
{
	public static class UIAnchorExt
	{
		public static UILayoutConstraint Constraint(
			this UIAnchor self,
			ClStrength strength,
			float constant = 0f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
			=> new(strength, self, constant, relation: relation);

		public static UILayoutConstraint Constraint(
			this UIAnchor self,
			float constant = 0f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
			=> new(self, constant, relation: relation);

		public static UILayoutConstraint ConstraintTo(
			this UIAnchor self,
			UIAnchor other,
			ClStrength strength,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
			=> new(strength, self, constant, multiplier, other, relation);

		public static UILayoutConstraint ConstraintTo(
			this UIAnchor self,
			UIAnchor other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
			=> new(self, constant, multiplier, other, relation);

		public static UILayoutConstraint ConstraintTo<ConstrainableType>(
			this UITypedAnchor<ConstrainableType> self,
			ConstrainableType other,
			ClStrength strength,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		) where ConstrainableType : IConstrainable
			=> new(strength, self, constant, multiplier, self.AnchorFunction(other), relation);

		public static UILayoutConstraint ConstraintTo<ConstrainableType>(
			this UITypedAnchor<ConstrainableType> self,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		) where ConstrainableType : IConstrainable
			=> new(self, constant, multiplier, self.AnchorFunction(other), relation);

		public static UILayoutConstraint ConstraintToOpposite<ConstrainableType>(
			this UITypedAnchorWithOpposite<ConstrainableType> self,
			ConstrainableType other,
			ClStrength strength,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		) where ConstrainableType : IConstrainable
			=> new(strength, self, constant, multiplier, self.GetOpposite(other), relation);

		public static UILayoutConstraint ConstraintToOpposite<ConstrainableType>(
			this UITypedAnchorWithOpposite<ConstrainableType> self,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		) where ConstrainableType : IConstrainable
			=> new(self, constant, multiplier, self.GetOpposite(other), relation);
	}
}