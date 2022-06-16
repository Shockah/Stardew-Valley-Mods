namespace Shockah.ProjectFluent
{
	internal class ModConfig
	{
		public ContentPatcherPatchingMode ContentPatcherPatchingMode { get; set; } = ContentPatcherPatchingMode.PatchFluentToken;
		public string CurrentLocaleOverride { get; set; } = "";
	}
}