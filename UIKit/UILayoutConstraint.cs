using Cassowary;
using System;

namespace Shockah.UIKit
{
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
	
	public class UILayoutConstraint: IEquatable<UILayoutConstraint>
	{
		public IUIAnchor Anchor1 { get; }
		public IUIAnchor? Anchor2 { get; }
		public float Constant { get; }
		public float Multiplier { get; }
		public UILayoutConstraintRelation Relation { get; }
		public ClStrength Strength { get; }

		public bool IsActive { get; private set; } = false;
		internal readonly Lazy<ClConstraint> CassowaryConstraint;

		public UILayoutConstraint(IUIAnchor anchor1, float constant = 0f, float multiplier = 1f, IUIAnchor? anchor2 = null, UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal)
			: this(ClStrength.Required, anchor1, constant, multiplier, anchor2, relation)
		{
		}

		public UILayoutConstraint(ClStrength strength, IUIAnchor anchor1, float constant = 0f, float multiplier = 1f, IUIAnchor? anchor2 = null, UILayoutConstraintRelation relation = UILayoutConstraintRelation.Equal)
		{
			this.Anchor1 = anchor1;
			this.Constant = constant;
			this.Multiplier = multiplier;
			this.Anchor2 = anchor2;
			this.Relation = relation;
			this.Strength = strength;
			CassowaryConstraint = new(() => CreateCassowaryConstraint());
		}

		public override string ToString()
		{
			if (Anchor2 is null)
				return $"{Anchor1} {Relation.GetSymbol()} {Constant} @{Strength}";
			else
				return $"{Anchor1} {Relation.GetSymbol()} {Anchor2} * {Multiplier} + {Constant} @{Strength}";
		}

		private ClConstraint CreateCassowaryConstraint()
		{
			var anchor1 = Anchor1 as IUIAnchor.Internal ?? throw new InvalidOperationException();
			IUIAnchor.Internal? anchor2 = null;
			if (Anchor2 is not null)
			{
				if (Anchor2 is IUIAnchor.Internal @internal)
					anchor2 = @internal;
				else
					throw new InvalidOperationException();
			}

			if (anchor2 is null)
			{
				return Relation switch
				{
					UILayoutConstraintRelation.Equal =>
						new ClLinearEquation(anchor1.Expression, new ClLinearExpression(Constant), Strength),
					UILayoutConstraintRelation.LessThanOrEqual =>
						new ClLinearInequality(anchor1.Expression, Cl.Operator.LessThanOrEqualTo, new ClLinearExpression(Constant), Strength),
					UILayoutConstraintRelation.GreaterThanOrEqual =>
						new ClLinearInequality(anchor1.Expression, Cl.Operator.GreaterThanOrEqualTo, new ClLinearExpression(Constant), Strength),
					_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
				};
			}
			else
			{
				return Relation switch
				{
					UILayoutConstraintRelation.Equal =>
						new ClLinearEquation(anchor1.Expression, anchor2.Expression.Times(Multiplier).Plus(new ClLinearExpression(Constant)), Strength),
					UILayoutConstraintRelation.LessThanOrEqual =>
						new ClLinearInequality(anchor1.Expression, Cl.Operator.LessThanOrEqualTo, anchor2.Expression.Times(Multiplier).Plus(new ClLinearExpression(Constant)), Strength),
					UILayoutConstraintRelation.GreaterThanOrEqual =>
						new ClLinearInequality(anchor1.Expression, Cl.Operator.GreaterThanOrEqualTo, anchor2.Expression.Times(Multiplier).Plus(new ClLinearExpression(Constant)), Strength),
					_ => throw new InvalidOperationException($"{nameof(UILayoutConstraintRelation)} has an invalid value."),
				};
			}
		}

		public void Activate()
		{
			if (IsActive)
				return;
			var constraintOwningView = GetViewToPutConstraintOn();
			constraintOwningView._heldConstraints.Add(this);
			(constraintOwningView.Root ?? constraintOwningView as UIRoot)?.ConstraintSolver.TryAddConstraint(CassowaryConstraint.Value);
			Anchor1.Owner.ConstrainableOwnerView.AddConstraint(this);
			Anchor2?.Owner.ConstrainableOwnerView.AddConstraint(this);
			IsActive = true;
		}

		public void Deactivate()
		{
			if (!IsActive)
				return;
			var constraintOwningView = GetViewToPutConstraintOn();
			constraintOwningView._heldConstraints.Remove(this);
			(constraintOwningView.Root ?? constraintOwningView as UIRoot)?.ConstraintSolver.RemoveConstraint(CassowaryConstraint.Value);
			Anchor1.Owner.ConstrainableOwnerView.RemoveConstraint(this);
			Anchor2?.Owner.ConstrainableOwnerView.RemoveConstraint(this);
			IsActive = false;
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
				var commonView = UIViewExt.GetCommonSuperview(owner1, owner2);
				if (commonView is null)
					throw new InvalidOperationException($"Cannot add a constraint between unrelated views {owner1} and {owner2}.");
				return commonView;
			}
		}

		public bool Equals(UILayoutConstraint? other)
			=> other is not null
			&& other.Anchor1 == Anchor1
			&& other.Anchor2 == Anchor2
			&& other.Constant == Constant
			&& other.Multiplier == Multiplier;

		public override bool Equals(object? obj)
			=> obj is UILayoutConstraint other && Equals(other);

		public override int GetHashCode()
			=> (Anchor1, Anchor2, Constant, Multiplier).GetHashCode();

		public static bool operator ==(UILayoutConstraint lhs, UILayoutConstraint rhs)
			=> lhs.Equals(rhs);

		public static bool operator !=(UILayoutConstraint lhs, UILayoutConstraint rhs)
			=> !lhs.Equals(rhs);
	}
}