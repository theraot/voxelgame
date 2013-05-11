using System;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	internal class RemoveBlock : GameAction
	{
		public RemoveBlock()
		{
			DataLength = Position.SIZE;
		}

		public RemoveBlock(ref Position position) : this()
		{
			Position = position;
		}

		public override string ToString()
		{
			return String.Format("RemoveBlock {0}", Position);
		}

		internal override ActionType ActionType { get { return ActionType.RemoveBlock; } }
		public Position Position;

		protected override void Queue()
		{
			base.Queue();
			Write(ref Position);
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
				}
			}

			var existingBlock = Position.GetBlock(); //store the existing block before we overwrite it
			WorldData.PlaceBlock(Position, Block.BlockType.Air);

			//if destroying a block, create an item
			BlockItem newBlockItem = null;
			if ((Config.IsSinglePlayer && !Config.CreativeMode) || (Config.IsServer && !ConnectedPlayer.IsCreative))
			{
				if (!existingBlock.IsTransparent)
				{
					var temp = Position.ToCoords();
					newBlockItem = new BlockItem(ref temp, existingBlock.Type);
				}
			}

			if (Config.IsServer)
			{
				foreach (var player in Server.Controller.Players.Values)
				{
					new RemoveBlock(ref Position) { ConnectedPlayer = player }.Send();
					if (newBlockItem != null) new AddBlockItem(ref newBlockItem.Coords, ref newBlockItem.Velocity, newBlockItem.BlockType, newBlockItem.Id) {ConnectedPlayer = player}.Send();
				}
			}
		}
	}
}
