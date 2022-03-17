using Cassowary;
using System;
using System.Diagnostics.Contracts;

namespace Shockah.UIKit
{
	public interface IUIAnchor
	{
		IConstrainable Owner { get; }
		ClLinearExpression Expression { get; }

		[Pure]
		bool IsCompatibleWithAnchor(IUIAnchor other);

		public interface Typed<in ConstrainableType> : IUIAnchor where ConstrainableType : IConstrainable
		{
			[Pure]
			Typed<ConstrainableType> GetSameAnchorInConstrainable(ConstrainableType constrainable);

			public interface Positional: Typed<ConstrainableType>
			{
				[Pure]
				bool IUIAnchor.IsCompatibleWithAnchor(IUIAnchor other)
					=> other is Positional;

				[Pure]
				new Positional GetSameAnchorInConstrainable(ConstrainableType constrainable);

				public interface WithOpposite: Positional
				{
					[Pure]
					new WithOpposite GetSameAnchorInConstrainable(ConstrainableType constrainable);

					[Pure]
					WithOpposite GetOppositeAnchorInConstrainable(ConstrainableType constrainable);
				}
			}

			public interface Length: Typed<ConstrainableType>
			{
				[Pure]
				bool IUIAnchor.IsCompatibleWithAnchor(IUIAnchor other)
					=> other is Typed<IConstrainable.Horizontal>.Length or Typed<IConstrainable.Vertical>.Length;

				[Pure]
				new Length GetSameAnchorInConstrainable(ConstrainableType constrainable);
			}
		}
	}

	public abstract class UIAnchor: IUIAnchor
	{
		public IConstrainable Owner { get; private set; }
		public ClLinearExpression Expression { get; private set; }
		private readonly string AnchorName;

		public UIAnchor(IConstrainable owner, ClLinearExpression expression, string anchorName)
		{
			this.Owner = owner;
			this.Expression = expression;
			this.AnchorName = anchorName;
		}

		public override string ToString()
			=> $"{Owner}.{AnchorName}";

		public abstract bool IsCompatibleWithAnchor(IUIAnchor other);
	}

	public class UIEdgeAnchor<ConstrainableType>: UIAnchor, IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite
		where ConstrainableType : IConstrainable
	{
		private Func<ConstrainableType, IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite> AnchorFunction { get; }
		private Func<ConstrainableType, IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite> OppositeAnchorFunction { get; }

		public UIEdgeAnchor(
			ConstrainableType owner,
			ClLinearExpression expression,
			string anchorName,
			Func<ConstrainableType, IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite> anchorFunction,
			Func<ConstrainableType, IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite> oppositeAnchorFunction
		) : base(owner, expression, anchorName)
		{
			this.AnchorFunction = anchorFunction;
			this.OppositeAnchorFunction = oppositeAnchorFunction;
		}

		[Pure]
		public IUIAnchor.Typed<ConstrainableType> GetSameAnchorInConstrainable(ConstrainableType constrainable)
			=> AnchorFunction(constrainable);

		[Pure]
		IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite.GetSameAnchorInConstrainable(ConstrainableType constrainable)
			=> AnchorFunction(constrainable);

		[Pure]
		IUIAnchor.Typed<ConstrainableType>.Positional IUIAnchor.Typed<ConstrainableType>.Positional.GetSameAnchorInConstrainable(ConstrainableType constrainable)
			=> AnchorFunction(constrainable);

		[Pure]
		public IUIAnchor.Typed<ConstrainableType>.Positional.WithOpposite GetOppositeAnchorInConstrainable(ConstrainableType constrainable)
			=> OppositeAnchorFunction(constrainable);

		public override bool IsCompatibleWithAnchor(IUIAnchor other)
			=> other is IUIAnchor.Typed<ConstrainableType>.Positional;
	}

	public class UILengthAnchor<ConstrainableType>: UIAnchor, IUIAnchor.Typed<ConstrainableType>.Length
		where ConstrainableType : IConstrainable
	{
		private Func<ConstrainableType, IUIAnchor.Typed<ConstrainableType>.Length> AnchorFunction { get; }

		public UILengthAnchor(
			ConstrainableType owner,
			ClLinearExpression expression,
			string anchorName,
			Func<ConstrainableType, IUIAnchor.Typed<ConstrainableType>.Length> anchorFunction
		) : base(owner, expression, anchorName)
		{
			this.AnchorFunction = anchorFunction;
		}

		[Pure]
		public IUIAnchor.Typed<ConstrainableType> GetSameAnchorInConstrainable(ConstrainableType constrainable)
			=> AnchorFunction(constrainable);

		[Pure]
		IUIAnchor.Typed<ConstrainableType>.Length IUIAnchor.Typed<ConstrainableType>.Length.GetSameAnchorInConstrainable(ConstrainableType constrainable)
			=> AnchorFunction(constrainable);

		public override bool IsCompatibleWithAnchor(IUIAnchor other)
			=> other is IUIAnchor.Typed<IConstrainable.Horizontal>.Length or IUIAnchor.Typed<IConstrainable.Vertical>.Length;
	}

	public class UICenterAnchor<ConstrainableType>: UIAnchor, IUIAnchor.Typed<ConstrainableType>.Positional
		where ConstrainableType : IConstrainable
	{
		private Func<ConstrainableType, IUIAnchor.Typed<ConstrainableType>.Positional> AnchorFunction { get; }

		public UICenterAnchor(
			ConstrainableType owner,
			ClLinearExpression expression,
			string anchorName,
			Func<ConstrainableType, IUIAnchor.Typed<ConstrainableType>.Positional> anchorFunction
		) : base(owner, expression, anchorName)
		{
			this.AnchorFunction = anchorFunction;
		}

		[Pure]
		public IUIAnchor.Typed<ConstrainableType> GetSameAnchorInConstrainable(ConstrainableType constrainable)
			=> AnchorFunction(constrainable);

		[Pure]
		IUIAnchor.Typed<ConstrainableType>.Positional IUIAnchor.Typed<ConstrainableType>.Positional.GetSameAnchorInConstrainable(ConstrainableType constrainable)
			=> AnchorFunction(constrainable);

		public override bool IsCompatibleWithAnchor(IUIAnchor other)
			=> other is IUIAnchor.Typed<ConstrainableType>.Positional;
	}
}