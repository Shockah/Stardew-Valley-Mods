using Cassowary;
using System;

namespace Shockah.UIKit
{
	public class UIAnchor
	{
		public IConstrainable Owner { get; private set; }
		internal ClLinearExpression Expression { get; private set; }
		private readonly string AnchorName;

		public UIAnchor(IConstrainable owner, ClLinearExpression expression, string anchorName)
		{
			this.Owner = owner;
			this.Expression = expression;
			this.AnchorName = anchorName;
		}

		public override string ToString()
			=> $"{Owner}.{AnchorName}";
	}

	public class UITypedAnchor<ConstrainableType>: UIAnchor where ConstrainableType : IConstrainable
	{
		internal Func<ConstrainableType, UITypedAnchor<ConstrainableType>> AnchorFunction { get; private set; }

		public UITypedAnchor(
			ConstrainableType owner,
			ClLinearExpression expression,
			string anchorName,
			Func<ConstrainableType, UITypedAnchor<ConstrainableType>> anchorFunction
		) : base(owner, expression, anchorName)
		{
			this.AnchorFunction = anchorFunction;
		}
	}

	public class UITypedAnchorWithOpposite<ConstrainableType>: UITypedAnchor<ConstrainableType> where ConstrainableType : IConstrainable
	{
		private Func<ConstrainableType, UITypedAnchorWithOpposite<ConstrainableType>> OppositeAnchorFunction { get; set; }

		public UITypedAnchorWithOpposite(
			ConstrainableType owner,
			ClLinearExpression expression,
			string anchorName,
			Func<ConstrainableType, UITypedAnchor<ConstrainableType>> anchorFunction,
			Func<ConstrainableType, UITypedAnchorWithOpposite<ConstrainableType>> oppositeFunction
		) : base(owner, expression, anchorName, anchorFunction)
		{
			this.OppositeAnchorFunction = oppositeFunction;
		}

		public UITypedAnchorWithOpposite<ConstrainableType> GetOpposite(ConstrainableType constrainable)
			=> OppositeAnchorFunction(constrainable);
	}
}