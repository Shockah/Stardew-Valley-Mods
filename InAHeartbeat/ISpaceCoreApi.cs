using System;

namespace Shockah.InAHeartbeat;

public interface ISpaceCoreApi
{
	public event EventHandler<Action<string, Action>> AdvancedInteractionStarted;
}