using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shockah.UIKit.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.UIKit.Tools
{
	internal class DebugFrames
	{
		private const float EdgeOvershoot = 8f;
		private const float HoverAlpha = 0.3f;
		private const float NonHoverAlpha = 1f;

		internal IReadOnlyList<Color> Colors { get; set; }
		internal readonly IDictionary<UIView, Color> ColorOverrides = new Dictionary<UIView, Color>();
		internal readonly ISet<UIRootView> ObservedRootViews = new HashSet<UIRootView>();
		internal readonly ISet<UIView> ObservedViews = new HashSet<UIView>();
		internal readonly ISet<UIView> ObservedViewHierarchies = new HashSet<UIView>();

		public DebugFrames()
		{
			Colors = new[] { Color.Red, Color.Orange, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta };
		}

		~DebugFrames()
		{
			foreach (var root in ObservedRootViews)
				StopObservingRootView(root);
		}

		internal bool IsObservingView(UIView view)
			=> ObservedViews.Contains(view);

		internal bool IsObservingViewHierarchy(UIView view)
			=> ObservedViewHierarchies.Contains(view);

		internal void ObserveView(UIView view)
		{
			var root = view.Root ?? view as UIRootView;
			if (root is null)
				throw new ArgumentException($"View {view} does not have a root view nor is a root view itself.");
			if (ObservedViews.Contains(view))
				return;
			ObservedViews.Add(view);
			ObserveRootView(root);
		}

		internal void StopObservingView(UIView view)
		{
			if (!ObservedViews.Contains(view))
				return;
			ObservedViews.Remove(view);
			CleanUpObservedRootViews();
		}

		internal void StopObserving()
		{
			ObservedViews.Clear();
			ObservedViewHierarchies.Clear();
			CleanUpObservedRootViews();
		}

		internal void ObserveViewHierarchy(UIView view)
		{
			var root = view.Root ?? view as UIRootView;
			if (root is null)
				throw new ArgumentException($"View {view} does not have a root view nor is a root view itself.");
			if (ObservedViewHierarchies.Contains(view))
				return;
			ObservedViewHierarchies.Add(view);
			ObserveRootView(root);
		}

		internal void StopObservingViewHierarchy(UIView view)
		{
			if (!ObservedViewHierarchies.Contains(view))
				return;
			ObservedViewHierarchies.Remove(view);
			CleanUpObservedRootViews();
		}

		private void ObserveRootView(UIRootView root)
		{
			if (ObservedRootViews.Contains(root))
				return;
			root.RenderedViewEvent += OnRenderedView;
			ObservedRootViews.Add(root);
		}

		private void StopObservingRootView(UIRootView root)
		{
			if (!ObservedRootViews.Contains(root))
				return;
			root.RenderedViewEvent -= OnRenderedView;
			ObservedRootViews.Remove(root);
		}

		private void CleanUpObservedRootViews()
		{
			var rootViewsToStopObserving = ObservedRootViews.Where(rv => !ObservedViews.Any(v => v.Root == rv || v == rv) && !ObservedViewHierarchies.Any(v => v.Root == rv || v == rv)).ToList();
			foreach (var root in rootViewsToStopObserving)
				StopObservingRootView(root);
		}

		private void OnRenderedView(UIRootView root, UIView view, RenderContext context)
		{
			bool ShouldShow(UIView view)
			{
				if (ObservedViews.Contains(view))
					return true;
				foreach (var hierarchy in ObservedViewHierarchies)
					if (view.GetViewHierarchy(true).Contains(hierarchy))
						return true;
				return false;
			}

			if (!ShouldShow(view))
				return;
			if (!ColorOverrides.TryGetValue(view, out var color))
			{
				var depth = view.GetViewHierarchy(false).Count();
				color = Colors[depth % Colors.Count];
			}

			var alpha = view.Hover == HoverState.None ? NonHoverAlpha : HoverAlpha;
			context.SpriteBatch.Draw(UITextureRect.Pixel.Texture, context.Offset, null, color * 0.5f * alpha, 0f, Vector2.Zero, view.Size, SpriteEffects.None, 0f);
			context.SpriteBatch.Draw(UITextureRect.Pixel.Texture, context.Offset + new UIVector2(-0.5f, -EdgeOvershoot), null, color * alpha, 0f, Vector2.Zero, new UIVector2(1, view.Height + 1f + EdgeOvershoot * 2f), SpriteEffects.None, 0f);
			context.SpriteBatch.Draw(UITextureRect.Pixel.Texture, context.Offset + new UIVector2(view.Width - 0.5f, -EdgeOvershoot), null, color * alpha, 0f, Vector2.Zero, new UIVector2(1, view.Height + 1f + EdgeOvershoot * 2f), SpriteEffects.None, 0f);
			context.SpriteBatch.Draw(UITextureRect.Pixel.Texture, context.Offset + new UIVector2(-EdgeOvershoot, -0.5f), null, color * alpha, 0f, Vector2.Zero, new UIVector2(view.Width + 1f + EdgeOvershoot * 2f, 1), SpriteEffects.None, 0f);
			context.SpriteBatch.Draw(UITextureRect.Pixel.Texture, context.Offset + new UIVector2(-EdgeOvershoot, view.Height - 0.5f), null, color * alpha, 0f, Vector2.Zero, new UIVector2(view.Width + 1f + EdgeOvershoot * 2f, 1), SpriteEffects.None, 0f);
		}
	}
}
