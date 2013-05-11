using System;
using System.Text;
using Hexpoint.Blox.Hosts.Ui;

namespace Hexpoint.Blox.GameActions
{
	internal class Disconnect : GameAction
	{
		public Disconnect()
		{
			DataLength = sizeof(int) + 30;
		}

		public Disconnect(int playerId, string reason) : this()
		{
			PlayerId = playerId;
			Reason = reason.Length > 30 ? reason.Substring(0, 30) : reason;
		}

		public override string ToString()
		{
			return string.Format("Disconnect ({0}): {1}", PlayerId, Reason);
		}

		internal override ActionType ActionType { get { return ActionType.Disconnect; } }
		/// <summary>Id of the player that disconnected.</summary>
		internal int PlayerId;
		internal string Reason;

		protected override void Queue()
		{
			base.Queue();
			Write(PlayerId);
			Write(Encoding.ASCII.GetBytes(Reason.PadRight(30)), 30);
		}

		internal override void Receive()
		{
			lock (TcpClient)
			{
				base.Receive();
				var bytes = ReadStream(DataLength);
				PlayerId = BitConverter.ToInt32(bytes, 0);
				Reason = Encoding.ASCII.GetString(bytes, sizeof(int), 30).TrimEnd();
			}

			if (Config.IsServer)
			{
				Server.NetworkPlayer removedPlayer;
				Server.Controller.Players.TryRemove(ConnectedPlayer.Id, out removedPlayer);
				if (ConnectedPlayer.TcpClient.Connected)
				{
					ConnectedPlayer.TcpClient.GetStream().Close();
					ConnectedPlayer.TcpClient.Close();
				}
				Server.Controller.UpdateServerConsolePlayerList();
				Server.Controller.WriteToServerConsoleLog(string.Format("{0} (id {1}, ip {2}) Disconnected: {3}", ConnectedPlayer.UserName, ConnectedPlayer.Id, ConnectedPlayer.IpAddress, Reason));
				foreach (var otherPlayer in Server.Controller.Players.Values)
				{
					new Disconnect(ConnectedPlayer.Id, Reason) { ConnectedPlayer = otherPlayer }.Send(); //inform other connected players of this player disconnect
				}
			}
			else
			{
				if (PlayerId == -1 || (Game.Player != null && PlayerId == Game.Player.Id)) throw new Exception(Reason);
				
				GameObjects.Units.Player disconnectedPlayer;
				NetworkClient.Players.TryRemove(PlayerId, out disconnectedPlayer);
				if (disconnectedPlayer != null && Game.UiHost != null) Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Server, string.Format("{0} has disconnected: {1}", disconnectedPlayer.UserName, Reason)));	
			}
		}
	}
}
