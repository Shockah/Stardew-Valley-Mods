namespace Shockah.MachineStatus
{
	internal static class MachineRenderingOptions
	{
		public enum Grouping { None, ByMachine, ByMachineAndItem, ByMachineAndItemAndQuality }

		public enum Sorting {
			None,
			ByMachineAZ, ByMachineZA,
			ByDistanceAscending, ByDistanceDescending,
			ByItemAZ, ByItemZA,
			ByItemQualityBest, ByItemQualityWorst
		}
	}
}
