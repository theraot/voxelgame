using System;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	internal class AddBlock : GameAction
	{
		public AddBlock()
		{
			DataLength = Position.SIZE + sizeof(ushort); //coords + block type
		}

		public AddBlock(ref Position position, Block.BlockType blockType) : this()
		{
			if (blockType == Block.BlockType.Air) throw new Exception("You can't place air, use RemoveBlock");
			Position = position;
			BlockType = blockType;
		}

		public override string ToString()
		{
			return String.Format("AddBlock {0} {1}", BlockType, Position);
		}

		internal override ActionType ActionType { get { return ActionType.AddBlock; } }
		public Position Position;
		public Block.BlockType BlockType;

		protected override void Queue()
		{
			base.Queue();
			Write(ref Position);
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
					Position = new Position(bytes, 0);
					BlockType = (Block.BlockType)BitConverter.ToUInt16(bytes, Position.SIZE);
				}
			}

			WorldData.PlaceBlock(Position, BlockType);
			
			if (Config.IsServer)
			{
				//bm: this has to wait until the server can manage who's in creative mode
				//if (ConnectedPlayer.Inventory[(int)BlockType] <= 0) return;
				//ConnectedPlayer.Inventory[(int)BlockType] -= 1;

				foreach (var player in Server.Controller.Players.Values)
				{
					new AddBlock(ref Position, BlockType) { ConnectedPlayer = player }.Send();
				}
			}
		}
	}
}