using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Talented.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.Talented
{
	public class Talented : BaseMod, ITalentedApi
	{
		internal static Talented Instance = null!;

		private readonly List<ITalentTag> TalentTags = new();
		private readonly List<ITalent> Talents = new();

		private readonly List<ITalent> ActiveTalents = new();
		private readonly Dictionary<ITalentTag, int> EarnedTalentPoints = new();
		private readonly Dictionary<ITalentTag, int> SpentTalentPoints = new();

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.Display.MenuChanged += OnMenuChanged;
		}

		private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
		{
			var harmony = new Harmony(ModManifest.UniqueID);
			GameMenuPatches.Apply(harmony);
			SkillsPagePatches.Apply(harmony);
		}

		private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
		{
			SkillsPagePatches.OnMenuChanged(sender, e);
		}

		#region API

		public ITalentedApi.IRequirementFactories RequirementFactories => throw new NotImplementedException();

		public void RegisterTalentTag(ITalentTag tag)
		{
			if (TalentTags.Any(t => t.UniqueID == tag.UniqueID))
				throw new ArgumentException($"Tried to register a talent tag with ID {tag.UniqueID}, but it's already registered.");
			TalentTags.Add(tag);
		}

		public void UnregisterTalentTag(ITalentTag tag)
		{
			for (int i = TalentTags.Count - 1; i >= 0; i++)
				if (TalentTags[i].UniqueID == tag.UniqueID)
					TalentTags.RemoveAt(i);
		}

		public void RegisterTalent(ITalent talent)
		{
			if (Talents.Any(t => t.UniqueID == talent.UniqueID))
				throw new ArgumentException($"Tried to register a talent with ID {talent.UniqueID}, but it's already registered.");
			Talents.Add(talent);
		}

		public void UnregisterTalent(ITalent talent)
		{
			for (int i = Talents.Count - 1; i >= 0; i++)
				if (Talents[i].UniqueID == talent.UniqueID)
					Talents.RemoveAt(i);
		}

		public IReadOnlyList<ITalentTag> RootTalentTags
			=> TalentTags.Where(t => t.Parent is null).ToList();

		public IReadOnlyList<ITalentTag> AllTalentTags
			=> TalentTags.ToList();

		public IReadOnlyList<ITalentTag> GetChildTalentTags(ITalentTag parent)
			=> TalentTags.Where(t => t.Parent == parent).ToList();

		public IReadOnlyList<ITalent> GetTalents(ITalentTag tag)
			=> Talents.Where(t => t.Matches(tag)).ToList();

		public ITalent? GetTalent(string uniqueID)
			=> Talents.FirstOrDefault(t => t.UniqueID == uniqueID);

		public IReadOnlyList<ITalent> GetTalents()
			=> Talents.ToList();

		public bool IsTalentActive(ITalent talent)
		{
			if (!Context.IsWorldReady)
				throw new InvalidOperationException("The save file is not yet ready.");
			return ActiveTalents.Contains(talent);
		}

		public IReadOnlyList<ITalent> GetActiveTalents()
		{
			if (!Context.IsWorldReady)
				throw new InvalidOperationException("The save file is not yet ready.");
			return ActiveTalents.ToList();
		}

		public IReadOnlyDictionary<ITalentTag, int> GetEarnedTalentPoints()
		{
			if (!Context.IsWorldReady)
				throw new InvalidOperationException("The save file is not yet ready.");
			return EarnedTalentPoints.ToDictionary(e => e.Key, e => e.Value);
		}

		public IReadOnlyDictionary<ITalentTag, int> GetSpentTalentPoints()
		{
			if (!Context.IsWorldReady)
				throw new InvalidOperationException("The save file is not yet ready.");
			return SpentTalentPoints.ToDictionary(e => e.Key, e => e.Value);
		}

		public IReadOnlyDictionary<ITalentTag, int> GetAvailableTalentPoints()
		{
			if (!Context.IsWorldReady)
				throw new InvalidOperationException("The save file is not yet ready.");
			return EarnedTalentPoints.ToDictionary(e => e.Key, e => e.Value - (SpentTalentPoints.TryGetValue(e.Key, out var spentPoints) ? spentPoints : 0));
		}

		public bool ActivateTalent(ITalent talent)
		{
			if (!Context.IsWorldReady)
				throw new InvalidOperationException("The save file is not yet ready.");
			if (ActiveTalents.Contains(talent))
				return false;

			// TODO: check if enough points are available
			// TODO: reduce points

			ActiveTalents.Add(talent);
			// TODO: persist
			return true;
		}

		public IReadOnlySet<ITalent> DeactivateTalent(ITalent talent)
		{
			if (!Context.IsWorldReady)
				throw new InvalidOperationException("The save file is not yet ready.");

			HashSet<ITalent> deactivatedTalents = new();
			if (!ActiveTalents.Contains(talent))
				return deactivatedTalents;

			// refund points

			ActiveTalents.Remove(talent);
			deactivatedTalents.Add(talent);
			// TODO: remove all other talents that no longer have satisfied requirements
			// TODO: persist

			return deactivatedTalents;
		}

		#endregion
	}
}