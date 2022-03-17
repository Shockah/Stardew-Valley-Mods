using Cassowary;
using Shockah.CommonModCode;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Shockah.UIKit
{
	public readonly struct UILayoutConstraintPriority: IEquatable<UILayoutConstraintPriority>, IComparable<UILayoutConstraintPriority>
	{
		public static UILayoutConstraintPriority Required => _required;
		public static UILayoutConstraintPriority High => _high;
		public static UILayoutConstraintPriority Medium => _medium;
		public static UILayoutConstraintPriority Low => _low;
		public static UILayoutConstraintPriority OptimalCalculations => _optimalCalculations;

		private static readonly UILayoutConstraintPriority _required = new(1000f);
		private static readonly UILayoutConstraintPriority _high = new(750f);
		private static readonly UILayoutConstraintPriority _medium = new(500f);
		private static readonly UILayoutConstraintPriority _low = new(250f);
		private static readonly UILayoutConstraintPriority _optimalCalculations = new(50f);

		public readonly float Value { get; }
		internal readonly ClStrength Strength { get; }

		public UILayoutConstraintPriority(float value = 1000f)
		{
			Value = Math.Clamp(value, 0f, 1000f);
			Strength = Value >= 1000f ? ClStrength.Required : new($"{Value}", new(0f, Value, 0f));
		}

		public override string ToString()
			=> $"{Value}";

		public bool Equals(UILayoutConstraintPriority other)
			=> Strength.SymbolicWeight.Equal(other.Strength.SymbolicWeight);

		public override bool Equals(object? obj)
			=> obj is UILayoutConstraintPriority other && Equals(other);

		public override int GetHashCode()
			=> Strength.SymbolicWeight.AsDouble().GetHashCode();

		public int CompareTo(UILayoutConstraintPriority other)
			=> Strength.SymbolicWeight.AsDouble().CompareTo(other.Strength.SymbolicWeight.AsDouble());

		public static bool operator ==(UILayoutConstraintPriority left, UILayoutConstraintPriority right)
			=> left.Equals(right);

		public static bool operator !=(UILayoutConstraintPriority left, UILayoutConstraintPriority right)
			=> !(left == right);

		public static bool operator <(UILayoutConstraintPriority left, UILayoutConstraintPriority right)
			=> left.Strength.SymbolicWeight.LessThan(right.Strength.SymbolicWeight);

		public static bool operator <=(UILayoutConstraintPriority left, UILayoutConstraintPriority right)
			=> left.Strength.SymbolicWeight.LessThanOrEqual(right.Strength.SymbolicWeight);

		public static bool operator >(UILayoutConstraintPriority left, UILayoutConstraintPriority right)
			=> left.Strength.SymbolicWeight.GreaterThan(right.Strength.SymbolicWeight);

		public static bool operator >=(UILayoutConstraintPriority left, UILayoutConstraintPriority right)
			=> !left.Strength.SymbolicWeight.LessThan(right.Strength.SymbolicWeight);
	}

	public enum UILayoutConstraintRelation { LessThanOrEqual, Equal, GreaterThanOrEqual }

	public static class UILayoutConstraintRelationExt
	{
		public static string GetSymbol(this UILayoutConstraintRelation self)
			=> self switch
			{
				UILayoutConstraintRelation.Equal => "==",
				UILayoutConstraintRelation.GreaterThanOrEqual => ">=",
				UILayoutConstraintRelation.LessThanOrEqual => "<=",
				_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
			};

		public static UILayoutConstraintRelation GetReverse(this UILayoutConstraintRelation self)
			=> self switch
			{
				UILayoutConstraintRelation.Equal => UILayoutConstraintRelation.Equal,
				UILayoutConstraintRelation.GreaterThanOrEqual => UILayoutConstraintRelation.LessThanOrEqual,
				UILayoutConstraintRelation.LessThanOrEqual => UILayoutConstraintRelation.GreaterThanOrEqual,
				_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
			};
	}

	public interface IUILayoutConstraint : IEquatable<IUILayoutConstraint>
	{
		UIView? Holder { get; }
		UILayoutConstraintPriority Priority { get; }
		ClConstraint CassowaryConstraint { get; }
		IReadOnlyCollection<IUIAnchor> Anchors { get; }

		void Activate();
		void Deactivate();

		bool IsActive => Holder is not null;
	}
	
	public class UILayoutConstraint: IUILayoutConstraint
	{
		public IUIAnchor Anchor1 { get; }
		public IUIAnchor? Anchor2 { get; }
		public float Constant { get; }
		public float Multiplier { get; }
		public UILayoutConstraintRelation Relation { get; }
		public UILayoutConstraintPriority Priority { get; }
		public string? Identifier { get; }

		public UIView? Holder { get; private set; }
		public ClConstraint CassowaryConstraint => LazyCassowaryConstraint.Value;
		public IReadOnlyCollection<IUIAnchor> Anchors => Anchor2 is null ? new[] { Anchor1 } : new[] { Anchor1, Anchor2 };

		private readonly Lazy<ClConstraint> LazyCassowaryConstraint;

		public UILayoutConstraint(
			IUIAnchor anchor1,
			float constant = 0f,
			float multiplier = 1f,
			IUIAnchor? anchor2 = null,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null,
			[CallerFilePath] string? callerFilePath = null,
			[CallerMemberName] string? callerMemberName = null,
			[CallerLineNumber] int? callerLineNumber = null
		) : this(
			CallerIdentifiers.GetCallerIdentifier(callerFilePath, callerMemberName, callerLineNumber),
			anchor1, constant, multiplier, anchor2, relation, priority
		)
		{
		}

		public UILayoutConstraint(
			string? identifier,
			IUIAnchor anchor1,
			float constant = 0f,
			float multiplier = 1f,
			IUIAnchor? anchor2 = null,
			UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal,
			UILayoutConstraintPriority? priority = null
		)
		{
			if (anchor2 is not null && !anchor1.IsCompatibleWithAnchor(anchor2))
				throw new ArgumentException($"Anchors {anchor1} and {anchor2} are not compatible with each other.");
			this.Identifier = identifier;
			this.Anchor1 = anchor1;
			this.Constant = constant;
			this.Multiplier = multiplier;
			this.Anchor2 = anchor2;
			this.Relation = relation;
			this.Priority = priority ?? UILayoutConstraintPriority.Required;
			LazyCassowaryConstraint = new(() => CreateCassowaryConstraint());
		}

		public override string ToString()
		{
			StringBuilder sb = new();
			if (Identifier is not null)
				sb.Append($"'{Identifier}' ");
			sb.Append($"{Anchor1} {Relation.GetSymbol()}");
			if (Anchor2 is null)
			{
				sb.Append($" {Constant}");
			}
			else
			{
				sb.Append($" {Anchor2}");
				if (Multiplier != 1f)
					sb.Append($" * {Multiplier}");

				if (Constant > 0f)
					sb.Append($" + {Constant}");
				else if (Constant < 0f)
					sb.Append($" - {-Constant}");
			}
			sb.Append($" @{Priority}");
			return sb.ToString();
		}

		private ClConstraint CreateCassowaryConstraint()
		{
			if (Anchor2 is null)
			{
				return Relation switch
				{
					UILayoutConstraintRelation.Equal =>
						new ClLinearEquation(Anchor1.Expression, new ClLinearExpression(Constant), Priority.Strength),
					UILayoutConstraintRelation.LessThanOrEqual =>
						new ClLinearInequality(Anchor1.Expression, Cl.Operator.LessThanOrEqualTo, new ClLinearExpression(Constant), Priority.Strength),
					UILayoutConstraintRelation.GreaterThanOrEqual =>
						new ClLinearInequality(Anchor1.Expression, Cl.Operator.GreaterThanOrEqualTo, new ClLinearExpression(Constant), Priority.Strength),
					_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
				};
			}
			else
			{
				return Relation switch
				{
					UILayoutConstraintRelation.Equal =>
						new ClLinearEquation(Anchor1.Expression, Anchor2.Expression.Times(Multiplier).Plus(new ClLinearExpression(Constant)), Priority.Strength),
					UILayoutConstraintRelation.LessThanOrEqual =>
						new ClLinearInequality(Anchor1.Expression, Cl.Operator.LessThanOrEqualTo, Anchor2.Expression.Times(Multiplier).Plus(new ClLinearExpression(Constant)), Priority.Strength),
					UILayoutConstraintRelation.GreaterThanOrEqual =>
						new ClLinearInequality(Anchor1.Expression, Cl.Operator.GreaterThanOrEqualTo, Anchor2.Expression.Times(Multiplier).Plus(new ClLinearExpression(Constant)), Priority.Strength),
					_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
				};
			}
		}

		public void Activate()
		{
			if (Holder is not null)
				return;
			var constraintOwningView = GetViewToPutConstraintOn();
			(constraintOwningView.Root ?? constraintOwningView as UIRootView)?.QueueAddConstraint(this);
			constraintOwningView.HeldConstraints.Add(this);
			Holder = constraintOwningView;
		}

		public void Deactivate()
		{
			if (Holder is null)
				return;
			Holder.HeldConstraints.Remove(this);
			(Holder.Root ?? Holder as UIRootView)?.QueueRemoveConstraint(this);
			Holder = null;
		}

		private UIView GetViewToPutConstraintOn()
		{
			var owner1 = Anchor1.Owner.ConstrainableOwnerView;
			var owner2 = Anchor2?.Owner.ConstrainableOwnerView;
			if (owner2 is null || owner1 == owner2)
			{
				return owner1;
			}
			else
			{
				var commonView = UIViews.GetCommonSuperview(owner1, owner2);
				if (commonView is null)
					throw new InvalidOperationException($"Cannot add a constraint between unrelated views {owner1} and {owner2}.");
				return commonView;
			}
		}

		public bool Equals(IUILayoutConstraint? obj)
			=> obj is UILayoutConstraint other
			&& other.Anchor1 == Anchor1
			&& other.Anchor2 == Anchor2
			&& other.Constant == Constant
			&& other.Multiplier == Multiplier
			&& other.Relation == Relation
			&& other.Priority == Priority;

		public override bool Equals(object? obj)
			=> obj is UILayoutConstraint other && Equals(other);

		public override int GetHashCode()
			=> (Anchor1, Anchor2, Constant, Multiplier, Relation, Priority).GetHashCode();

		public static bool operator ==(UILayoutConstraint lhs, UILayoutConstraint rhs)
			=> lhs.Equals(rhs);

		public static bool operator !=(UILayoutConstraint lhs, UILayoutConstraint rhs)
			=> !lhs.Equals(rhs);
	}
}