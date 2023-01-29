namespace Shockah.CommonModCode
{
	public sealed class StructRef<T> where T : struct
	{
		public T Value { get; set; }

		public StructRef(T initialValue)
		{
			this.Value = initialValue;
		}

		public static implicit operator StructRef<T>(T value)
			=> new(value);

		public static implicit operator T(StructRef<T> value)
			=> value.Value;
	}
}