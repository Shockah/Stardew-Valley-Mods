using Cassowary;
using Shockah.UIKit.Gesture;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit
{
	public class UIRootView: UIView
	{
		public IGestureRecognizerManager GestureRecognizerManager { get; private set; }

		public event UnsatisfiableConstraintEvent? UnsatifiableConstraintEvent;
		public event RenderedView? RenderedViewEvent;

		private ClSimplexSolver ConstraintSolver { get; set; } = new() { AutoSolve = false };

		private float LastX1 { get; set; } = 0f;
		private float LastY1 { get; set; } = 0f;
		private float LastX2 { get; set; } = 0f;
		private float LastY2 { get; set; } = 0f;
		private ClConstraint? LeftConstraint;
		private ClConstraint? RightConstraint;
		private ClConstraint? TopConstraint;
		private ClConstraint? BottomConstraint;

		private readonly ISet<IUILayoutConstraint> QueuedConstraintsToAdd = new HashSet<IUILayoutConstraint>();
		private readonly ISet<IUILayoutConstraint> QueuedConstraintsToRemove = new HashSet<IUILayoutConstraint>();

		public UIRootView(IGestureRecognizerManager gestureRecognizerManager)
		{
			this.GestureRecognizerManager = gestureRecognizerManager;
		}

		public void SolveLayout()
		{
			foreach (var constraint in QueuedConstraintsToRemove)
				ConstraintSolver.RemoveConstraintIfExists(constraint.CassowaryConstraint);
			QueuedConstraintsToRemove.Clear();

			if (X1 != LastX1 || LeftConstraint is null)
			{
				if (LeftConstraint is not null)
					ConstraintSolver.RemoveConstraintIfExists(LeftConstraint);
				LeftConstraint = new ClLinearEquation(new ClLinearExpression(LeftVariable.Value), new ClLinearExpression(X1));
				ConstraintSolver.TryAddConstraint(LeftConstraint);
				LastX1 = X1;
			}
			if (Y1 != LastY1 || TopConstraint is null)
			{
				if (TopConstraint is not null)
					ConstraintSolver.RemoveConstraintIfExists(TopConstraint);
				TopConstraint = new ClLinearEquation(new ClLinearExpression(TopVariable.Value), new ClLinearExpression(Y1));
				ConstraintSolver.TryAddConstraint(TopConstraint);
				LastY1 = Y1;
			}
			if (X2 != LastX2 || RightConstraint is null)
			{
				if (RightConstraint is not null)
					ConstraintSolver.RemoveConstraintIfExists(RightConstraint);
				RightConstraint = new ClLinearEquation(new ClLinearExpression(RightVariable.Value), new ClLinearExpression(X2));
				ConstraintSolver.TryAddConstraint(RightConstraint);
				LastX2 = X2;
			}
			if (Y2 != LastY2 || BottomConstraint is null)
			{
				if (BottomConstraint is not null)
					ConstraintSolver.RemoveConstraintIfExists(BottomConstraint);
				BottomConstraint = new ClLinearEquation(new ClLinearExpression(BottomVariable.Value), new ClLinearExpression(Y2));
				ConstraintSolver.TryAddConstraint(BottomConstraint);
				LastY2 = Y2;
			}

			foreach (var constraint in QueuedConstraintsToAdd.OrderByDescending(c => c.Priority))
			{
				try
				{
					ConstraintSolver.AddConstraint(constraint.CassowaryConstraint);
				}
				catch
				{
					UnsatifiableConstraintEvent?.Invoke(this, constraint);
				}
			}
			QueuedConstraintsToAdd.Clear();

			ConstraintSolver.Solve();
		}

		public void AddVariables(params ClVariable[] variables)
		{
			foreach (var variable in variables)
				ConstraintSolver.AddVar(variable);
		}

		public void RemoveVariables(params ClVariable[] variables)
		{
			foreach (var variable in variables)
				ConstraintSolver.NoteRemovedVariable(variable, variable);
		}

		internal void FireRenderedViewEvent(UIView view, RenderContext context)
		{
			if (view.Root != this)
				throw new ArgumentException($"View {view} is not in the hierarchy of root view {this}.");
			RenderedViewEvent?.Invoke(this, view, context);
		}

		protected override void OnLayoutIfNeeded()
		{
			if (Root is null)
				SolveLayout();
			base.OnLayoutIfNeeded();
		}

		internal void QueueAddConstraint(IUILayoutConstraint constraint)
		{
			foreach (var anchorView in constraint.Anchors.Select(a => a.Owner.ConstrainableOwnerView).Distinct())
				anchorView.AddConstraint(constraint);
			QueuedConstraintsToAdd.Add(constraint);
		}

		internal void QueueRemoveConstraint(IUILayoutConstraint constraint)
		{
			foreach (var anchorView in constraint.Anchors.Select(a => a.Owner.ConstrainableOwnerView).Distinct())
				anchorView.RemoveConstraint(constraint);
			QueuedConstraintsToRemove.Add(constraint);
		}
	}
}