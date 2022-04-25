namespace Shockah.UIKit.Solver
{
	public struct LinearSolverResult
	{
	}

	public interface ILinearSolver
	{
		void AddEquation(LinearSolverEquation equation);
		void RemoveEquation(LinearSolverEquation equation);

		void Reset();

		LinearSolverResult Solve();
	}
}
