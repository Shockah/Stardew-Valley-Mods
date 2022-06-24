namespace Shockah.ProjectFluent
{
	public interface IFluentFunctionValue
	{
		object /* IFluentType */ AsFluentValue();

		string AsString();
		int? AsIntOrNull();
		long? AsLongOrNull();
		float? AsFloatOrNull();
		double? AsDoubleOrNull();
	}
}