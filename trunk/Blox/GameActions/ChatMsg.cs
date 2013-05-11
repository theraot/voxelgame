using System;
using System.Text;
using Hexpoint.Blox.Hosts.Ui;

namespace Hexpoint.Blox.GameActions
{
	internal class ChatMsg : GameAction
	{
		public ChatMsg(int fromPlayerId = 0, string message = null)
		{
			FromPlayerId = fromPlayerId;
			Message = message;
			DataLength = sizeof(int);
			if (message != null) DataLength += message.Length;
		}

		public override string ToString()
		{
			return String.Format("Chat ({0}): {1}", FromPlayerId, Message);
		}

		internal override ActionType ActionType { get { return ActionType.ChatMsg; } }
		public int FromPlayerId;
		public string Message;

		protected override void Queue()
		{
			if (!Config.IsServer) Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Global, string.Format("<{0}> {1}", Game.Player.UserName, Message)));
			
			base.Queue();
			Write(FromPlayerId);
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
					FromPlayerId = BitConverter.ToInt32(bytes, 0);
					Message = Encoding.ASCII.GetString(bytes, sizeof(int), DataLength - sizeof(int));
				}
			}

			if (Config.IsServer)
			{
				//receive a chat message from a player and send it to all the other connected players
				Server.Controller.WriteToServerConsoleLog(string.Format("<{0}>({1}): {2}", Server.Controller.Players[ConnectedPlayer.Id].UserName, ConnectedPlayer.Id, Message));
				foreach (var player in Server.Controller.Players.Values)
				{
					if (player.Id == ConnectedPlayer.Id) continue; //dont send messages back to the player that sent it
					if (!player.TcpClient.Client.Connected) return; //todo: needs comment, why do we return if this player isnt connected, shouldnt we just continue?
					new ChatMsg(ConnectedPlayer.Id, Message) {ConnectedPlayer = player}.Send();
				}
			}
			else if (Game.UiHost != null) //this is a client
			{
				//only plays for clients receiving a message because the sender doesnt receive their own message
				Sounds.Audio.PlaySound(Sounds.SoundType.Message);
				Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Global, string.Format("<{0}> {1}", NetworkClient.Players[FromPlayerId].UserName, Message)));
			}

		}
	}
}