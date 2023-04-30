using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class LootAffix : BaseSeasonAffix
	{
		private static bool IsHarmonySetup = false;
		private static readonly WeakCounter<GameLocation> MonsterDropCallCounter = new();
		private static readonly ConditionalWeakTable<GameLocation, Random> MonsterDropRandomCache = new();

		private static string ShortID => "Loot";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(352, 96, 16, 16));

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
			=> 0;

		public override IReadOnlySet<string> Tags
			=> new HashSet<string> { VanillaSkill.Combat.UniqueID };

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatchVirtual(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(GameLocation), nameof(GameLocation.monsterDrop)),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(LootAffix), nameof(GameLocation_monsterDrop_Prefix)), priority: Priority.First),
				finalizer: new HarmonyMethod(AccessTools.Method(typeof(LootAffix), nameof(GameLocation_monsterDrop_Finalizer)), priority: Priority.Last)
			);
		}

		private static void GameLocation_monsterDrop_Prefix(GameLocation __instance)
		{
			if (!Mod.ActiveAffixes.Any(a => a is LootAffix))
				return;

			uint counter = MonsterDropCallCounter.Push(__instance);
			if (counter != 1)
				return;

			MonsterDropRandomCache.AddOrUpdate(__instance, Game1.random);
			Game1.random = new CustomRandom(Game1.random);
		}

		private static void GameLocation_monsterDrop_Finalizer(GameLocation __instance)
		{
			if (!Mod.ActiveAffixes.Any(a => a is LootAffix))
				return;

			uint counter = MonsterDropCallCounter.Pop(__instance);
			if (counter != 0)
				return;

			if (!MonsterDropRandomCache.TryGetValue(__instance, out var cachedRandom))
				throw new InvalidOperationException("Expected a Random instance to be cached.");
			Game1.random = cachedRandom;
		}

		private sealed class CustomRandom : Random
		{
			private Random Wrapped { get; init; }

			public CustomRandom(Random wrapped)
			{
				this.Wrapped = wrapped;
			}

			public override int Next()
				=> Wrapped.Next();

			public override int Next(int maxValue)
				=> Wrapped.Next(maxValue);

			public override int Next(int minValue, int maxValue)
				=> Wrapped.Next(minValue, maxValue);

			public override void NextBytes(byte[] buffer)
				=> Wrapped.NextBytes(buffer);

			public override void NextBytes(Span<byte> buffer)
				=> Wrapped.NextBytes(buffer);

			public override double NextDouble()
				=> Math.Pow(Wrapped.NextDouble(), 2.0 / 3.0);
		}
	}
}