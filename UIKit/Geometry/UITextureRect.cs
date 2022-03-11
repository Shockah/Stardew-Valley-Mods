using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Shockah.UIKit.Geometry
{
	public readonly struct UITextureRect: IEquatable<UITextureRect>
	{
		public readonly Texture2D Texture { get; }
		public Rectangle SourceRect => _sourceRect ?? new(0, 0, Texture.Width, Texture.Height);

		public readonly Rectangle? _sourceRect { get; }

		public UITextureRect(Texture2D texture, Rectangle? sourceRect = null)
		{
			this.Texture = texture;
			this._sourceRect = sourceRect;
		}

		public bool Equals(UITextureRect other)
			=> Texture == other.Texture && SourceRect == other._sourceRect;

		public override bool Equals(object? obj)
			=> obj is UITextureRect other && Equals(other);

		public override int GetHashCode()
			=> (Texture, _sourceRect).GetHashCode();

		public static bool operator ==(UITextureRect left, UITextureRect right)
			=> left.Equals(right);

		public static bool operator !=(UITextureRect left, UITextureRect right)
			=> !(left == right);
	}
}