using System;

namespace Shockah.ProjectFluent
{
	internal class MappingFluent<Key>: IFluent<Key>
	{
		private IFluent<string> Wrapped { get; set; }
		private Func<Key, string> Mapper { get; set; }

		public MappingFluent(IFluent<string> wrapped, Func<Key, string> mapper)
		{
			this.Wrapped = wrapped;
			this.Mapper = mapper;
		}

		public bool ContainsKey(Key key)
			=> Wrapped.ContainsKey(Mapper(key));

		public string Get(Key key, object? tokens)
			=> Wrapped.Get(Mapper(key), tokens);
	}
}