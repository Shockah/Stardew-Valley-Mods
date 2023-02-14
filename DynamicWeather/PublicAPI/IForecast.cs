namespace Shockah.DynamicWeather
{
	public interface IForecast
	{
		int StartTime { get; }
		int EndTime { get; }

		Weather GetWeather(int time);
	}
}