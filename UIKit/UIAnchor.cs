using Cassowary;
using System;
using System.Diagnostics.Contracts;

namespace Shockah.UIKit
{
	public interface IUIAnchor
	{
		IConstrainable Owner { get; }

		internal interface Internal: IUIAnchor
		{
			ClLinearExpression Expression { get; }
		}
	}

	public class UIAnchor: IUIAnchor.Internal
	{
		public IConstrainable Owner { get; private set; }
		ClLinearExpression IUIAnchor.Internal.Expression => _expression;
		private readonly string AnchorName;

		private readonly ClLinearExpression _expression;

		public UIAnchor(IConstrainable owner, ClLinearExpression expression, string anchorName)
		{
			this.Owner = owner;
			this._expression = expression;
			this.AnchorName = anchorName;
		}

		public override string ToString()
			=> $"{Owner}.{AnchorName}";
	}

	public interface IUITypedAnchor<in ConstrainableType>: IUIAnchor where ConstrainableType : IConstrainable
	{
		[Pure]
		IUITypedAnchor<ConstrainableType> GetSameAnchorInConstrainable(ConstrainableType constrainable);
	}

	public class UITypedAnchor<ConstrainableType>: UIAnchor, IUITypedAnchor<ConstrainableType> where ConstrainableType : IConstrainable
	{
		private Func<ConstrainableType, IUITypedAnchor<ConstrainableType>> AnchorFunction { get; }

		public UITypedAnchor(
			ConstrainableType owner,
			ClLinearExpression expression,
			string anchorName,
			Func<ConstrainableType, IUITypedAnchor<ConstrainableType>> anchorFunction
		) : base(owner, expression, anchorName)
		{
			this.AnchorFunction = anchorFunction;
		}

		[Pure]
		public IUITypedAnchor<ConstrainableType> GetSameAnchorInConstrainable(ConstrainableType constrainable)
			=> AnchorFunction(constrainable);
	}

	public interface IUITypedAnchorWithOpposite<in ConstrainableType>: IUITypedAnchor<ConstrainableType> where ConstrainableType : IConstrainable
	{
		[Pure]
		IUITypedAnchor<ConstrainableType> GetOppositeAnchorInConstrainable(ConstrainableType constrainable);
	}

	public class UITypedAnchorWithOpposite<ConstrainableType>: UITypedAnchor<ConstrainableType>, IUITypedAnchorWithOpposite<ConstrainableType> where ConstrainableType : IConstrainable
	{
		private Func<ConstrainableType, IUITypedAnchorWithOpposite<ConstrainableType>> OppositeAnchorFunction { get; }

		public UITypedAnchorWithOpposite(
			ConstrainableType owner,
			ClLinearExpression expression,
			string anchorName,
			Func<ConstrainableType, IUITypedAnchor<ConstrainableType>> anchorFunction,
			Func<ConstrainableType, IUITypedAnchorWithOpposite<ConstrainableType>> oppositeFunction
		) : base(owner, expression, anchorName, anchorFunction)
		{
			this.OppositeAnchorFunction = oppositeFunction;
		}

		[Pure]
		public IUITypedAnchor<ConstrainableType> GetOppositeAnchorInConstrainable(ConstrainableType constrainable)
			=> OppositeAnchorFunction(constrainable);
	}
}