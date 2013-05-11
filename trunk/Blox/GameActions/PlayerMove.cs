using System;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	internal class PlayerMove : GameAction
	{
		public PlayerMove()
		{
			DataLength = Coords.SIZE + sizeof(int);
		}

		public PlayerMove(Coords coords, int playerId) : this()
		{
			Coords = coords;
			PlayerId = playerId;
		}

		public override string ToString()
		{
			return String.Format("PlayerMove {0} d{1:f1} p{2:f1}", Coords, Coords.Direction, Coords.Pitch);
		}

		internal override ActionType ActionType { get { return ActionType.PlayerMove; } }
		public Coords Coords;
		public int PlayerId;

		protected override void Queue()
		{
			base.Queue();
			Write(ref Coords);
			Write(PlayerId);
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
					PlayerId = BitConverter.ToInt32(bytes, Coords.SIZE);
				}
			}

			if (Config.IsServer)
			{
				Server.Controller.Players[PlayerId].Coords = Coords;
				foreach (var player in Server.Controller.Players.Values)
				{
					if (player.Id == PlayerId) continue; //no need to send move back to the player thats moving
					//future enhancement could be to check if the other players are within renderable distance and skip sending the move packet to them
					//-an issue will be that other players still need to somewhat know where each other are
					//-could possibly be solved by sending no more than one move packet per second or something for players that are out of range
					//	-this is enough to know where they are, prevent them from looking stuck when going out of range, etc.
					//	-would cut down a lot of packets on servers with many players and large world, although this isnt really a big issue yet
					new PlayerMove(Coords, PlayerId) { ConnectedPlayer = player }.Send();
				}
			}
			else if (!Config.IsSinglePlayer) //this is a network client
			{
				//gm: this assignment will be roughly 3x slower for ConcurrentDictionary, however is worth it for simpler code, less bugs and some performance gains for not having to lock while iterating
				//see: http://www.albahari.com/threading/part5.aspx#_Concurrent_Collections
				NetworkClient.Players[PlayerId].Coords = Coords;
			}
		}
	}
}
