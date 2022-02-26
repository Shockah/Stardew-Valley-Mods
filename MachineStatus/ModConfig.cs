using Newtonsoft.Json;
using Shockah.CommonModCode.UI;
using System.Collections.Generic;

namespace Shockah.MachineStatus
{
	internal class ModConfig
	{
		public UIAnchorSide ScreenAnchorSide { get; set; } = UIAnchorSide.BottomLeft;
		public int AnchorInset { get; set; } = 16;
		public int AnchorOffsetX { get; set; } = 0;
		public int AnchorOffsetY { get; set; } = 0;
		public UIAnchorSide PanelAnchorSide { get; set; } = UIAnchorSide.BottomLeft;
		[JsonIgnore] public UIAnchor Anchor => new(ScreenAnchorSide, AnchorInset, new(AnchorOffsetX, AnchorOffsetY), PanelAnchorSide);

		public bool GroupByItem { get; set; } = true;
		public IList<string> GroupByItemExceptions { get; set; } = new List<string>();

		public bool GroupByItemQuality { get; set; } = true;
		public IList<string> GroupByItemQualityExceptions { get; set; } = new List<string>();

		public bool ShowReady { get; set; } = true;
		public IList<string> ShowReadyExceptions { get; set; } = new List<string>();

		public bool ShowWaiting { get; set; } = false;
		public IList<string> ShowWaitingExceptions { get; set; } = new List<string> { "*|Cask", "*|Keg", "*|Preserves Jar" };

		public bool ShowBusy { get; set; } = false;
		public IList<string> ShowBusyExceptions { get; set; } = new List<string>();
	}
}
