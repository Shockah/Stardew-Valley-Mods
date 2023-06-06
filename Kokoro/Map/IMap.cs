namespace Shockah.Kokoro.Map;

public interface IMap<TTile>
{
	TTile this[IntPoint point] { get; }

	public interface WithKnownSize : IMap<TTile>
	{
		IntRectangle Bounds { get; }
	}

	public interface Writable : IMap<TTile>
	{
		new TTile this[IntPoint point] { get; set; }
	}
}