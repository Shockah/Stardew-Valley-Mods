using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace Shockah.CommonModCode.GMCM
{
	public readonly struct GraphViewConfiguration
	{
		public readonly bool Inline { get; private init; }
		public readonly int Height { get; private init; }
		public readonly Func<string?>? XAxisTitle { get; private init; }
		public readonly Func<string?>? YAxisTitle { get; private init; }
		public readonly Func<IEnumerable<Vector2>> Data { get; private init; }

		public GraphViewConfiguration(
			bool inline,
			int height,
			Func<string?>? xAxisTitle,
			Func<string?>? yAxisTitle,
			Func<IEnumerable<Vector2>> data
		)
		{
			this.Inline = inline;
			this.Height = height;
			this.XAxisTitle = xAxisTitle;
			this.YAxisTitle = yAxisTitle;
			this.Data = data;
		}
	}

	public class GraphView
	{
		private const int RowHeight = 60;

		private readonly Func<string> Name;
		private readonly Func<string>? Tooltip;
		private readonly GraphViewConfiguration Configuration;

		public GraphView(Func<string> name, Func<string>? tooltip, GraphViewConfiguration configuration)
		{
			this.Name = name;
			this.Tooltip = tooltip;
			this.Configuration = configuration;
		}

		internal void AddToGMCM(IGenericModConfigMenuApi api, IManifest mod)
		{
			api.AddComplexOption(
				mod: mod,
				name: Name,
				tooltip: Tooltip,
				draw: (b, position) => Draw(b, position),
				height: () =>
				{
					if (Configuration.Inline)
						return Math.Max(RowHeight, Configuration.Height);
					else
						return RowHeight + Configuration.Height;
				},
				beforeMenuOpened: () => { },
				beforeMenuClosed: () => { },
				afterReset: () => { },
				beforeSave: () => { }
			);
		}

		private Vector2 GetGMCMSize()
			=> new(Math.Min(1200, Game1.uiViewport.Width - 200), Game1.uiViewport.Height - 128 - 116);

		private Vector2 GetGMCMPosition(Vector2? size = null)
		{
			Vector2 gmcmSize = size ?? GetGMCMSize();
			return new((Game1.uiViewport.Width - gmcmSize.X) / 2, (Game1.uiViewport.Height - gmcmSize.Y) / 2);
		}

		private void Draw(SpriteBatch b, Vector2 basePosition)
		{
			Vector2 gmcmSize = GetGMCMSize();
			Vector2 graphBoxPosition = Configuration.Inline ? basePosition : new(GetGMCMPosition(gmcmSize).X, basePosition.Y + RowHeight);
			Vector2 graphBoxSize = new(Configuration.Inline ? gmcmSize.X / 2 : gmcmSize.X, Configuration.Height);

			string? xAxisTitle = Configuration.XAxisTitle?.Invoke();
			string? yAxisTitle = Configuration.YAxisTitle?.Invoke();
			Vector2? xAxisTitleMeasurement = string.IsNullOrEmpty(xAxisTitle) ? null : Game1.dialogueFont.MeasureString(xAxisTitle);
			Vector2? yAxisTitleMeasurement = string.IsNullOrEmpty(yAxisTitle) ? null : Game1.dialogueFont.MeasureString(yAxisTitle);

			Vector2 graphSize = new(
				yAxisTitleMeasurement is null ? graphBoxSize.X : graphBoxSize.X - yAxisTitleMeasurement.Value.Y,
				xAxisTitleMeasurement is null ? graphBoxSize.Y : graphBoxSize.Y - xAxisTitleMeasurement.Value.Y
			);
			Vector2 graphPosition = new(graphBoxPosition.X + (graphBoxSize.X - graphSize.X), graphBoxPosition.Y);

			if (!string.IsNullOrEmpty(xAxisTitle))
				Utility.drawTextWithShadow(b, xAxisTitle, Game1.dialogueFont, graphBoxPosition + new Vector2(graphBoxSize.X / 2 - xAxisTitleMeasurement!.Value.X / 2, graphBoxSize.Y - xAxisTitleMeasurement!.Value.Y), Game1.textColor);
			if (!string.IsNullOrEmpty(yAxisTitle))
				b.DrawString(Game1.dialogueFont, yAxisTitle, graphBoxPosition + new Vector2(0f, graphBoxSize.Y / 2f), Game1.textColor, (float)-Math.PI / 2, Vector2.Zero, 1f, SpriteEffects.None, 0f);
		}
	}

	public static class GraphViewExtensions
	{
		public static void AddGraphView(
			this IGenericModConfigMenuApi api,
			IManifest mod,
			Func<string> name,
			GraphViewConfiguration configuration,
			Func<string>? tooltip = null
		)
		{
			var option = new GraphView(name, tooltip, configuration);
			option.AddToGMCM(api, mod);
		}
	}
}