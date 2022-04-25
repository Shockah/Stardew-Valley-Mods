using Cassowary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shockah.UIKit.Solver
{
	public class CassowaryLinearSolver: ILinearSolver
	{
		private ClSimplexSolver Solver = new() { AutoSolve = false };
		private readonly ISet<LinearSolverEquation> Current = new HashSet<LinearSolverEquation>();
		private readonly ISet<LinearSolverEquation> ToAdd = new HashSet<LinearSolverEquation>();
		private readonly ISet<LinearSolverEquation> ToRemove = new HashSet<LinearSolverEquation>();
		private readonly ConditionalWeakTable<LinearSolverEquation, ClConstraint> EquationConstraintMap = new();
		private readonly ConditionalWeakTable<ILinearSolverVariable, ClAbstractVariable> VariableMap = new();

		public void AddEquation(LinearSolverEquation equation)
		{
			ToRemove.Remove(equation);
			if (!Current.Contains(equation))
				ToAdd.Add(equation);
		}

		public void RemoveEquation(LinearSolverEquation equation)
		{
			ToAdd.Remove(equation);
			if (Current.Contains(equation))
				ToRemove.Add(equation);
		}

		public void Reset()
		{
			Solver = new() { AutoSolve = false };
		}

		public LinearSolverResult Solve()
		{
			foreach (var equation in ToRemove)
			{
				if (EquationConstraintMap.TryGetValue(equation, out var constraint))
				{
					Current.Add(equation);
					try
					{
						Solver.RemoveConstraint(constraint);
					}
					catch (CassowaryConstraintNotFoundException)
					{
					}
					catch (KeyNotFoundException)
					{
					}
				}
			}
			ToRemove.Clear();

			foreach (var equation in ToAdd.OrderByDescending(e => e.IsRequired).ThenByDescending(e => e.Priority))
			{
				try
				{
					if (!EquationConstraintMap.TryGetValue(equation, out var constraint))
					{
						constraint = CreateConstraintFromEquation(equation);
						EquationConstraintMap.Add(equation, constraint);
					}
					Current.Remove(equation);
					Solver.AddConstraint(constraint);
				}
				catch
				{
					//UnsatifiableConstraintEvent?.Invoke(this, constraint);
				}
			}
			ToAdd.Clear();

			Solver.Solve();
			throw new NotImplementedException();
		}

		private ClConstraint CreateConstraintFromEquation(LinearSolverEquation equation)
		{
			var rhs = new ClLinearExpression(equation.Right);
			var lhs = MemoryMarshal.ToEnumerable(equation.Left)
					.Select(c => CreateCassowaryExpression(c))
					.Aggregate((a, b) => a.Plus(b));
			if (lhs is null)
				throw new ArgumentException($"Invalid `{nameof(equation)}`.");

			return equation.Op switch
			{
				LinearSolverEquation.Operator.Equals =>
					new ClLinearEquation(lhs, rhs, GetStrength(equation.IsRequired, equation.Priority)),
				LinearSolverEquation.Operator.LessThanOrEqual =>
					new ClLinearInequality(lhs, Cl.Operator.LessThanOrEqualTo, rhs, GetStrength(equation.IsRequired, equation.Priority)),
				LinearSolverEquation.Operator.GreaterThanOrEqual =>
					new ClLinearInequality(lhs, Cl.Operator.GreaterThanOrEqualTo, rhs, GetStrength(equation.IsRequired, equation.Priority)),
				_ => throw new ArgumentException($"{nameof(LinearSolverEquation.Operator)} has an invalid value.")
			};
		}

		private static ClStrength GetStrength(bool isRequired, double priority)
			=> isRequired || priority >= 1000 ? ClStrength.Required : new($"{priority}", new(0f, priority, 0f));

		private ClLinearExpression CreateCassowaryExpression(LinearSolverEquation.Coefficient coefficient)
			=> new ClLinearExpression(RetrieveVariable(coefficient.Variable)).Times(coefficient.Multiplier);

		private ClAbstractVariable RetrieveVariable(ILinearSolverVariable variable)
		{
			if (!VariableMap.TryGetValue(variable, out var cassowaryVariable))
			{
				cassowaryVariable = new ClVariable($"{Guid.NewGuid()}");
				VariableMap.Add(variable, cassowaryVariable);
			}
			return cassowaryVariable;
		}
	}
}
