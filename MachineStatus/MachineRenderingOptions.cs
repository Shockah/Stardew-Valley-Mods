namespace Shockah.MachineStatus
{
	internal static class MachineRenderingOptions
	{
		public enum Grouping { None, ByMachine, ByMachineAndItem }

		public enum Sorting {
			None,
			ByMachineAZ, ByMachineZA,
			ReadyFirst, WaitingFirst, BusyFirst,
			ByDistanceAscending, ByDistanceDescending,
			ByItemAZ, ByItemZA
		}

		public enum BubbleSway { Static, Together, Wave }

		public enum Visibility { Hidden, Normal, Focused }
	}
}
