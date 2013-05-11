using System;
using System.Collections.Generic;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	internal class PickupBlockItem : GameAction
	{
		public PickupBlockItem()
		{
			DataLength = sizeof(int) * 2; //player id, item id
		}

		public PickupBlockItem(int playerId, int gameObjectId) : this()
		{
			PlayerId = playerId;
			GameObjectId = gameObjectId;
		}

		public override string ToString()
		{
			return String.Format("PickupBlockItem Player {0} Obj {1}", PlayerId, GameObjectId);
		}

		internal override ActionType ActionType { get { return ActionType.PickupBlockItem; } }
		public int PlayerId;
		public int GameObjectId;

		protected override void Queue()
		{
			base.Queue();
			Write(PlayerId);
			Write(GameObjectId);
		}

		internal override void Send()
		{
			if (!Config.IsServer && PendingPickups.Contains(GameObjectId)) return;
			base.Send();
			PendingPickups.Add(GameObjectId);
		}

		internal override void Receive()
		{
			if (!Config.IsSinglePlayer)
			{
				lock (TcpClient)
				{
					base.Receive();
					var bytes = ReadStream(DataLength);
					PlayerId = BitConverter.ToInt32(bytes, 0);
					GameObjectId = BitConverter.ToInt32(bytes, sizeof(int));
				}
			}

			//bm: the removal could be requested more than once as the player moves while waiting for a response. maybe this should be prevented client-side
			if (!WorldData.GameItems.ContainsKey(GameObjectId)) return;

			var blockItem = (BlockItem)WorldData.GameItems[GameObjectId];
			var chunk = WorldData.Chunks[blockItem.Coords];
			GameItemDynamic remove;
			chunk.GameItems.TryRemove(GameObjectId, out remove);
			WorldData.GameItems.TryRemove(GameObjectId, out remove);

			if (Config.IsServer)
			{
				foreach (var player in Server.Controller.Players.Values)
				{
					if (player.Id == PlayerId)
					{
						//this player is picking it up, update their inventory and send a different packet so they do too
						player.Inventory[(int)blockItem.BlockType]++;
						new PickupBlockItem(PlayerId, GameObjectId) { ConnectedPlayer = player }.Send();	
					}
					else
					{
						//as far as this player knows it was destroyed (any reason to do otherwise?)
						new RemoveBlockItem(GameObjectId, false) { ConnectedPlayer = player }.Send();
					}
				}
			}
			else
			{
				Game.Player.Inventory[(int)blockItem.BlockType]++;
				Sounds.Audio.PlaySound(Sounds.SoundType.ItemPickup, ref blockItem.Coords);
				PendingPickups.Remove(GameObjectId);
			}
		}

		private readonly static List<int> PendingPickups = new List<int>(); //keep track of the items we've requested to pick up, avoid spamming the requests
	}
}
