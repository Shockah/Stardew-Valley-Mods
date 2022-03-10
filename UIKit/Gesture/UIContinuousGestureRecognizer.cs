namespace Shockah.UIKit.Gesture
{
	public abstract class UIContinuousGestureRecognizer: UIGestureRecognizer
	{
		public virtual void OnTouchUsedByRecognizer(UITouch touch)
		{
		}
	}
}
