using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class InnovationAffix : BaseSeasonAffix, ISeasonAffix
	{
		private static string ShortID => "Innovation";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description", new { Decrease = $"{(int)(Mod.Config.InnovationDecrease * 100):0.##}%" });
		public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(32, 80, 16, 16));

		private List<WeakReference<SObject>> AffixApplied = new();

		public InnovationAffix() : base($"{Mod.ModManifest.UniqueID}.{ShortID}") { }

		public int GetPositivity(OrdinalSeason season)
			=> 1;

		public int GetNegativity(OrdinalSeason season)
			=> 0;

		public void OnActivate()
		{
			AffixApplied.Clear();
			Mod.Helper.Events.GameLoop.DayEnding += OnDayEnding;
			MachineTracker.MachineChangedEvent += OnMachineChanged;
		}

		public void OnDeactivate()
		{
			Mod.Helper.Events.GameLoop.DayEnding -= OnDayEnding;
			MachineTracker.MachineChangedEvent -= OnMachineChanged;
		}

		public void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.positive.{ShortID}.config.decrease", () => Mod.Config.InnovationDecrease, min: 0.05f, max: 0.9f, interval: 0.05f, value => $"{(int)(value * 100):0.##}%");
		}

		private void OnDayEnding(object? sender, DayEndingEventArgs e)
		{
			AffixApplied = AffixApplied
				.Where(r => r.TryGetTarget(out _))
				.ToList();
		}

		private void OnMachineChanged(GameLocation location, SObject machine, MachineProcessingState? oldState, MachineProcessingState? newState)
		{
			if (!Context.IsMainPlayer)
				return;
			if (oldState is null || newState is null)
				return;

			var existingIndex = AffixApplied.FirstIndex(weakMachine => weakMachine.TryGetTarget(out var appliedMachine) && ReferenceEquals(machine, appliedMachine));
			if (existingIndex is not null)
			{
				AffixApplied.RemoveAt(existingIndex.Value);
				return;
			}

			if (!newState.Value.ReadyForHarvest && newState.Value.MinutesUntilReady > 0 && (oldState.Value.ReadyForHarvest || oldState.Value.MinutesUntilReady < newState.Value.MinutesUntilReady))
			{
				AffixApplied.Add(new(machine));
				machine.MinutesUntilReady = (int)Math.Floor(machine.MinutesUntilReady * (1f - Mod.Config.InnovationDecrease));
			}
		}
	}
}