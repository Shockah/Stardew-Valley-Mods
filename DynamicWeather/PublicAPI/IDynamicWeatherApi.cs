using System.Collections.Generic;

namespace Shockah.DynamicWeather
{
	public interface IDynamicWeatherApi
	{
		Weather CurrentWeather { get; }
		IForecast CurrentForecast { get; }
		IReadOnlyList<StaticForecast> CurrentDayStaticForecastList { get; }
		IReadOnlyList<IForecast> CurrentDayForecastList { get; }
	}
}