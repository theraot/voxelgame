using System;
using Hexpoint.Blox.Hosts.World;
using Hexpoint.Blox.Server;

namespace Hexpoint.Blox.GameActions
{
	public enum StructureType : byte
	{
		Tree,
		Tower,
		SmallKeep,
		LargeKeep
	}

	internal class AddStructure : GameAction
	{
		public AddStructure()
		{
			DataLength = Position.SIZE + sizeof(ushort) * 2; //position + structure type + front face
		}

		public AddStructure(Position position, StructureType structureType, Facing frontFace) : this()
		{
			Position = position;
			StructureType = structureType;
			FrontFace = frontFace;
		}

		public override string ToString()
		{
			return String.Format("AddStructure {0} {1}", StructureType, Position);
		}

		internal override ActionType ActionType { get { return ActionType.AddStructure; } }
		/// <summary>Center of the structure.</summary>
		public Position Position;
		public StructureType StructureType;
		public Facing FrontFace;

		/// <summary>Number of blocks out from the center block to the bounding box border of the structure.</summary>
		public int Radius
		{
			get
			{
				switch (StructureType)
				{
					case StructureType.Tree:
					case StructureType.Tower:
						return 3;
					case StructureType.SmallKeep:
						return 5;
					case StructureType.LargeKeep:
						return 7;
				}
				throw new Exception("Unknown structure type: " + StructureType);
			}
		}

		/// <summary>Length required for a bounding box for this structure. Adds 1 to Radius x 2 to account for the center block.</summary>
		/// <remarks>Didnt end up using this property yet, but it could be useful down the road.</remarks>
		public int Diameter
		{
			get { return Radius * 2 + 1; }
		}

		protected override void Queue()
		{
			base.Queue();
			Write(ref Position);
			Write((ushort)StructureType);
			Write((ushort)FrontFace);
		}

		internal override void Receive()
		{
			if (!Config.IsSinglePlayer)
			{
				lock (TcpClient)
				{
					base.Receive();
					var bytes = ReadStream(DataLength);
					Position = new Position(bytes, 0);
					StructureType = (StructureType)BitConverter.ToUInt16(bytes, Position.SIZE);
					FrontFace = (Facing)BitConverter.ToUInt16(bytes, Position.SIZE + sizeof(ushort));
				}
			}

			switch (StructureType)
			{
				case StructureType.Tree:
					StructureBuilder.BuildTree(Position);
					break;
				case StructureType.Tower:
					StructureBuilder.BuildCastle(Position, Radius, 6, Block.BlockType.Cobble, FrontFace);
					break;
				case StructureType.SmallKeep:
					StructureBuilder.BuildCastle(Position, Radius, 8, Block.BlockType.Cobble, FrontFace);
					break;
				case StructureType.LargeKeep:
					StructureBuilder.BuildCastle(Position, Radius, 10, Block.BlockType.SteelPlate, FrontFace);
					break;
			}

			if (Config.IsServer)
			{
				foreach (var player in Controller.Players.Values)
				{
					new AddStructure(Position, StructureType, FrontFace) { ConnectedPlayer = player }.Send();
				}
			}
			else
			{
				//determine the corner coords for this structure; pass them for light calc and chunk queueing (note: the Y doesnt matter so use 0 for both)
				WorldData.ModifyLightAndQueueChunksForCuboidChange(new Position(Position.X - Radius, 0, Position.Z - Radius), new Position(Position.X + Radius, 0, Position.Z + Radius));
				Sounds.Audio.PlaySound(Sounds.SoundType.AddBlock);
			}
		}
	}
}
