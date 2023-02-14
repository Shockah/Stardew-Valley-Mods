using StardewValley;

namespace Shockah.DynamicWeather
{
	public record TransitionForecast(
		IForecast StartForecast,
		IForecast EndForecast
	) : IForecast
	{
		public int StartTime
			=> StartForecast.EndTime;

		public int EndTime
			=> EndForecast.StartTime;

		public Weather GetWeather(int time)
		{
			int minutesBetweenStartAndEnd = Utility.CalculateMinutesBetweenTimes(StartTime, EndTime);
			int minutesBetweenStartAndTime = Utility.CalculateMinutesBetweenTimes(StartTime, time);
			float coefficient = 1f * minutesBetweenStartAndTime / minutesBetweenStartAndEnd;
			return StartForecast.GetWeather(StartTime).Mix(EndForecast.GetWeather(EndTime), coefficient);
		}
	}
}