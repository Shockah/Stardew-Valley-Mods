using Shockah.CommonModCode;
using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Shockah.UIKit
{
	public static class UIViewConstraintsExt
	{
		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeHorizontalEdgeConstraintsToSuperview(
			this UIView self,
			string? identifier,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.Superview
				?? throw new InvalidOperationException($"View {self} does not have a superview.");
			return self.MakeHorizontalEdgeConstraintsTo(identifier, superview, insets, relation, priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeHorizontalEdgeConstraintsToSuperview(
			this UIView self,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeHorizontalEdgeConstraintsToSuperview(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "horizontalEdges-toSuperview"),
				insets, relation, priority
			);

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsToSuperview(
			this UIView self,
			string? identifier,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.Superview
				?? throw new InvalidOperationException($"View {self} does not have a superview.");
			return self.MakeVerticalEdgeConstraintsTo(identifier, superview, insets, relation, priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsToSuperview(
			this UIView self,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeVerticalEdgeConstraintsToSuperview(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "verticalEdges-toSuperview"),
				insets, relation, priority
			);

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsToSuperview(
			this UIView self,
			string? identifier,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.Superview
				?? throw new InvalidOperationException($"View {self} does not have a superview.");
			return self.MakeEdgeConstraintsTo(identifier, superview, insets, relation, priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsToSuperview(
			this UIView self,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeEdgeConstraintsToSuperview(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "edges-toSuperview"),
				insets, relation, priority
			);

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeCenterConstraintsToSuperview(
			this UIView self,
			string? identifier,
			UIVector2? offset = null,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var superview = self.Superview
				?? throw new InvalidOperationException($"View {self} does not have a superview.");
			return self.MakeCenterConstraintsTo(identifier, superview, offset, relation, priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeCenterConstraintsToSuperview(
			this UIView self,
			UIVector2? offset = null,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeCenterConstraintsToSuperview(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "center-toSuperview"),
				offset, relation, priority
			);
	}
}
