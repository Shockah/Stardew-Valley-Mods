namespace Shockah.UIKit.Tools
{
	public static class Views
	{
		public static string? GetIdentifier(this UIView self)
			=> UIKitTools.Instance.GetViewIdentifier(self);

		public static void SetIdentifier(this UIView self, string? identifier)
			=> UIKitTools.Instance.SetViewIdentifier(self, identifier);
	}
}