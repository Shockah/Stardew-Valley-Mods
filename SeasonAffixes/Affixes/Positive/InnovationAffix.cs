using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class InnovationAffix : BaseSeasonAffix
	{
		private static string ShortID => "Innovation";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(32, 80, 16, 16));

		private readonly List<WeakReference<SObject>> AffixApplied = new();

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		public override void OnRegister()
		{
			MachineTracker.MachineChangedEvent += OnMachineChanged;
		}

		public override void OnUnregister()
		{
			MachineTracker.MachineChangedEvent -= OnMachineChanged;
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
				machine.MinutesUntilReady = (int)Math.Floor(machine.MinutesUntilReady * 0.75);
			}
		}
	}
}