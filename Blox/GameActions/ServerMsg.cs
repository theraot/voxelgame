using System;
using System.Text;
using Hexpoint.Blox.Hosts.Ui;

namespace Hexpoint.Blox.GameActions
{
	internal class ServerMsg : GameAction
	{
		public ServerMsg(string message = null)
		{
			Message = message;
			if (message != null) DataLength += message.Length;
		}

		/// <summary>Accept network player in constructor because this action is only sent by servers.</summary>
		public ServerMsg(string message, Server.NetworkPlayer player) : this(message)
		{
			ConnectedPlayer = player;
		}

		public override string ToString()
		{
			return String.Format("Server: {0}", Message);
		}

		internal override ActionType ActionType { get { return ActionType.ServerMsg; } }
		public string Message;

		protected override void Queue()
		{
			base.Queue();
			Write(Encoding.ASCII.GetBytes(Message), Message.Length);
		}

		internal override void Receive()
		{
			if (!Config.IsSinglePlayer)
			{
				lock (TcpClient)
				{
					base.Receive();
					var bytes = ReadStream(DataLength);
					Message = Encoding.ASCII.GetString(bytes, 0, DataLength);
				}
			}

			Sounds.Audio.PlaySound(Sounds.SoundType.Message);
			if (Game.UiHost != null) Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Server, string.Format("[SERVER] {0}", Message)));
		}

		/// <summary>Broadcast message from server to all connected players.</summary>
		public static void Broadcast(string message)
		{
			if (Config.IsServer)
			{
				foreach (var player in Server.Controller.Players.Values)
				{
					if (!player.TcpClient.Client.Connected) return;
					new ServerMsg(message, player).Send();
				}
			}
			else if (Config.IsSinglePlayer)
			{
				//todo: is this still applicable?
				//you wouldnt think a single player could broadcast a server message, but one time this happens is the "World saved" message
				//-removed the '[Server]' in front of the message since that was deceiving
				Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Server, message));
			}
			else
			{
				throw new Exception("Network clients cant broadcast a server message.");
			}
		}
	}
}