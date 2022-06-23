namespace Shockah.ProjectFluent
{
	internal interface IFluentValueFactory
	{
		IFluentApi.IFluentFunctionValue CreateStringValue(string value);
		IFluentApi.IFluentFunctionValue CreateIntValue(int value);
		IFluentApi.IFluentFunctionValue CreateLongValue(long value);
		IFluentApi.IFluentFunctionValue CreateFloatValue(float value);
		IFluentApi.IFluentFunctionValue CreateDoubleValue(double value);
	}
}