using System;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.Hosts.World;
using OpenTK;

namespace Hexpoint.Blox.GameActions
{
	internal class AddProjectile : GameAction
	{
		public AddProjectile()
		{
			DataLength = Coords.SIZE + Vector3.SizeInBytes + sizeof(ushort) + sizeof(bool) + sizeof(int); //coords + velocity + block type + AllowBounce + item ID
		}

		public AddProjectile(ref Coords coords, ref Vector3 velocity, Block.BlockType blockType, bool allowBounce, int gameObjectId = -1) : this()
		{
			Coords = coords;
			Velocity = velocity;
			BlockType = blockType;
			AllowBounce = allowBounce;
			GameObjectId = gameObjectId;
		}
		
		public override string ToString()
		{
			return string.Format("AddProjectile {0} P{1} V{2} bounce={3}", BlockType, Coords, Velocity, AllowBounce);
		}

		internal override ActionType ActionType { get { return ActionType.AddProjectile; } }
		public Coords Coords;
		public Vector3 Velocity;
		public Block.BlockType BlockType;
		public bool AllowBounce;
		public int GameObjectId;

		protected override void Queue()
		{
			base.Queue();
			Write(ref Coords);
			Write(ref Velocity);
			Write((ushort)BlockType);
			Write(AllowBounce);
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
					Velocity = new Vector3(BitConverter.ToSingle(bytes, Coords.SIZE),
										   BitConverter.ToSingle(bytes, Coords.SIZE + sizeof(float)),
										   BitConverter.ToSingle(bytes, Coords.SIZE + sizeof(float) * 2));
					BlockType = (Block.BlockType)BitConverter.ToUInt16(bytes, Coords.SIZE + Vector3.SizeInBytes);
					AllowBounce = BitConverter.ToBoolean(bytes, Coords.SIZE + Vector3.SizeInBytes + sizeof(ushort));
					GameObjectId = BitConverter.ToInt32(bytes, Coords.SIZE + Vector3.SizeInBytes + sizeof(ushort) + sizeof(bool));
				}
			}

			var newProjectile = new Projectile(ref Coords, BlockType, AllowBounce, Velocity, GameObjectId);

			if (Config.IsServer)
			{
				foreach (var player in Server.Controller.Players.Values)
				{
					new AddProjectile(ref newProjectile.Coords, ref newProjectile.Velocity, newProjectile.BlockType, newProjectile.AllowBounce, newProjectile.Id) { ConnectedPlayer = player }.Send();
				}
			}
		}
	}
}
