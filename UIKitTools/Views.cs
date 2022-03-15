namespace Shockah.UIKit.Tools
{
	public static class Views
	{
		public static string? GetIdentifier(this UIView self)
			=> UIKit.Instance.GetViewIdentifier(self);

		public static void SetIdentifier(this UIView self, string? identifier)
			=> UIKit.Instance.SetViewIdentifier(self, identifier);
	}
}