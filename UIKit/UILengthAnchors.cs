using Shockah.CommonModCode;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Shockah.UIKit
{
	public static class UILengthAnchors
	{
		[Pure]
		public static UILayoutConstraint MakeConstraint<ConstrainableType>(
			this IUIAnchor.Typed<ConstrainableType>.Length self,
			string? identifier,
			float constant,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		) where ConstrainableType : IConstrainable
			=> new(identifier, self, constant, relation: relation, priority: priority);

		[Pure]
		public static UILayoutConstraint MakeConstraint<ConstrainableType>(
			this IUIAnchor.Typed<ConstrainableType>.Length self,
			float constant,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		) where ConstrainableType : IConstrainable
			=> self.MakeConstraint(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber),
				constant, relation, priority
			);

		[Pure]
		public static UILayoutConstraint MakeConstraintTo<ConstrainableType>(
			this IUIAnchor.Typed<ConstrainableType>.Length self,
			string? identifier,
			IUIAnchor.Typed<ConstrainableType>.Length other,
			float constant = 0f,
			float multiplier = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		) where ConstrainableType : IConstrainable
			=> new(identifier, self, constant, multiplier, other, relation, priority);

		[Pure]
		public static UILayoutConstraint MakeConstraintTo<ConstrainableType>(
			this IUIAnchor.Typed<ConstrainableType>.Length self,
			IUIAnchor.Typed<ConstrainableType>.Length other,
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
		public static UILayoutConstraint MakeConstraintTo<ConstrainableType>(
			this IUIAnchor.Typed<ConstrainableType>.Length self,
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
			this IUIAnchor.Typed<ConstrainableType>.Length self,
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
		public static UILayoutConstraint MakeConstraintToSuperview(
			this IUIAnchor.Typed<UIView>.Length self,
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
			this IUIAnchor.Typed<UIView>.Length self,
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
	}
}