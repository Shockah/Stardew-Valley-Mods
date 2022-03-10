using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit
{
	public static class UIViews
	{
		public static UIVector2 ConvertPointBetweenViews(UIVector2 point, UIView from, UIView to)
		{
			if (from == to)
				return point;
			IList<UIView> aSuperviews = from.GetViewHierarchy(true).Reverse().ToList();
			IList<UIView> bSuperviews = to.GetViewHierarchy(true).Reverse().ToList();
			if (aSuperviews.Count == 0 || bSuperviews.Count == 0 || aSuperviews[0] != bSuperviews[0])
				throw new InvalidOperationException($"Views {from} and {to} are not in the same view hierarchy.");

			int GetCommonViewIndex()
			{
				int minCount = Math.Min(aSuperviews.Count, bSuperviews.Count);
				for (int i = 1; i < minCount; i++)
				{
					if (aSuperviews[i] == bSuperviews[i])
						continue;
					return i - 1;
				}
				return minCount - 1;
			}

			var commonIndex = GetCommonViewIndex();
			var index = aSuperviews.Count - 1;
			while (index-- > commonIndex)
				point += aSuperviews[index].TopLeft;
			while (index++ < bSuperviews.Count)
				point -= bSuperviews[index].TopLeft;
			return point;
		}

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
				if (aSuperviews[i] == bSuperviews[i])
					continue;
				return aSuperviews[i - 1];
			}
			return aSuperviews[minCount - 1];
		}
	}
}