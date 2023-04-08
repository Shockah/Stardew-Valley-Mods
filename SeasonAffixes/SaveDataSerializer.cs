using StardewModdingAPI;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Shockah.SeasonAffixes
{
	internal interface ISaveDataSerializer
	{
		SerializedSaveData Serialize(SaveData data);
		SaveData Deserialize(SerializedSaveData data);
	}

	internal sealed class SaveDataSerializer : ISaveDataSerializer
	{
		public SerializedSaveData Serialize(SaveData data)
		{
			return new(
				data.ActiveAffixes.Select(a => a.UniqueID).ToList(),
				data.AffixChoiceHistory.Select(step => step.Select(a => a.UniqueID).ToList()).ToList(),
				data.AffixSetChoiceHistory.Select(step => step.Select(set => set.Select(a => a.UniqueID).ToList()).ToList()).ToList()
			);
		}

		public SaveData Deserialize(SerializedSaveData data)
		{
			ISeasonAffix? GetOrLog(string id, string context, LogLevel level)
			{
				var affix = SeasonAffixes.Instance.GetAffix(id);
				if (affix is null)
					SeasonAffixes.Instance.Monitor.Log($"Tried to deserialize affix `{id}` for {context}, but no such affix is registered. Did you remove a mod?", level);
				return affix;
			}

			bool TryGetOrLog(string id, string context, LogLevel level, [NotNullWhen(true)] out ISeasonAffix? affix)
			{
				var value = GetOrLog(id, context, level);
				affix = value;
				return value is null;
			}

			SaveData result = new();
			foreach (var id in data.ActiveAffixes)
				if (TryGetOrLog(id, "active affixes", LogLevel.Warn, out var affix))
					result.ActiveAffixes.Add(affix);
			foreach (var step in data.AffixChoiceHistory)
				result.AffixChoiceHistory.Add(step.Select(id => GetOrLog(id, "affix choice history", LogLevel.Info)).Where(a => a is not null).Select(a => a!).ToHashSet());
			foreach (var step in data.AffixSetChoiceHistory)
				result.AffixSetChoiceHistory.Add(step.Select(set => (ISet<ISeasonAffix>)set.Select(id => GetOrLog(id, "affix set choice history", LogLevel.Info)).Where(a => a is not null).Select(a => a!).ToHashSet()).ToHashSet());
			return result;
		}
	}
}