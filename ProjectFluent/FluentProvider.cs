using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal interface IFluentProvider
	{
		IFluent<string> GetFluent(IGameLocale locale, IManifest mod, string? name = null);
	}

	internal class FluentProvider: IFluentProvider, IDisposable
	{
		private IFallbackFluentProvider FallbackFluentProvider { get; set; }
		private IModFluentPathProvider ModFluentPathProvider { get; set; }

		private IList<WeakReference<Fluent>> Fluents { get; set; } = new List<WeakReference<Fluent>>();

		public FluentProvider(IFallbackFluentProvider fallbackFluentProvider, IModFluentPathProvider modFluentPathProvider)
		{
			this.FallbackFluentProvider = fallbackFluentProvider;
			this.ModFluentPathProvider = modFluentPathProvider;

			ModFluentPathProvider.CandidatesChanged += OnCandidatesChanged;
		}

		public void Dispose()
		{
			ModFluentPathProvider.CandidatesChanged -= OnCandidatesChanged;
		}

		private void OnCandidatesChanged(IModFluentPathProvider provider)
		{
			foreach (var reference in Fluents)
			{
				if (!reference.TryGetTarget(out var cached))
					continue;
				cached.MarkDirty();
			}
		}

		public IFluent<string> GetFluent(IGameLocale locale, IManifest mod, string? name = null)
		{
			var toRemove = Fluents.Where(r => !r.TryGetTarget(out _)).ToList();
			foreach (var reference in toRemove)
				Fluents.Remove(reference);

			foreach (var reference in Fluents)
			{
				if (!reference.TryGetTarget(out var cached))
					continue;
				if (cached.Locale.LanguageCode == locale.LanguageCode && cached.Mod.UniqueID == mod.UniqueID && cached.Name == name)
					return cached;
			}

			var fluent = new Fluent(locale, mod, name, FallbackFluentProvider.GetFallbackFluent(mod), ModFluentPathProvider);
			Fluents.Add(new WeakReference<Fluent>(fluent));
			return fluent;
		}

		private class Fluent: IFluent<string>
		{
			internal IGameLocale Locale { get; private set; }
			internal IManifest Mod { get; private set; }
			internal string? Name { get; private set; }
			private IFluent<string> Fallback { get; set; }

			private IModFluentPathProvider ModFluentPathProvider { get; set; }

			private IFluent<string>? CachedFluent { get; set; }

			private IFluent<string> CurrentFluent
			{
				get
				{
					if (CachedFluent is null)
						CachedFluent = new FileResolvingFluent(Locale, ModFluentPathProvider.GetFilePathCandidates(Locale, Mod, Name), Fallback);
					return CachedFluent;
				}
			}

			public Fluent(
				IGameLocale locale, IManifest mod, string? name, IFluent<string> fallback,
				IModFluentPathProvider modFluentPathProvider
			)
			{
				this.Locale = locale;
				this.Mod = mod;
				this.Name = name;
				this.Fallback = fallback;

				this.ModFluentPathProvider = modFluentPathProvider;
			}

			internal void MarkDirty()
			{
				CachedFluent = null;
			}

			public bool ContainsKey(string key)
				=> CurrentFluent.ContainsKey(key);

			public string Get(string key, object? tokens)
				=> CurrentFluent.Get(key, tokens);
		}
	}
}