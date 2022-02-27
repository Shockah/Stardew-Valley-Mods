using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Shockah.CommonModCode.UI;
using System.Collections.Generic;

namespace Shockah.MachineStatus
{
	internal class ModConfig
	{
		public UIAnchorSide ScreenAnchorSide { get; set; } = UIAnchorSide.BottomLeft;
		public float AnchorInset { get; set; } = 16f;
		public float AnchorOffsetX { get; set; } = 0f;
		public float AnchorOffsetY { get; set; } = 0f;
		public UIAnchorSide PanelAnchorSide { get; set; } = UIAnchorSide.BottomLeft;
		[JsonIgnore] public UIAnchor Anchor => new(ScreenAnchorSide, AnchorInset, new(AnchorOffsetX, AnchorOffsetY), PanelAnchorSide);

		public FlowDirection FlowDirection { get; set; } = FlowDirection.LeftToRightAndBottomToTop;
		public float Scale { get; set; } = 1f;
		public float XSpacing { get; set; } = 4f;
		public float YSpacing { get; set; } = 4f;
		[JsonIgnore] public Vector2 Spacing => new(XSpacing, YSpacing);
		public int MaxColumns { get; set; } = 0;

		public bool ShowItemBubble { get; set; } = true;
		public float BubbleItemCycleTime { get; set; } = 2f;
		public MachineRenderingOptions.BubbleSway BubbleSway { get; set; } = MachineRenderingOptions.BubbleSway.Wave;

		public MachineRenderingOptions.Grouping Grouping { get; set; } = MachineRenderingOptions.Grouping.ByMachine;
		public IList<MachineRenderingOptions.Sorting> Sorting { get; set; } = new List<MachineRenderingOptions.Sorting>
		{
			MachineRenderingOptions.Sorting.ByMachineAZ,
			MachineRenderingOptions.Sorting.ByItemAZ,
			MachineRenderingOptions.Sorting.ByItemQualityBest,
			MachineRenderingOptions.Sorting.None
		};

		public bool ShowReady { get; set; } = true;
		public IList<string> ShowReadyExceptions { get; set; } = new List<string>();

		public bool ShowWaiting { get; set; } = false;
		public IList<string> ShowWaitingExceptions { get; set; } = new List<string> { "*|Cask", "*|Keg", "*|Preserves Jar" };

		public bool ShowBusy { get; set; } = false;
		public IList<string> ShowBusyExceptions { get; set; } = new List<string>();
	}
}
