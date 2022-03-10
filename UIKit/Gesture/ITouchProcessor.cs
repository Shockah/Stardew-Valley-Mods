using Shockah.UIKit.Geometry;
using System;

namespace Shockah.UIKit.Gesture
{
	public interface ITouchProcessor<TPointerID, TPointerState>
		where TPointerID : IEquatable<TPointerID>
	{
		void OnTouchDown(TPointerID pointerID, UIVector2 point, TPointerState pointerState);
		void OnTouchChanged(TPointerID pointerID, UIVector2 point, TPointerState pointerState);
		void OnTouchUp(TPointerID pointerID);
	}
}
