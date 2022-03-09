namespace Shockah.UIKit
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested interfaces")]
	public interface IConstrainable
	{
		public UIView ConstrainableOwnerView { get; }

		public interface Horizontal: IConstrainable
		{
			public UITypedAnchorWithOpposite<Horizontal> LeftAnchor { get; }
			public UITypedAnchorWithOpposite<Horizontal> RightAnchor { get; }
			public UITypedAnchor<Horizontal> WidthAnchor { get; }
			public UITypedAnchor<Horizontal> CenterXAnchor { get; }
		}

		public interface Vertical: IConstrainable
		{
			public UITypedAnchorWithOpposite<Vertical> TopAnchor { get; }
			public UITypedAnchorWithOpposite<Vertical> BottomAnchor { get; }
			public UITypedAnchor<Vertical> HeightAnchor { get; }
			public UITypedAnchor<Vertical> CenterYAnchor { get; }
		}
	}
}
