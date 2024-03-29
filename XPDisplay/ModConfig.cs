﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shockah.Kokoro;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.XPDisplay
{
	public sealed class ModConfig : IVersioned.Modifiable
	{
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ISemanticVersion? Version { get; set; }
		[JsonProperty] public Orientation SmallBarOrientation { get; internal set; } = Orientation.Vertical;
		[JsonProperty] public Orientation BigBarOrientation { get; internal set; } = Orientation.Horizontal;
		[JsonProperty] public float Alpha { get; internal set; } = 0.6f;
		[JsonProperty] public string? LevelUpSoundName { get; internal set; } = "crystal";
		[JsonProperty] public ToolbarSkillBarConfig ToolbarSkillBar { get; internal set; } = new();
		[JsonExtensionData] internal IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
	}

	public sealed class ToolbarSkillBarConfig
	{
		[JsonProperty] public bool IsEnabled { get; internal set; } = true;
		[JsonProperty] public float Scale { get; internal set; } = 4f;
		[JsonProperty] public float SpacingFromToolbar { get; internal set; } = 24f;
		[JsonProperty] public bool ShowIcon { get; internal set; } = true;
		[JsonProperty] public bool ShowLevelNumber { get; internal set; } = true;
		[JsonProperty] public bool ExcludeSkillsAtMaxLevel { get; internal set; } = true;
		[JsonProperty] public bool AlwaysShowCurrentTool { get; internal set; } = false;
		[JsonProperty] public float ToolSwitchDurationInSeconds { get; internal set; } = 3f;
		[JsonProperty] public float ToolUseDurationInSeconds { get; internal set; } = 3f;
		[JsonProperty] public float XPChangedDurationInSeconds { get; internal set; } = 3f;
		[JsonProperty] public float LevelChangedDurationInSeconds { get; internal set; } = 5f;
		[JsonProperty] public ISet<string> SkillsToExcludeOnXPGain { get; internal set; } = new HashSet<string> { "Achtuur.Travelling" };
		[JsonProperty] public ISet<string> SkillsToExcludeOnLevelUp { get; internal set; } = new HashSet<string>();
		[JsonProperty] public ISet<string> SkillsToExcludeOnToolHeld { get; internal set; } = new HashSet<string>();
		[JsonProperty] public ISet<string> SkillsToExcludeOnToolSwitch { get; internal set; } = new HashSet<string>();
		[JsonProperty] public ISet<string> SkillsToExcludeOnToolUse { get; internal set; } = new HashSet<string>();
	}
}