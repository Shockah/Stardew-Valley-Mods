using System;

namespace Shockah.ProjectFluent
{
	internal class MappingFluent<Input, Output>: IFluent<Input>
	{
		private IFluent<Output> Wrapped { get; set; }
		private Func<Input, Output> Mapper { get; set; }

		public MappingFluent(IFluent<Output> wrapped, Func<Input, Output> mapper)
		{
			this.Wrapped = wrapped;
			this.Mapper = mapper;
		}

		public bool ContainsKey(Input key)
			=> Wrapped.ContainsKey(Mapper(key));

		public string Get(Input key, object? tokens)
			=> Wrapped.Get(Mapper(key), tokens);
	}
}