using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	internal class AddStaticItem : GameAction
	{
		internal AddStaticItem()
		{
			DataLength = Coords.SIZE + (sizeof(ushort) * 3) + sizeof(int); //coords + type + sub type + item ID
		}

		internal AddStaticItem(ref Coords coords, StaticItemType staticItemType, ushort subType, Face attachedToFace, int gameObjectId = -1) : this()
		{
			Coords = coords;
			StaticItemType = staticItemType;
			SubType = subType;
			AttachedToFace = attachedToFace;
			GameObjectId = gameObjectId;
		}

		internal AddStaticItem(LightSource lightSource) : this()
		{
			Coords = lightSource.Coords;
			StaticItemType = StaticItemType.LightSource;
			SubType = (ushort)lightSource.Type;
			AttachedToFace = lightSource.AttachedToFace;
			GameObjectId = lightSource.Id;
		}

		internal override ActionType ActionType { get { return ActionType.AddStaticItem; } }
		public Coords Coords;
		public StaticItemType StaticItemType;
		public ushort SubType;
		public Face AttachedToFace;
		public int GameObjectId;

		protected override void Queue()
		{
			base.Queue();
			Write(ref Coords);
			Write((ushort)StaticItemType);
			Write(SubType);
			Write((ushort)AttachedToFace);
			Write(GameObjectId);
		}

		internal override void Receive()
		{
			if (!Config.IsSinglePlayer)
			{
				lock (TcpClient)
				{
					base.Receive();
					var bytes = ReadStream(DataLength);
					Coords = new Coords(bytes, 0);
					StaticItemType = (StaticItemType)BitConverter.ToUInt16(bytes, Coords.SIZE);
					SubType = BitConverter.ToUInt16(bytes, Coords.SIZE + sizeof(ushort));
					AttachedToFace = (Face)BitConverter.ToUInt16(bytes, Coords.SIZE + sizeof(ushort) * 2);
					GameObjectId = BitConverter.ToInt32(bytes, Coords.SIZE + sizeof(ushort) * 3);
				}
			}

			switch (StaticItemType)
			{
				case StaticItemType.Clutter:
					throw new NotSupportedException("Clutter cannot be placed yet.");
				case StaticItemType.LightSource:
					new LightSource(ref Coords, (LightSourceType)SubType, AttachedToFace, GameObjectId);
					if (!Config.IsServer) //lighting needs to be updated and affected chunks queued for non servers when adding a light source
					{
						var position = Coords.ToPosition();
						Task<Queue<Chunk>>.Factory.StartNew(() => Lighting.UpdateLightBox(ref position, null, false, false)).ContinueWith(task => WorldData.QueueAffectedChunks(task.Result));
					}
					break;
				default:
					throw new Exception(string.Format("Unknown static item type: {0}", StaticItemType));
			}

			if (Config.IsServer)
			{
				foreach (var player in Server.Controller.Players.Values)
				{
					new AddStaticItem(ref Coords, StaticItemType, SubType, AttachedToFace, GameObjectId) {ConnectedPlayer = player}.Send();
				}
			}
		}

		public override string ToString()
		{
			return string.Format("AddStaticItem {0} {1}", StaticItemType, Coords);
		}
	}
}
