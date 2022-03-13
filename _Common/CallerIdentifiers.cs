using System.Linq;

namespace Shockah.CommonModCode
{
	public static class CallerIdentifiers
	{
		public static string? GetCallerIdentifier(string? callerFilePath, string? callerMemberName, int? callerLineNumber, string? customIdentifier = null)
		{
			if (callerFilePath is null || callerMemberName is null || callerLineNumber is null)
				return null;
			var lastFilePathComponent = callerFilePath.Split(new char[] { '/', '\\' }).Last();
			return $"{lastFilePathComponent}:{callerMemberName}:{callerLineNumber}{(customIdentifier is null ? "" : $":{customIdentifier}")}";
		}
	}
}