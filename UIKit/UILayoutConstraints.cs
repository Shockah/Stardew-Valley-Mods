using Shockah.CommonModCode;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Shockah.UIKit
{
	public static class UILayoutConstraints
	{
		public static void Activate(this IEnumerable<IUILayoutConstraint> constraints)
		{
			foreach (var constraint in constraints)
				constraint.Activate();
		}

		public static void Deactivate(this IEnumerable<IUILayoutConstraint> constraints)
		{
			foreach (var constraint in constraints)
				constraint.Deactivate();
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEqualConstraints<ConstrainableType>(
			string? identifier,
			Func<ConstrainableType, UIAnchor> anchor,
			IEnumerable<ConstrainableType> constrainables,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableType: IConstrainable
		{
			var enumerator = constrainables.GetEnumerator();
			ConstrainableType? first = default;
			while (enumerator.MoveNext())
			{
				if (first is null)
				{
					first = enumerator.Current;
				}
				else
				{
					yield return new UILayoutConstraint(identifier, anchor(enumerator.Current), anchor2: anchor(first), priority: priority);
				}
			}
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEqualConstraints<ConstrainableType>(
			Func<ConstrainableType, UIAnchor> anchor,
			IEnumerable<ConstrainableType> constrainables,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			where ConstrainableType : IConstrainable
			=> MakeEqualConstraints(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "equalConstraints"),
				anchor, constrainables, priority
			);
	}
}