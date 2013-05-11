using System;
using System.Collections.Generic;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	internal class AddBlockMulti : GameAction
	{
		public override string ToString()
		{
			return String.Format("AddBlockMulti ({0} blocks)", Blocks.Count);
		}

		internal override ActionType ActionType { get { return ActionType.AddBlockMulti; } }
		internal readonly List<AddBlock> Blocks = new List<AddBlock>();
		
		protected override void Queue()
		{
			DataLength = sizeof(int) + Blocks.Count * (Position.SIZE + sizeof(ushort)); //num blocks + each block
			base.Queue();
			Write(Blocks.Count);
			foreach (var block in Blocks)
			{
				Write(ref block.Position);
				Write((ushort)block.BlockType);
			}
		}

		internal override void Receive()
		{
			if (!Config.IsSinglePlayer)
			{
				lock (TcpClient)
				{
					base.Receive();
					var blockCount = BitConverter.ToInt32(ReadStream(sizeof(int)), 0);
					
					for (var i = 0; i < blockCount; i++)
					{
						var bytes = ReadStream(Position.SIZE + sizeof(ushort));
						var position = new Position(bytes, 0);
						var blockType = (Block.BlockType)BitConverter.ToUInt16(bytes, Position.SIZE);
						Blocks.Add(new AddBlock(ref position, blockType));
					}
				}
			}

			Settings.ChunkUpdatesDisabled = true;
			foreach (var addBlock in Blocks) WorldData.PlaceBlock(addBlock.Position, addBlock.BlockType);
			Settings.ChunkUpdatesDisabled = false;

			if (Config.IsServer)
			{
				//bm: this has to wait until the server can manage who's in creative mode
				//if (ConnectedPlayer.Inventory[(int)BlockType] <= 0) return;
				//ConnectedPlayer.Inventory[(int)BlockType] -= 1;

				foreach (var player in Server.Controller.Players.Values)
				{
					var addBlockMulti = new AddBlockMulti { ConnectedPlayer = player };
					addBlockMulti.Blocks.AddRange(Blocks);
					addBlockMulti.Send();
				}
			}
		}
	}
}