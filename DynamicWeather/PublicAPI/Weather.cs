namespace Shockah.DynamicWeather
{
	public record Weather(
		float RainSeverity,
		float LightningSeverity,
		float WindSeverity
	)
	{
		public Weather Mix(Weather other, float coefficient)
			=> new(
				RainSeverity + (other.RainSeverity - RainSeverity) * coefficient,
				LightningSeverity + (other.LightningSeverity - LightningSeverity) * coefficient,
				WindSeverity + (other.WindSeverity - WindSeverity) * coefficient
			);
	}
}