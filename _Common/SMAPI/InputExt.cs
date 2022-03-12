using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;

namespace Shockah.CommonModCode.SMAPI
{
	public static class InputExt
	{
		public enum ButtonType { Mouse, Keyboard, Gamepad }

		public static ButtonType? GetButtonType(this SButton button)
		{
			switch (button)
			{
				case SButton.MouseLeft:
				case SButton.MouseRight:
				case SButton.MouseMiddle:
				case SButton.MouseX1:
				case SButton.MouseX2:
					return ButtonType.Mouse;
				default:
					if (button.TryGetKeyboard(out _))
						return ButtonType.Keyboard;
					if (button.TryGetController(out _))
						return ButtonType.Gamepad;
					return null;
			}
		}

		public static bool IsPressed(this SButton button, KeyboardState? keyboardState = null, MouseState? mouseState = null, GamePadState? gamePadState = null)
		{
			keyboardState ??= Game1.GetKeyboardState();
			mouseState ??= Game1.input.GetMouseState();
			gamePadState ??= Game1.input.GetGamePadState();
			switch (button)
			{
				case SButton.MouseLeft:
					return mouseState.Value.LeftButton == ButtonState.Pressed;
				case SButton.MouseRight:
					return mouseState.Value.RightButton == ButtonState.Pressed;
				case SButton.MouseMiddle:
					return mouseState.Value.MiddleButton == ButtonState.Pressed;
				case SButton.MouseX1:
					return mouseState.Value.XButton1 == ButtonState.Pressed;
				case SButton.MouseX2:
					return mouseState.Value.XButton2 == ButtonState.Pressed;
				default:
					if (button.TryGetKeyboard(out var key))
						return keyboardState.Value.IsKeyDown(key);
					if (button.TryGetController(out var controllerButton))
						return gamePadState.Value.IsButtonDown(controllerButton);
					return false;
			}
		}
	}
}
