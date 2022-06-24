using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.ProjectFluent
{
	internal interface IFluentProvider
	{
		IFluent<string> GetFluent(IGameLocale locale, IManifest mod, string? file = null);
	}

	internal class FluentProvider: IFluentProvider, IDisposable
	{
		private IFallbackFluentProvider FallbackFluentProvider { get; set; }
		private IModFluentPathProvider ModFluentPathProvider { get; set; }
		private IContextfulFluentFunctionProvider ContextfulFluentFunctionProvider { get; set; }

		private IList<WeakReference<Fluent>> Fluents { get; set; } = new List<WeakReference<Fluent>>();

		public FluentProvider(IFallbackFluentProvider fallbackFluentProvider, IModFluentPathProvider modFluentPathProvider, IContextfulFluentFunctionProvider contextfulFluentFunctionProvider)
		{
			this.FallbackFluentProvider = fallbackFluentProvider;
			this.ModFluentPathProvider = modFluentPathProvider;
			this.ContextfulFluentFunctionProvider = contextfulFluentFunctionProvider;

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

		public IFluent<string> GetFluent(IGameLocale locale, IManifest mod, string? file = null)
		{
			var toRemove = Fluents.Where(r => !r.TryGetTarget(out _)).ToList();
			foreach (var reference in toRemove)
				Fluents.Remove(reference);

			foreach (var reference in Fluents)
			{
				if (!reference.TryGetTarget(out var cached))
					continue;
				if (cached.Locale.LocaleCode == locale.LocaleCode && cached.Mod.UniqueID == mod.UniqueID && cached.File == file)
					return cached;
			}

			var fluent = new Fluent(locale, mod, file, FallbackFluentProvider.GetFallbackFluent(mod), ModFluentPathProvider, ContextfulFluentFunctionProvider);
			Fluents.Add(new WeakReference<Fluent>(fluent));
			return fluent;
		}

		private class Fluent: IFluent<string>
		{
			internal IGameLocale Locale { get; private set; }
			internal IManifest Mod { get; private set; }
			internal string? File { get; private set; }
			private IFluent<string> Fallback { get; set; }

			private IModFluentPathProvider ModFluentPathProvider { get; set; }
			private IContextfulFluentFunctionProvider ContextfulFluentFunctionProvider { get; set; }

			private IFluent<string>? CachedFluent { get; set; }

			private IFluent<string> CurrentFluent
			{
				get
				{
					if (CachedFluent is null)
						CachedFluent = new FileResolvingFluent(
							ContextfulFluentFunctionProvider.GetFluentFunctionsForMod(Mod),
							Locale, ModFluentPathProvider.GetFilePathCandidates(Locale, Mod, File), Fallback
						);
					return CachedFluent;
				}
			}

			public Fluent(
				IGameLocale locale, IManifest mod, string? file, IFluent<string> fallback,
				IModFluentPathProvider modFluentPathProvider,
				IContextfulFluentFunctionProvider contextfulFluentFunctionProvider
			)
			{
				this.Locale = locale;
				this.Mod = mod;
				this.File = file;
				this.Fallback = fallback;

				this.ModFluentPathProvider = modFluentPathProvider;
				this.ContextfulFluentFunctionProvider = contextfulFluentFunctionProvider;
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