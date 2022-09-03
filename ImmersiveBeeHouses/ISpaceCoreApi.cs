using System;
using System.Reflection;

namespace Shockah.ImmersiveBeeHouses
{
	public interface ISpaceCoreApi
	{
		void RegisterCustomProperty(Type declaringType, string name, Type propType, MethodInfo getter, MethodInfo setter);
	}
}