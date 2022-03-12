using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Shockah.UIKit
{
	public static class UILayoutConstraints
	{
		[Pure]
		public static UIView? GetStoringView(this UILayoutConstraint self)
		{
			if (!self.IsActive)
				return null;
			return self.Anchor1.Owner.ConstrainableOwnerView.GetViewHierarchy(true).FirstOrDefault(v => v.Constraints.Contains(self))
				?? self.Anchor2?.Owner?.ConstrainableOwnerView?.GetViewHierarchy(true)?.FirstOrDefault(v => v.Constraints.Contains(self));
		}

		public static void Activate(this IEnumerable<UILayoutConstraint> constraints)
		{
			foreach (var constraint in constraints)
				constraint.Activate();
		}

		public static void Deactivate(this IEnumerable<UILayoutConstraint> constraints)
		{
			foreach (var constraint in constraints)
				constraint.Deactivate();
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEqualConstraints<ConstrainableType>(
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
					yield return new UILayoutConstraint(anchor(enumerator.Current), anchor2: anchor(first), priority: priority);
				}
			}
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEqualConstraints<ConstrainableType>(
			Func<ConstrainableType, UIAnchor> anchor,
			UILayoutConstraintPriority priority,
			params ConstrainableType[] constrainables
		) where ConstrainableType : IConstrainable
			=> MakeEqualConstraints(anchor, constrainables, priority);

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEqualConstraints<ConstrainableType>(
			Func<ConstrainableType, UIAnchor> anchor,
			params ConstrainableType[] constrainables
		) where ConstrainableType : IConstrainable
			=> MakeEqualConstraints(anchor, constrainables, null);
	}
}