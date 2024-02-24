using Newtonsoft.Json;
using StardewModdingAPI;
using System.Collections.Generic;

namespace Shockah.Kokoro;

internal sealed class SaveFilesModel
{
	[JsonProperty] public ISemanticVersion Version { get; internal set; } = ModEntry.Instance.ModManifest.Version;
	[JsonProperty] public IList<SaveFileEntry> Entries { get; internal set; } = new List<SaveFileEntry>();

	public sealed record SaveFileEntry(
		long PlayerID,
		SaveFileDescriptor Descriptor
	);
}