using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit
{
	public static class UIViewExt
	{
		public static void AddToSuperview(this UIView subview, UIView superview)
			=> superview.AddSubview(subview);

		public static void RemoveSubview(this UIView superview, UIView subview)
		{
			if (subview.Superview != superview)
				throw new InvalidOperationException($"View {subview} is not a subview of {superview}.");
			subview.RemoveFromSuperview();
		}

		public static IEnumerable<UIView> GetViewHierarchy(this UIView self, bool includeSelf)
		{
			if (includeSelf)
				yield return self;
			var current = self.Superview;
			while (current is not null)
			{
				yield return current;
				current = current.Superview;
			}
		}

		public static UIView? GetCommonSuperview(UIView a, UIView b)
		{
			if (a == b)
				return a;
			var aSuperviews = a.GetViewHierarchy(true).Reverse().ToList();
			var bSuperviews = b.GetViewHierarchy(true).Reverse().ToList();
			if (aSuperviews.Count == 0 || bSuperviews.Count == 0 || aSuperviews[0] != bSuperviews[0])
				return null;
			int minCount = Math.Min(aSuperviews.Count, bSuperviews.Count);
			for (int i = 1; i < minCount; i++)
			{
				if (aSuperviews[i] != bSuperviews[i])
					return aSuperviews[i - 1];
			}
			return aSuperviews[0];
		}
	}
}