using Cassowary;

namespace Shockah.UIKit
{
	public class UIRoot: UIView
	{
		internal ClSimplexSolver ConstraintSolver { get; private set; } = new() { AutoSolve = false };

		private float LastX1 { get; set; } = 0f;
		private float LastY1 { get; set; } = 0f;
		private float LastX2 { get; set; } = 0f;
		private float LastY2 { get; set; } = 0f;
		private ClConstraint? LeftConstraint;
		private ClConstraint? RightConstraint;
		private ClConstraint? TopConstraint;
		private ClConstraint? BottomConstraint;

		public override void LayoutIfNeeded()
		{
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

			ConstraintSolver.Solve();

			foreach (var subview in Subviews)
				subview.LayoutIfNeeded();
		}
	}
}