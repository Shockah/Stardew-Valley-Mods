using Microsoft.Xna.Framework;
using Shockah.CommonModCode;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Shockah.UIKit.Tools
{
	public class UIKit: Mod, IUIKitToolsApi
	{
		internal static UIKit Instance = null!;

		private DebugFrames DebugFrames = null!;
		private ExampleUI ExampleUI = null!;

		private readonly IList<WeakReference<UIView>> KnownViewReferences = new List<WeakReference<UIView>>();
		private readonly IDictionary<UIView, string> ViewIdentifiers = new Dictionary<UIView, string>();

		private IEnumerable<UIRootView> KnownRootViews => KnownViewReferences
			.Select(r => r.GetTargetOrNull())
			.Select(r => r?.Root ?? r as UIRootView)
			.Where(r => r is not null)
			.Select(r => r!)
			.Distinct();

		public override void Entry(IModHelper helper)
		{
			Instance = this;
			DebugFrames = new();

			SetupCommands();

			ExampleUI = new(helper, Monitor);
			helper.Events.GameLoop.GameLaunched += ExampleUI.OnGameLaunched;
			helper.Events.GameLoop.UpdateTicking += ExampleUI.OnUpdateTicking;
			helper.Events.Display.RenderedHud += ExampleUI.OnRenderedHud;
			ExampleUI.Root.SetIdentifier($"{ModManifest.UniqueID}.example");
		}

		private void SetupCommands()
		{
			var prefix = "uikit_";

			foreach (var alias in new[] { "print_views", "pv" })
				Helper.ConsoleCommands.Add($"{prefix}{alias}", "TODO", PrintViewsCommand);

			foreach (var alias in new[] { "show_debug_frames", "sdf" })
				Helper.ConsoleCommands.Add($"{prefix}{alias}", "TODO", ShowDebugFramesCommand);
			foreach (var alias in new[] { "hide_debug_frames", "hdf" })
				Helper.ConsoleCommands.Add($"{prefix}{alias}", "TODO", HideDebugFramesCommand);
			foreach (var alias in new[] { "show_hierarchy_debug_frames", "shdf" })
				Helper.ConsoleCommands.Add($"{prefix}{alias}", "TODO", ShowHierarchyDebugFramesCommand);
			foreach (var alias in new[] { "hide_hierarchy_debug_frames", "hhdf" })
				Helper.ConsoleCommands.Add($"{prefix}{alias}", "TODO", HideHierarchyDebugFramesCommand);
		}

		public override object GetApi()
			=> this;

		#region Commands

		private IEnumerable<KeyValuePair<UIView, string>> ParseViewsArgument(string? arg)
		{
			IEnumerable<KeyValuePair<UIView, string>> matchingViews = ViewIdentifiers;
			if (arg is not null)
			{
				try
				{
					var regex = new Regex(arg);
					matchingViews = matchingViews
						.Where(kv => regex.IsMatch(kv.Value));
				}
				catch (Exception)
				{
					Monitor.Log("Provided regex is invalid.", LogLevel.Error);
					return matchingViews;
				}
			}
			return matchingViews;
		}

		private void PrintViewsCommand(string command, string[] args)
		{
			string ViewToString(UIView view, int depth)
			{
				var sb = new StringBuilder();
				for (int i = 1; i < depth; i++)
					sb.Append("  ");
				if (depth != 0)
					sb.Append("|-");

				sb.Append(view.ToString());
				if (ViewIdentifiers.TryGetValue(view, out var identifier))
					sb.Append($" '{identifier}'");
				sb.Append($" | Type: {view.GetType().Name}");
				sb.Append($" | IsVisible: {view.IsVisible}");
				sb.Append($" | {view.Width}x{view.Height} @ {view.TopLeft}"); // TODO: configuration to print a different coordinate instead, maybe

				foreach (var subview in view.Subviews)
					sb.Append($"\n{ViewToString(subview, depth + 1)}");

				return sb.ToString();
			}

			var knownRootViews = KnownRootViews.ToList();
			if (knownRootViews.Count == 0)
			{
				Monitor.Log("No views to print.", LogLevel.Info);
			}
			else
			{
				Monitor.Log($"Printing {knownRootViews.Count} view hierarch(y/ies):", LogLevel.Info);
				foreach (var root in knownRootViews)
					Monitor.Log(ViewToString(root, 0), LogLevel.Info);
			}
		}

		private void ShowDebugFramesCommand(string command, string[] args)
		{
			var views = ParseViewsArgument(args.Length == 0 ? null : args[0]).ToList();
			Monitor.Log($"Showing debug frames for {views.Count} matching view(s).", LogLevel.Info);
			foreach (var (view, _) in views)
				DebugFrames.ObserveView(view);
		}

		private void HideDebugFramesCommand(string command, string[] args)
		{
			var views = ParseViewsArgument(args.Length == 0 ? null : args[0]).ToList();
			Monitor.Log($"Hiding debug frames for {views.Count} matching view(s).", LogLevel.Info);
			foreach (var (view, _) in views)
				DebugFrames.StopObservingView(view);
		}

		private void ShowHierarchyDebugFramesCommand(string command, string[] args)
		{
			var views = ParseViewsArgument(args.Length == 0 ? null : args[0]).ToList();
			Monitor.Log($"Showing hierarchy debug frames for {views.Count} matching view(s).", LogLevel.Info);
			foreach (var (view, _) in views)
				DebugFrames.ObserveViewHierarchy(view);
		}

		private void HideHierarchyDebugFramesCommand(string command, string[] args)
		{
			var views = ParseViewsArgument(args.Length == 0 ? null : args[0]).ToList();
			Monitor.Log($"Hiding hierarchy debug frames for {views.Count} matching view(s).", LogLevel.Info);
			foreach (var (view, _) in views)
				DebugFrames.StopObservingViewHierarchy(view);
		}

		#endregion

		#region API

		#region Identifiers

		public string? GetViewIdentifier(UIView view)
			=> ViewIdentifiers.TryGetValue(view, out var identifier) ? identifier : null;

		public void SetViewIdentifier(UIView view, string? identifier)
		{
			if (identifier is null)
			{
				ViewIdentifiers.Remove(view);
			}
			else
			{
				ViewIdentifiers[view] = identifier;
				RegisterView(view);
			}
		}

		public void RegisterView(UIView view)
		{
			foreach (var reference in KnownViewReferences)
				if (reference.GetTargetOrNull() == view)
					return;
			KnownViewReferences.Add(new(view));
		}

		#endregion

		#region Debug Frames

		public IReadOnlyList<Color> GetDebugFrameColors()
			=> DebugFrames.Colors;

		public void SetDebugFrameColors(IReadOnlyList<Color> colors)
			=> DebugFrames.Colors = colors;

		public Color? GetDebugFrameColorOverride(UIView view)
			=> DebugFrames.ColorOverrides.TryGetValue(view, out var value) ? value : null;

		public void SetDebugFrameColorOverride(Color? color, UIView view)
		{
			if (color is null)
				DebugFrames.ColorOverrides.Remove(view);
			else
				DebugFrames.ColorOverrides[view] = color.Value;
		}

		public bool GetDebugFrameVisible(UIView view)
			=> DebugFrames.IsObservingView(view);

		public void SetDebugFrameVisible(bool visible, UIView view)
		{
			if (visible)
				DebugFrames.ObserveView(view);
			else
				DebugFrames.StopObservingView(view);
		}

		public bool GetHierarchyDebugFramesVisible(UIView hierarchy)
			=> DebugFrames.IsObservingViewHierarchy(hierarchy);

		public void SetHierarchyDebugFramesVisible(bool visible, UIView hierarchy)
		{
			if (visible)
				DebugFrames.ObserveViewHierarchy(hierarchy);
			else
				DebugFrames.StopObservingViewHierarchy(hierarchy);
		}

		#endregion

		#endregion API
	}
}