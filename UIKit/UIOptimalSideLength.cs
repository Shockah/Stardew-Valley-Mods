namespace Shockah.UIKit
{
	public abstract class UIOptimalSideLength
	{
		public static CompressedType Compressed { get; } = new();
		public static ExpandedType Expanded { get; } = new();

		internal UIOptimalSideLength()
		{
		}

		public static LengthType OfLength(float length)
			=> new(length);

		public class LengthType: UIOptimalSideLength
		{
			public float Value { get; private set; }

			internal LengthType(float value) : base()
			{
				this.Value = value;
			}
		}

		public class CompressedType: UIOptimalSideLength
		{
			internal CompressedType() : base()
			{
			}
		}

		public class ExpandedType: UIOptimalSideLength
		{
			internal ExpandedType() : base()
			{
			}
		}
	}
}
