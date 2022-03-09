using Cassowary;
using System;

namespace Shockah.UIKit
{
	public static class UIAnchorExt
	{
		public static UILayoutConstraint MakeConstraint(
			this IUIAnchor self,
			ClStrength strength,
			float constant = 0f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
			=> new(strength, self, constant, relation: relation);

		public static UILayoutConstraint MakeConstraint(
			this IUIAnchor self,
			float constant = 0f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
			=> new(self, constant, relation: relation);

		public static UILayoutConstraint MakeConstraintTo(
			this IUIAnchor self,
			IUIAnchor other,
			ClStrength strength,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
			=> new(strength, self, constant, multiplier, other, relation);

		public static UILayoutConstraint MakeConstraintTo(
			this IUIAnchor self,
			IUIAnchor other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
			=> new(self, constant, multiplier, other, relation);

		public static UILayoutConstraint MakeConstraintTo<ConstrainableType>(
			this IUITypedAnchor<ConstrainableType> self,
			ConstrainableType other,
			ClStrength strength,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		) where ConstrainableType : IConstrainable
			=> new(strength, self, constant, multiplier, self.GetSameAnchorInConstrainable(other), relation);

		public static UILayoutConstraint MakeConstraintTo<ConstrainableType>(
			this IUITypedAnchor<ConstrainableType> self,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		) where ConstrainableType : IConstrainable
			=> new(self, constant, multiplier, self.GetSameAnchorInConstrainable(other), relation);

		public static UILayoutConstraint MakeConstraintToOpposite<ConstrainableType>(
			this IUITypedAnchorWithOpposite<ConstrainableType> self,
			ConstrainableType other,
			ClStrength strength,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		) where ConstrainableType : IConstrainable
			=> new(strength, self, constant, multiplier, self.GetOppositeAnchorInConstrainable(other), relation);

		public static UILayoutConstraint MakeConstraintToOpposite<ConstrainableType>(
			this IUITypedAnchorWithOpposite<ConstrainableType> self,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		) where ConstrainableType : IConstrainable
			=> new(self, constant, multiplier, self.GetOppositeAnchorInConstrainable(other), relation);

		public static UILayoutConstraint MakeConstraintToSuperview(
			this IUITypedAnchor<UIView> self,
			ClStrength strength,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
		{
			var superview = self.Owner.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.Owner.ConstrainableOwnerView} does not have a superview.");
			return self.MakeConstraintTo(self.GetSameAnchorInConstrainable(superview), strength, constant, multiplier, relation);
		}

		public static UILayoutConstraint MakeConstraintToSuperview(
			this IUITypedAnchor<UIView> self,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
		{
			var superview = self.Owner.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.Owner.ConstrainableOwnerView} does not have a superview.");
			return self.MakeConstraintTo(self.GetSameAnchorInConstrainable(superview), constant, multiplier, relation);
		}

		public static UILayoutConstraint MakeConstraintToSuperviewOpposite(
			this IUITypedAnchorWithOpposite<UIView> self,
			ClStrength strength,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
		{
			var superview = self.Owner.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.Owner.ConstrainableOwnerView} does not have a superview.");
			return self.MakeConstraintTo(self.GetOppositeAnchorInConstrainable(superview), strength, constant, multiplier, relation);
		}

		public static UILayoutConstraint MakeConstraintToSuperviewOpposite(
			this IUITypedAnchorWithOpposite<UIView> self,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal
		)
		{
			var superview = self.Owner.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.Owner.ConstrainableOwnerView} does not have a superview.");
			return self.MakeConstraintTo(self.GetOppositeAnchorInConstrainable(superview), constant, multiplier, relation);
		}
	}
}