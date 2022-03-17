namespace Shockah.UIKit
{
	public interface IConstrainable
	{
		public UIView ConstrainableOwnerView { get; }

		public interface Horizontal: IConstrainable
		{
			public IUIAnchor.Typed<Horizontal>.Positional.WithOpposite LeftAnchor { get; }
			public IUIAnchor.Typed<Horizontal>.Positional.WithOpposite RightAnchor { get; }
			public IUIAnchor.Typed<Horizontal>.Length WidthAnchor { get; }
			public IUIAnchor.Typed<Horizontal>.Positional CenterXAnchor { get; }
		}

		public interface Vertical: IConstrainable
		{
			public IUIAnchor.Typed<Vertical>.Positional.WithOpposite TopAnchor { get; }
			public IUIAnchor.Typed<Vertical>.Positional.WithOpposite BottomAnchor { get; }
			public IUIAnchor.Typed<Vertical>.Length HeightAnchor { get; }
			public IUIAnchor.Typed<Vertical>.Positional CenterYAnchor { get; }
		}
	}
}
