using System;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	internal class AddCuboid : GameAction
	{
		public AddCuboid()
		{
			DataLength = Position.SIZE + Position.SIZE + sizeof(ushort); //2 coords + block type
		}

		public AddCuboid(Position position1, Position position2, Block.BlockType blockType) : this()
		{
			Position1 = position1;
			Position2 = position2;
			BlockType = blockType;
		}

		public override string ToString()
		{
			return String.Format("AddCuboid {0} {1} {2}", BlockType, Position1, Position2);
		}

		internal override ActionType ActionType { get { return ActionType.AddCuboid; } }
		public Position Position1;
		public Position Position2;
		public Block.BlockType BlockType;

		protected override void Queue()
		{
			base.Queue();
			Write(ref Position1);
			Write(ref Position2);
			Write((ushort)BlockType);
		}

		internal override void Receive()
		{
			if (!Config.IsSinglePlayer)
			{
				lock (TcpClient)
				{
					base.Receive();
					var bytes = ReadStream(DataLength);
					Position1 = new Position(bytes, 0);
					Position2 = new Position(bytes, Position.SIZE);
					BlockType = (Block.BlockType)BitConverter.ToUInt16(bytes, Position.SIZE * 2);
				}
			}

			WorldData.PlaceCuboid(Position1, Position2, BlockType);

			if (Config.IsServer)
			{
				foreach (var player in Server.Controller.Players.Values)
				{
					new AddCuboid(Position1, Position2, BlockType) { ConnectedPlayer = player }.Send();
				}
			}
			else
			{
				//play the sound relative to the closer of the 2 diagonal corners (accurate enough for cuboids, otherwise we would have to check around the entire perimeter)
				var playerPosition = Game.Player.Coords.ToPosition();
				if (Position1.GetDistanceExact(ref playerPosition) < Position2.GetDistanceExact(ref playerPosition))
				{
					Sounds.Audio.PlaySound(Sounds.SoundType.AddBlock, ref Position1);
				}
				else
				{
					Sounds.Audio.PlaySound(Sounds.SoundType.AddBlock, ref Position2);
				}
			}
		}
	}
}
