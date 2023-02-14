namespace Shockah.DynamicWeather
{
	public record StaticForecast(
		int StartTime,
		int EndTime,
		Weather Weather
	) : IForecast
	{
		public Weather GetWeather(int time)
			=> Weather;
	}
}