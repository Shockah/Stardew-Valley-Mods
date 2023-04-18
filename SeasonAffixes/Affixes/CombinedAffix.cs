using Shockah.Kokoro.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes
{
	internal sealed class CombinedAffix : BaseSeasonAffix, ISeasonAffix
	{
		public override string UniqueID => $"CombinedAffix{{{string.Join(", ", Affixes.Select(a => a.UniqueID).OrderBy(id => id))}}}";
		public override string LocalizedName => LocalizedNameProvider();
		public override string LocalizedDescription => LocalizedDescriptionProvider();
		public override TextureRectangle Icon => IconProvider();

		internal readonly IReadOnlySet<ISeasonAffix> Affixes;
		private readonly Func<string> LocalizedNameProvider;
		private readonly Func<string> LocalizedDescriptionProvider;
		private readonly Func<TextureRectangle> IconProvider;
		private readonly Func<OrdinalSeason, double> ProbabilityWeightProvider;

		public CombinedAffix(IReadOnlySet<ISeasonAffix> affixes, Func<string> localizedName, Func<string> localizedDescription, Func<TextureRectangle> icon, Func<OrdinalSeason, double>? probabilityWeightProvider = null)
		{
			this.Affixes = affixes;
			this.LocalizedNameProvider = localizedName;
			this.LocalizedDescriptionProvider = localizedDescription;
			this.IconProvider = icon;
			this.ProbabilityWeightProvider = probabilityWeightProvider ?? (_ => 1);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetPositivity(OrdinalSeason season)
			=> Affixes.Sum(a => a.GetPositivity(season));

		[MethodImpl(MethodImplOptions.NoInlining)]
		public override int GetNegativity(OrdinalSeason season)
			=> Affixes.Sum(a => a.GetNegativity(season));

		[MethodImpl(MethodImplOptions.NoInlining)]
		public double GetProbabilityWeight(OrdinalSeason season)
			=> ProbabilityWeightProvider(season);

		public override void OnRegister()
		{
			foreach (var affix in Affixes)
				affix.OnRegister();
		}

		public override void OnUnregister()
		{
			foreach (var affix in Affixes)
				affix.OnUnregister();
		}

		public override void OnActivate()
		{
			foreach (var affix in Affixes)
				affix.OnActivate();
		}

		public override void OnDeactivate()
		{
			foreach (var affix in Affixes)
				affix.OnDeactivate();
		}
	}
}