namespace Shockah.UIKit
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested interfaces")]
	public interface IConstrainable
	{
		public UIView ConstrainableOwnerView { get; }

		public interface Horizontal: IConstrainable
		{
			public IUITypedAnchorWithOpposite<Horizontal> LeftAnchor { get; }
			public IUITypedAnchorWithOpposite<Horizontal> RightAnchor { get; }
			public IUITypedAnchor<Horizontal> WidthAnchor { get; }
			public IUITypedAnchor<Horizontal> CenterXAnchor { get; }
		}

		public interface Vertical: IConstrainable
		{
			public IUITypedAnchorWithOpposite<Vertical> TopAnchor { get; }
			public IUITypedAnchorWithOpposite<Vertical> BottomAnchor { get; }
			public IUITypedAnchor<Vertical> HeightAnchor { get; }
			public IUITypedAnchor<Vertical> CenterYAnchor { get; }
		}
	}
}
