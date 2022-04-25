using System;

namespace Shockah.UIKit.Solver
{
	public sealed class LinearSolverEquation
	{
		public readonly struct Coefficient
		{
			public readonly ILinearSolverVariable Variable;
			public readonly double Multiplier;

			public Coefficient(ILinearSolverVariable variable, double multiplier = 1)
			{
				this.Variable = variable;
				this.Multiplier = multiplier;
			}
		}

		public enum Operator
		{
			Equals, GreaterThanOrEqual, LessThanOrEqual
		}

		public readonly ReadOnlyMemory<Coefficient> Left;
		public readonly Operator Op;
		public readonly double Right;
		public readonly bool IsRequired;
		public readonly double Priority;

		public LinearSolverEquation(ReadOnlyMemory<Coefficient> left, double right, bool isRequired = true, double priority = 1000)
			: this(left, Operator.Equals, right, isRequired, priority) { }

		public LinearSolverEquation(ReadOnlyMemory<Coefficient> left, Operator op, double right, bool isRequired = true, double priority = 1000)
		{
			this.Left = left;
			this.Op = op;
			this.Right = right;
			this.IsRequired = isRequired;
			this.Priority = priority;
		}
	}
}
