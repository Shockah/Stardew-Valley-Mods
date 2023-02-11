using Newtonsoft.Json;
using Shockah.CommonModCode.UI;

namespace Shockah.XPDisplay
{
	public sealed class ModConfig
	{
		[JsonProperty] public Orientation SmallBarOrientation { get; internal set; } = Orientation.Vertical;
		[JsonProperty] public Orientation BigBarOrientation { get; internal set; } = Orientation.Horizontal;
		[JsonProperty] public float Alpha { get; internal set; } = 0.6f;
		[JsonProperty] public string? LevelUpSoundName { get; internal set; } = "crystal";
		[JsonProperty] public ToolbarSkillBarConfig ToolbarSkillBar { get; internal set; } = new();
	}

	public sealed class ToolbarSkillBarConfig
	{
		[JsonProperty] public bool IsEnabled { get; internal set; } = true;
		[JsonProperty] public float SpacingFromToolbar { get; internal set; } = 24f;
		[JsonProperty] public bool AlwaysShowCurrentTool { get; internal set; } = false;
		[JsonProperty] public float ToolSwitchDurationInSeconds { get; internal set; } = 3f;
		[JsonProperty] public float XPChangedDurationInSeconds { get; internal set; } = 3f;
		[JsonProperty] public float LevelChangedDurationInSeconds { get; internal set; } = 5f;
		[JsonProperty] public bool ShowIcon { get; internal set; } = true;
	}
}