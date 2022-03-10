using Cassowary;
using Shockah.UIKit.Gesture;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit
{
	public class UIRootView: UIView
	{
		public IGestureRecognizerManager GestureRecognizerManager { get; private set; }

		public event UnsatisfiableConstraintEvent? UnsatifiableConstraintEvent;

		private ClSimplexSolver ConstraintSolver { get; set; } = new() { AutoSolve = false };

		private float LastX1 { get; set; } = 0f;
		private float LastY1 { get; set; } = 0f;
		private float LastX2 { get; set; } = 0f;
		private float LastY2 { get; set; } = 0f;
		private ClConstraint? LeftConstraint;
		private ClConstraint? RightConstraint;
		private ClConstraint? TopConstraint;
		private ClConstraint? BottomConstraint;

		private readonly ISet<UILayoutConstraint> QueuedConstraintsToAdd = new HashSet<UILayoutConstraint>();
		private readonly ISet<UILayoutConstraint> QueuedConstraintsToRemove = new HashSet<UILayoutConstraint>();

		public UIRootView(IGestureRecognizerManager gestureRecognizerManager)
		{
			this.GestureRecognizerManager = gestureRecognizerManager;
		}

		public void SolveLayout()
		{
			foreach (var constraint in QueuedConstraintsToRemove)
				ConstraintSolver.RemoveConstraint(constraint.CassowaryConstraint.Value);
			QueuedConstraintsToRemove.Clear();

			if (X1 != LastX1 || LeftConstraint is null)
			{
				if (LeftConstraint is not null)
					ConstraintSolver.RemoveConstraint(LeftConstraint);
				LeftConstraint = new ClLinearEquation(new ClLinearExpression(LeftVariable.Value), new ClLinearExpression(X1));
				ConstraintSolver.TryAddConstraint(LeftConstraint);
				LastX1 = X1;
			}
			if (Y1 != LastY1 || TopConstraint is null)
			{
				if (TopConstraint is not null)
					ConstraintSolver.RemoveConstraint(TopConstraint);
				TopConstraint = new ClLinearEquation(new ClLinearExpression(TopVariable.Value), new ClLinearExpression(Y1));
				ConstraintSolver.TryAddConstraint(TopConstraint);
				LastY1 = Y1;
			}
			if (X2 != LastX2 || RightConstraint is null)
			{
				if (RightConstraint is not null)
					ConstraintSolver.RemoveConstraint(RightConstraint);
				RightConstraint = new ClLinearEquation(new ClLinearExpression(RightVariable.Value), new ClLinearExpression(X2));
				ConstraintSolver.TryAddConstraint(RightConstraint);
				LastX2 = X2;
			}
			if (Y2 != LastY2 || BottomConstraint is null)
			{
				if (BottomConstraint is not null)
					ConstraintSolver.RemoveConstraint(BottomConstraint);
				BottomConstraint = new ClLinearEquation(new ClLinearExpression(BottomVariable.Value), new ClLinearExpression(Y2));
				ConstraintSolver.TryAddConstraint(BottomConstraint);
				LastY2 = Y2;
			}

			foreach (var constraint in QueuedConstraintsToAdd.OrderByDescending(c => c.Priority))
			{
				try
				{
					ConstraintSolver.AddConstraint(constraint.CassowaryConstraint.Value);
				}
				catch
				{
					constraint.IsUnsatisfied = true;
					UnsatifiableConstraintEvent?.Invoke(this, constraint);
				}
			}
			QueuedConstraintsToAdd.Clear();

			ConstraintSolver.Solve();
		}

		internal void AddViewVariables(UIView view)
		{
			ConstraintSolver.AddVar(view.LeftVariable.Value);
			ConstraintSolver.AddVar(view.RightVariable.Value);
			ConstraintSolver.AddVar(view.TopVariable.Value);
			ConstraintSolver.AddVar(view.BottomVariable.Value);
		}

		internal void RemoveViewVariables(UIView view)
		{
			ConstraintSolver.NoteRemovedVariable(view.LeftVariable.Value, view.LeftVariable.Value);
			ConstraintSolver.NoteRemovedVariable(view.RightVariable.Value, view.RightVariable.Value);
			ConstraintSolver.NoteRemovedVariable(view.TopVariable.Value, view.TopVariable.Value);
			ConstraintSolver.NoteRemovedVariable(view.BottomVariable.Value, view.BottomVariable.Value);
		}

		internal override void OnInternalLayoutIfNeeded()
		{
			SolveLayout();

			OnLayoutIfNeeded();
			foreach (var subview in Subviews)
				subview.LayoutIfNeeded();
		}

		internal void QueueAddConstraint(UILayoutConstraint constraint)
		{
			QueuedConstraintsToAdd.Add(constraint);
		}

		internal void QueueRemoveConstraint(UILayoutConstraint constraint)
		{
			QueuedConstraintsToRemove.Add(constraint);
		}
	}
}