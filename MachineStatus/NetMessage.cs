using Shockah.CommonModCode;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Shockah.MachineStatus
{
	internal static class NetMessage
	{
		public static class Entity
		{
			public readonly struct Color
			{
				public readonly byte R { get; }
				public readonly byte G { get; }
				public readonly byte B { get; }
				public readonly byte A { get; }

				public Color(byte r, byte g, byte b, byte a)
				{
					this.R = r;
					this.G = g;
					this.B = b;
					this.A = a;
				}

				public static implicit operator Color(XnaColor c)
					=> new(c.R, c.G, c.B, c.A);

				public static implicit operator XnaColor(Color c)
					=> new(c.R, c.G, c.B, c.A);
			}

			public readonly struct SObject
			{
				public readonly int ParentSheetIndex { get; }
				public readonly string Name { get; }
				public readonly bool BigCraftable { get; }
				public readonly bool ShowNextIndex { get; }
				public readonly Color? Color { get; }
				public readonly bool ColorSameIndexAsParentSheetIndex { get; }

				public SObject(int parentSheetIndex, string name, bool bigCraftable, bool showNextIndex, Color? color, bool colorSameIndexAsParentSheetIndex)
				{
					this.ParentSheetIndex = parentSheetIndex;
					this.Name = name;
					this.BigCraftable = bigCraftable;
					this.ShowNextIndex = showNextIndex;
					this.Color = color;
					this.ColorSameIndexAsParentSheetIndex = colorSameIndexAsParentSheetIndex;
				}

				public static SObject Create(StardewValley.Object @object)
				{
					Color? color = null;
					bool colorSameIndexAsParentSheetIndex = false;
					if (@object is StardewValley.Objects.ColoredObject colored)
					{
						color = colored.color.Value;
						colorSameIndexAsParentSheetIndex = colored.ColorSameIndexAsParentSheetIndex;
					}

					return new(
						@object.ParentSheetIndex,
						@object.Name,
						@object.bigCraftable.Value,
						@object.showNextIndex.Value,
						color,
						colorSameIndexAsParentSheetIndex
					);
				}

				public bool Matches(StardewValley.Object @object)
					=> ParentSheetIndex == @object.ParentSheetIndex && BigCraftable == @object.bigCraftable.Value && Name == @object.Name;

				public StardewValley.Object Retrieve(IntPoint? tileLocation)
				{
					var result = tileLocation.HasValue
						? new StardewValley.Object(new Microsoft.Xna.Framework.Vector2(tileLocation.Value.X, tileLocation.Value.Y), ParentSheetIndex, 1)
						: (Color.HasValue ? new StardewValley.Objects.ColoredObject(ParentSheetIndex, 1, Color.Value) : new StardewValley.Object(ParentSheetIndex, 1));
					result.Name = Name;
					result.bigCraftable.Value = BigCraftable;
					result.showNextIndex.Value = ShowNextIndex;
					if (result is StardewValley.Objects.ColoredObject colored)
						colored.ColorSameIndexAsParentSheetIndex = ColorSameIndexAsParentSheetIndex;
					return result;
				}

				public override string ToString()
					=> $"{ParentSheetIndex}:{Name}{(BigCraftable ? " (BigCraftable)" : "")}";
			}
		}
		
		public struct MachineUpsert
		{
			public LocationDescriptor Location { get; set; }
			public IntPoint TileLocation { get; set; }
			public Entity.SObject Machine { get; set; }
			public Entity.SObject? HeldObject { get; set; }
			public bool ReadyForHarvest { get; set; }
			public int MinutesUntilReady { get; set; }
			public MachineState State { get; set; }

			public MachineUpsert(
				LocationDescriptor location,
				IntPoint tileLocation,
				Entity.SObject machine,
				Entity.SObject? heldObject,
				bool readyForHarvest,
				int minutesUntilReady,
				MachineState state
			)
			{
				this.Location = location;
				this.TileLocation = tileLocation;
				this.Machine = machine;
				this.HeldObject = heldObject;
				this.ReadyForHarvest = readyForHarvest;
				this.MinutesUntilReady = minutesUntilReady;
				this.State = state;
			}

			public static MachineUpsert Create(LocationDescriptor location, StardewValley.Object machine, MachineState state)
			{
				Entity.SObject? heldObject = null;
				if (machine.heldObject.Value is not null)
					heldObject = Entity.SObject.Create(machine.heldObject.Value);
				return new(
					location,
					new IntPoint((int)machine.TileLocation.X, (int)machine.TileLocation.Y),
					Entity.SObject.Create(machine),
					heldObject,
					machine.readyForHarvest.Value,
					machine.MinutesUntilReady,
					state
				);
			}

			public bool MatchesMachine(StardewValley.Object machine)
				=> Machine.Matches(machine) && TileLocation.X == (int)machine.TileLocation.X && TileLocation.Y == (int)machine.TileLocation.Y;

			public StardewValley.Object RetrieveMachine()
			{
				var machine = Machine.Retrieve(TileLocation);
				machine.TileLocation = new Microsoft.Xna.Framework.Vector2(TileLocation.X, TileLocation.Y);
				if (HeldObject is not null)
					machine.heldObject.Value = HeldObject.Value.Retrieve(null);
				machine.readyForHarvest.Value = ReadyForHarvest;
				machine.MinutesUntilReady = MinutesUntilReady;
				return machine;
			}
		}

		public readonly struct MachineRemove
		{
			public readonly LocationDescriptor Location { get; }
			public readonly IntPoint TileLocation { get; }
			public readonly int MachineParentSheetIndex { get; }
			public readonly string MachineName { get; }

			public MachineRemove(LocationDescriptor location, IntPoint tileLocation, int machineParentSheetIndex, string machineName)
			{
				this.Location = location;
				this.TileLocation = tileLocation;
				this.MachineParentSheetIndex = machineParentSheetIndex;
				this.MachineName = machineName;
			}

			public static MachineRemove Create(StardewValley.GameLocation location, StardewValley.Object machine)
				=> new(
					LocationDescriptor.Create(location),
					new IntPoint((int)machine.TileLocation.X, (int)machine.TileLocation.Y),
					machine.ParentSheetIndex,
					machine.Name
				);

			public bool MatchesMachine(StardewValley.Object machine)
				=> MachineParentSheetIndex == machine.ParentSheetIndex && MachineName == machine.Name &&
				TileLocation.X == (int)machine.TileLocation.X && TileLocation.Y == (int)machine.TileLocation.Y;
		}
	}
}