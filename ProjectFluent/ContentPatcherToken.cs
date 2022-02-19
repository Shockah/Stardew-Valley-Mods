using StardewModdingAPI;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal class ContentPatcherToken
	{
		private readonly IManifest mod;
		private readonly IDictionary<string, IFluent<string>> fluents = new Dictionary<string, IFluent<string>>();

		private bool isUpdated = false;

		public ContentPatcherToken(IManifest mod)
		{
			this.mod = mod;
		}

		public bool IsReady() => true;

		public bool AllowsInput() => true;

		public bool RequiresInput() => true;

		public bool CanHaveMultipleValues(string input = null) => false;

		public bool UpdateContext()
		{
			var wasUpdated = isUpdated;
			isUpdated = true;
			return !wasUpdated;
		}

		public IEnumerable<string> GetValues(string input)
		{
			var args = ParseArgs(input);
			if (args.Named.TryGetValue("file", out string localizationsName))
				args.Named.Remove("file");
			else
				localizationsName = null;

			yield return ObtainFluent(localizationsName).Get(args.Key, args.Named);
		}

		private Args ParseArgs(string input)
		{
			if (!input.Contains("|"))
				return new Args(input);

			var key = input[0 ..^ input.IndexOf('|')];
			var named = new Dictionary<string, string>();
			var argSplit = input[input.IndexOf('|') ..].Split('|').Select(s => s.Trim());
			foreach (var wholeArg in argSplit)
			{
				var split = wholeArg.Split('=');
				var argName = split[0].Trim();
				var argValue = split[1].Trim();
				named[argName] = argValue;
			}
			return new Args(key, named);
		}

		private IFluent<string> ObtainFluent(string name)
		{
			if (fluents.TryGetValue(name, out IFluent<string> fluent))
				return fluent;
			fluent = ProjectFluent.Instance.Api.GetLocalizationsForCurrentLocale<string>(mod, name);
			fluents[name] = fluent;
			return fluent;
		}

		internal struct Args
		{
			internal string Key;
			internal Dictionary<string, string> Named;

			public Args(string key): this(key, new Dictionary<string, string>()) { }

			public Args(string key, Dictionary<string, string> named)
			{
				this.Key = key;
				this.Named = named;
			}
		}
	}
}