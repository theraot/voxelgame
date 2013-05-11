using System;
using System.Collections.Generic;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.Hosts.World;
using OpenTK;

namespace Hexpoint.Blox.GameActions
{
	internal class RemoveBlockMulti : GameAction
	{
		public override string ToString()
		{
			return String.Format("RemoveBlockMulti ({0} blocks, {1} items)", Blocks.Count, BlockItems.Count);
		}

		internal override ActionType ActionType { get { return ActionType.RemoveBlockMulti; } }
		internal readonly List<RemoveBlock> Blocks = new List<RemoveBlock>();
		internal readonly List<AddBlockItem> BlockItems = new List<AddBlockItem>();

		protected override void Queue()
		{
			DataLength = sizeof(int) + sizeof(int) + (Position.SIZE * Blocks.Count) + (BlockItems.Count * (Coords.SIZE + Vector3.SizeInBytes + sizeof(ushort) + sizeof(int))); //num blocks + num items + each block + each item
			base.Queue();
			Write(Blocks.Count);
			Write(BlockItems.Count);
			foreach (var block in Blocks) Write(ref block.Position);
			foreach (var blockItem in BlockItems)
			{
				Write(ref blockItem.Coords);
				Write(ref blockItem.Velocity);
				Write((ushort)blockItem.BlockType);
				Write(blockItem.GameObjectId);
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
					var blockItemCount = BitConverter.ToInt32(ReadStream(sizeof(int)), 0);

					for (var i = 0; i < blockCount; i++)
					{
						var bytes = ReadStream(Position.SIZE);
						var position = new Position(bytes, 0);
						Blocks.Add(new RemoveBlock(ref position));
					}

					for (var i = 0; i < blockItemCount; i++)
					{
						var bytes = ReadStream(Coords.SIZE + Vector3.SizeInBytes + sizeof(ushort) + sizeof(int));
						var coords = new Coords(bytes, 0);
						var velocity = new Vector3(BitConverter.ToSingle(bytes, Coords.SIZE),
											   BitConverter.ToSingle(bytes, Coords.SIZE + sizeof(float)),
											   BitConverter.ToSingle(bytes, Coords.SIZE + sizeof(float) * 2));
						var blockType = (Block.BlockType)BitConverter.ToUInt16(bytes, Coords.SIZE + Vector3.SizeInBytes);
						var gameObjectId = BitConverter.ToInt32(bytes, Coords.SIZE + Vector3.SizeInBytes + sizeof(ushort));
						BlockItems.Add(new AddBlockItem(ref coords, ref velocity, blockType, gameObjectId));
					}
				}
			}

			Settings.ChunkUpdatesDisabled = true;
			foreach (var removeBlock in Blocks) WorldData.PlaceBlock(removeBlock.Position, Block.BlockType.Air);
			Settings.ChunkUpdatesDisabled = false;

			if (!Config.IsServer && !Config.IsSinglePlayer)
			{
				foreach (var blockItem in BlockItems)
				{
					// ReSharper disable ObjectCreationAsStatement
					new BlockItem(ref blockItem.Coords, blockItem.BlockType, blockItem.Velocity, blockItem.GameObjectId);
					// ReSharper restore ObjectCreationAsStatement
				}
			}

			if (Config.IsServer && (Blocks.Count > 0 || BlockItems.Count > 0))
			{
				foreach (var player in Server.Controller.Players.Values)
				{
					var removeBlockMulti = new RemoveBlockMulti { ConnectedPlayer = player };
					removeBlockMulti.Blocks.AddRange(Blocks);
					removeBlockMulti.BlockItems.AddRange(BlockItems);
					removeBlockMulti.Send();
				}
			}
		}
	}
}