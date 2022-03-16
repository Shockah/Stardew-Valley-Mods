using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.UIKit.Tools
{
	public interface IUIKitToolsApi
	{
		#region Identifiers
		string? GetViewIdentifier(UIView view);
		void SetViewIdentifier(UIView view, string? identifier);
		void RegisterView(UIView view);
		#endregion

		#region Debug Frames
		IReadOnlyList<Color> GetDebugFrameColors();
		void SetDebugFrameColors(IReadOnlyList<Color> colors);
		Color? GetDebugFrameColorOverride(UIView view);
		void SetDebugFrameColorOverride(Color? color, UIView view);

		bool GetDebugFrameVisible(UIView view);
		void SetDebugFrameVisible(bool visible, UIView view);

		bool GetHierarchyDebugFramesVisible(UIView hierarchy);
		void SetHierarchyDebugFramesVisible(bool visible, UIView hierarchy);
		#endregion
	}
}
