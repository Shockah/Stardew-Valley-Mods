using System;
using System.Reflection;

namespace Shockah.PredictableRetainingSoil
{
	public interface ISpaceCoreApi
	{
		void RegisterCustomProperty(Type declaringType, string name, Type propType, MethodInfo getter, MethodInfo setter);
	}
}