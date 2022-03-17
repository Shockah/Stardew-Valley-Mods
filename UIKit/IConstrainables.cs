using Shockah.CommonModCode;
using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Shockah.UIKit
{
	public enum UILayoutConstraintMultipleEdgeRelation { Equal, Inside, Outside }

	public static class IConstrainables
	{
		#region Self

		[Pure]
		public static UILayoutConstraint MakeAspectRatioConstraint<ConstrainableType>(
			this ConstrainableType self,
			string? identifier,
			UIVector2 ratio,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
			=> self.MakeAspectRatioConstraint(identifier, ratio.X / ratio.Y, relation, priority);

		[Pure]
		public static UILayoutConstraint MakeAspectRatioConstraint<ConstrainableType>(
			this ConstrainableType self,
			UIVector2 ratio,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
			=> self.MakeAspectRatioConstraint(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "aspectRatio"),
				ratio, relation, priority
			);

		[Pure]
		public static UILayoutConstraint MakeAspectRatioConstraint<ConstrainableType>(
			this ConstrainableType self,
			string? identifier,
			float ratio = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
			=> new(identifier, self.WidthAnchor, 0f, ratio, self.HeightAnchor, relation, priority);

		[Pure]
		public static UILayoutConstraint MakeAspectRatioConstraint<ConstrainableType>(
			this ConstrainableType self,
			float ratio = 1f,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			where ConstrainableType : IConstrainable.Horizontal, IConstrainable.Vertical
			=> self.MakeAspectRatioConstraint(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "aspectRatio"),
				ratio, relation, priority
			);

		#endregion

		#region Any other constrainable

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeHorizontalEdgeConstraintsTo(
			this IConstrainable.Horizontal self,
			string? identifier,
			IConstrainable.Horizontal other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var singleEdgeLeftRelation = relation switch
			{
				UILayoutConstraintMultipleEdgeRelation.Equal => UILayoutConstraintRelation.Equal,
				UILayoutConstraintMultipleEdgeRelation.Inside => UILayoutConstraintRelation.GreaterThanOrEqual,
				UILayoutConstraintMultipleEdgeRelation.Outside => UILayoutConstraintRelation.LessThanOrEqual,
				_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
			};
			var singleEdgeRightRelation = singleEdgeLeftRelation.GetReverse();
			yield return self.LeftAnchor.MakeConstraintTo(identifier, other, insets, relation: singleEdgeLeftRelation, priority: priority);
			yield return self.RightAnchor.MakeConstraintTo(identifier, other, -insets, relation: singleEdgeRightRelation, priority: priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeHorizontalEdgeConstraintsTo(
			this IConstrainable.Horizontal self,
			IConstrainable.Horizontal other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeHorizontalEdgeConstraintsTo(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "horizontalEdges"),
				other, insets, relation, priority
			);

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsTo(
			this IConstrainable.Vertical self,
			string? identifier,
			IConstrainable.Vertical other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			var singleEdgeTopRelation = relation switch
			{
				UILayoutConstraintMultipleEdgeRelation.Equal => UILayoutConstraintRelation.Equal,
				UILayoutConstraintMultipleEdgeRelation.Inside => UILayoutConstraintRelation.GreaterThanOrEqual,
				UILayoutConstraintMultipleEdgeRelation.Outside => UILayoutConstraintRelation.LessThanOrEqual,
				_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
			};
			var singleEdgeBottomRelation = singleEdgeTopRelation.GetReverse();
			yield return self.TopAnchor.MakeConstraintTo(identifier, other, insets, relation: singleEdgeTopRelation, priority: priority);
			yield return self.BottomAnchor.MakeConstraintTo(identifier, other, -insets, relation: singleEdgeBottomRelation, priority: priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeVerticalEdgeConstraintsTo(
			this IConstrainable.Vertical self,
			IConstrainable.Vertical other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			=> self.MakeVerticalEdgeConstraintsTo(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "verticalEdges"),
				other, insets, relation, priority
			);

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			string? identifier,
			ConstrainableTypeB other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
		{
			foreach (var constraint in self.MakeHorizontalEdgeConstraintsTo(identifier, other, insets, relation, priority))
				yield return constraint;
			foreach (var constraint in self.MakeVerticalEdgeConstraintsTo(identifier, other, insets, relation, priority))
				yield return constraint;
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeEdgeConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			ConstrainableTypeB other,
			float insets = 0f,
			UILayoutConstraintMultipleEdgeRelation relation = UILayoutConstraintMultipleEdgeRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
			=> self.MakeEdgeConstraintsTo(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "edges"),
				other, insets, relation, priority
			);

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeCenterConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			string? identifier,
			ConstrainableTypeB other,
			UIVector2? offset = null,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
		{
			yield return self.CenterXAnchor.MakeConstraintTo(identifier, other, offset?.X ?? 0f, relation: relation, priority: priority);
			yield return self.CenterYAnchor.MakeConstraintTo(identifier, other, offset?.Y ?? 0f, relation: relation, priority: priority);
		}

		[Pure]
		public static IEnumerable<UILayoutConstraint> MakeCenterConstraintsTo<ConstrainableTypeA, ConstrainableTypeB>(
			this ConstrainableTypeA self,
			ConstrainableTypeB other,
			UIVector2? offset = null,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		)
			where ConstrainableTypeA : IConstrainable.Horizontal, IConstrainable.Vertical
			where ConstrainableTypeB : IConstrainable.Horizontal, IConstrainable.Vertical
			=> self.MakeCenterConstraintsTo(
				CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber, "center"),
				other, offset, relation, priority
			);

		#endregion
	}
}
