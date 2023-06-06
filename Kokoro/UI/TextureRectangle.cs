using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Shockah.Kokoro.UI;

public record TextureRectangle(
	Texture2D Texture,
	Rectangle Rectangle
);