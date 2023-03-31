﻿using Shockah.Kokoro.Stardew;
using System;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes
{
	public interface ISeasonAffixesApi
	{
		ModConfig Config { get; }

		IReadOnlyDictionary<string, ISeasonAffix> AllAffixes { get; }
		IReadOnlyList<ISeasonAffix> ActiveAffixes { get; }

		ISeasonAffix? GetAffix(string uniqueID);

		void RegisterAffix(ISeasonAffix affix);
		void UnregisterAffix(ISeasonAffix affix);
		void RegisterAffixConflictHandler(Func<ISeasonAffix, ISeasonAffix, Season, int, bool> handler);

		void ActivateAffix(ISeasonAffix affix);
		void DeactivateAffix(ISeasonAffix affix);
		void DeactivateAllAffixes();

		IReadOnlySet<ISeasonAffix> GetAllPossibleAffixesForSeason(Season season, int year);

		void PresentAffixChoiceMenu(Season season, int year, int rerollCount, Action<ISeasonAffix> onAffixChosen);
		void PresentAffixChoiceMenu(IEnumerable<IReadOnlySet<ISeasonAffix>> choices, int rerollCount, Action<ISeasonAffix> onAffixChosen);
	}
}