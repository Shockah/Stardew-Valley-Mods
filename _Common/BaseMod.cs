using Newtonsoft.Json;
using Shockah.CommonModCode.SMAPI;
using StardewModdingAPI;

namespace Shockah.CommonModCode
{
	public abstract class BaseMod : Mod
	{
		public override object? GetApi()
			=> this;
	}

	public abstract class BaseMod<TConfig> : BaseMod
		where TConfig : class, new()
	{
		public TConfig Config
		{
			get => ConfigStorage!;
			set
			{
				ConfigStorage = value;
				LogConfig();
			}
		}

		private TConfig? ConfigStorage;
		private JsonSerializerSettings? JsonSerializerSettings;

		public override void Entry(IModHelper helper)
		{
			Config = helper.ReadConfig<TConfig>();
		}

		protected internal void LogConfig()
		{
			if (JsonSerializerSettings is null)
				JsonSerializerSettings = JsonSerializerExt.GetSMAPISerializerSettings(Helper.Data);

			var json = JsonConvert.SerializeObject(Config, JsonSerializerSettings);
			Monitor.Log($"Current config:\n{json}", LogLevel.Trace);
		}
	}
}