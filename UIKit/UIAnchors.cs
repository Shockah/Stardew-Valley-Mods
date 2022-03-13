using Shockah.CommonModCode;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Shockah.UIKit
{
	public static class UIAnchors
	{
		[Pure]
		public static UILayoutConstraint MakeConstraint(
			this IUIAnchor self,
			string? identifier,
			float constant,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			=> new(identifier, self, constant, relation: relation, priority: priority);

		[Pure]
		public static UILayoutConstraint MakeConstraint(
			this IUIAnchor self,
			float constant,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeConstraint(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber),
				constant, relation, priority
			);

		[Pure]
		public static UILayoutConstraint MakeConstraintTo(
			this IUIAnchor self,
			string? identifier,
			IUIAnchor other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			=> new(identifier, self, constant, multiplier, other, relation, priority);

		[Pure]
		public static UILayoutConstraint MakeConstraintTo(
			this IUIAnchor self,
			IUIAnchor other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeConstraintTo(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber),
				other, constant, multiplier, relation, priority
			);

		[Pure]
		public static UILayoutConstraint MakeConstraintTo<ConstrainableType>(
			this IUITypedAnchor<ConstrainableType> self,
			string? identifier,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		) where ConstrainableType : IConstrainable
			=> new(identifier, self, constant, multiplier, self.GetSameAnchorInConstrainable(other), relation, priority);

		[Pure]
		public static UILayoutConstraint MakeConstraintTo<ConstrainableType>(
			this IUITypedAnchor<ConstrainableType> self,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		) where ConstrainableType : IConstrainable
			=> self.MakeConstraintTo(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber),
				other, constant, multiplier, relation, priority
			);

		[Pure]
		public static UILayoutConstraint MakeConstraintToOpposite<ConstrainableType>(
			this IUITypedAnchorWithOpposite<ConstrainableType> self,
			string? identifier,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		) where ConstrainableType : IConstrainable
			=> new(identifier, self, constant, multiplier, self.GetOppositeAnchorInConstrainable(other), relation, priority);

		[Pure]
		public static UILayoutConstraint MakeConstraintToOpposite<ConstrainableType>(
			this IUITypedAnchorWithOpposite<ConstrainableType> self,
			ConstrainableType other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		) where ConstrainableType : IConstrainable
			=> self.MakeConstraintToOpposite(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber),
				other, constant, multiplier, relation, priority
			);

		[Pure]
		public static UILayoutConstraint MakeConstraintToSuperview(
			this IUITypedAnchor<UIView> self,
			string? identifier,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.Owner.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.Owner.ConstrainableOwnerView} does not have a superview.");
			return self.MakeConstraintTo(identifier, self.GetSameAnchorInConstrainable(superview), constant, multiplier, relation, priority);
		}

		[Pure]
		public static UILayoutConstraint MakeConstraintToSuperview(
			this IUITypedAnchor<UIView> self,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeConstraintToSuperview(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "toSuperview"),
				constant, multiplier, relation, priority
			);

		[Pure]
		public static UILayoutConstraint MakeConstraintToSuperviewOpposite(
			this IUITypedAnchorWithOpposite<UIView> self,
			string? identifier,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.Owner.ConstrainableOwnerView.Superview
				?? throw new InvalidOperationException($"View {self.Owner.ConstrainableOwnerView} does not have a superview.");
			return self.MakeConstraintTo(identifier, self.GetOppositeAnchorInConstrainable(superview), constant, multiplier, relation, priority);
		}

		[Pure]
		public static UILayoutConstraint MakeConstraintToSuperviewOpposite(
			this IUITypedAnchorWithOpposite<UIView> self,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeConstraintToSuperviewOpposite(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "toSuperview"),
				constant, multiplier, relation, priority
			);
	}
}