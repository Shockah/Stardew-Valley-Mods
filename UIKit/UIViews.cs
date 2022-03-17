using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Shockah.UIKit
{
	public enum UIViewVisitingOrder { SuperviewFirstSubviewOrder, SuperviewLastSubviewOrder, VisibleOrder, HoverOrder }

	public static class UIViews
	{
		[Pure]
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
			for (int i = aSuperviews.Count - 1; i > commonIndex; i--)
				point += aSuperviews[i].TopLeft;
			for (int i = commonIndex + 1; i < bSuperviews.Count; i++)
				point -= bSuperviews[i].TopLeft;
			return point;
		}

		[Pure]
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

		[Pure]
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

		[Pure]
		public static IEnumerable<UIView> VisitAllViews(this UIView self, UIViewVisitingOrder order, bool includeSelf = true)
		{
			return order switch
			{
				UIViewVisitingOrder.SuperviewFirstSubviewOrder => self.VisitAllViewsInSuperviewFirstSubviewOrder(true),
				UIViewVisitingOrder.SuperviewLastSubviewOrder => self.VisitAllViewsInSuperviewLastSubviewOrder(true),
				UIViewVisitingOrder.VisibleOrder => self.VisitAllViewsInVisibleOrder(true),
				_ => throw new InvalidOperationException($"{nameof(UIViewVisitingOrder)} has an invalid value.")
			};
		}

		[Pure]
		private static IEnumerable<UIView> VisitAllViewsInSuperviewFirstSubviewOrder(this UIView self, bool includeSelf)
		{
			if (includeSelf)
				yield return self;
			foreach (var subview in self.Subviews)
				foreach (var toReturn in subview.VisitAllViewsInSuperviewFirstSubviewOrder(true))
					yield return toReturn;
		}

		[Pure]
		private static IEnumerable<UIView> VisitAllViewsInSuperviewLastSubviewOrder(this UIView self, bool includeSelf)
		{
			foreach (var subview in self.Subviews)
				foreach (var toReturn in subview.VisitAllViewsInSuperviewLastSubviewOrder(true))
					yield return toReturn;
			if (includeSelf)
				yield return self;
		}

		[Pure]
		private static IEnumerable<UIView> VisitAllViewsInVisibleOrder(this UIView self, bool includeSelf)
		{
			foreach (var subview in self.Subviews.Reverse())
				foreach (var toReturn in subview.VisitAllViewsInVisibleOrder(true))
					yield return toReturn;
			if (includeSelf)
				yield return self;
		}
	}
}