namespace Shockah.ProjectFluent
{
	internal interface IFluentValueFactory
	{
		IFluentFunctionValue CreateStringValue(string value);
		IFluentFunctionValue CreateIntValue(int value);
		IFluentFunctionValue CreateLongValue(long value);
		IFluentFunctionValue CreateFloatValue(float value);
		IFluentFunctionValue CreateDoubleValue(double value);
	}
}