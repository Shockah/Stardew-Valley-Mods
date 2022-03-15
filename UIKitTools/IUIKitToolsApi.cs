using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Shockah.UIKit.Tools
{
	public interface IUIKitToolsApi
	{
		#region Identifiers
		string? GetViewIdentifier(UIView view);
		void SetViewIdentifier(UIView view, string? identifier);
		#endregion

		#region Debug Frames
		IReadOnlyList<Color> GetDebugFrameColors();
		void SetDebugFrameColors(IReadOnlyList<Color> colors);
		Color? GetDebugFrameColorOverride(UIView view);
		void SetDebugFrameColorOverride(Color? color, UIView view);

		bool IsDebugFrameVisible(UIView view);
		void SetDebugFrameVisible(bool visible, UIView view);
		void ToggleDebugFrameVisibility(UIView view);
		void ShowDebugFrame(UIView view);
		void HideDebugFrame(UIView view);

		bool AreHierarchyDebugFramesVisible(UIView hierarchy);
		void SetHierarchyDebugFramesVisible(bool visible, UIView hierarchy);
		void ToggleHierarchyDebugFramesVisibility(UIView hierarchy);
		void ShowHierarchyDebugFrames(UIView hierarchy);
		void HideHierarchyDebugFrames(UIView hierarchy);
		#endregion
	}
}
