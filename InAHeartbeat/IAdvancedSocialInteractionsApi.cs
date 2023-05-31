using System;

namespace Shockah.InAHeartbeat
{
	public interface IAdvancedSocialInteractionsApi
	{
		public event EventHandler<Action<string, Action>> AdvancedInteractionStarted;
	}
}