using System;
using System.Diagnostics;
using System.IO;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	public enum ServerCommandType : byte
	{
		ServerVersion,
		WorldSize,
		MoveSun,
		Broadcast
	}

	/// <summary>Used by clients to send a command request to the server.</summary>
	internal class ServerCommand : GameAction
	{
		public ServerCommand()
		{
			DataLength = sizeof(ushort) + sizeof(float);
		}

		public ServerCommand(ServerCommandType commandType, float param = 0) : this()
		{
			CommandType = commandType;
			Param = param;
		}

		public override string ToString()
		{
			return String.Format("ServerCommand {0} from PlayerId {1}", CommandType, ConnectedPlayer.Id);
		}

		internal override ActionType ActionType
		{
			get { return ActionType.ServerCommand; }
		}

		public ServerCommandType CommandType;
		/// <summary>Optional param that can be sent for performing actions with the command. Ex: changing sun radians.</summary>
		public float Param;

		protected override void Queue()
		{
			base.Queue();
			Write((ushort)CommandType);
			Write(Param);
		}

		internal override void Receive()
		{
			if (!Config.IsSinglePlayer)
			{
				lock (TcpClient)
				{
					base.Receive();
					var bytes = ReadStream(DataLength);
					CommandType = (ServerCommandType)BitConverter.ToUInt16(bytes, 0);
					Param = BitConverter.ToSingle(bytes, sizeof(ushort));
				}
			}

			switch (CommandType)
			{
				case ServerCommandType.ServerVersion:
					new ServerMsg(Settings.VersionDisplay, ConnectedPlayer).Send();
					break;
				case ServerCommandType.WorldSize:
					string message = !File.Exists(Settings.WorldFilePath) ? string.Format("World file not found @ {0}", Settings.WorldFilePath) : string.Format("World size {0}x{1} ({2} KB)", WorldData.SizeInBlocksX, WorldData.SizeInBlocksZ, new FileInfo(Settings.WorldFilePath).Length / 1024);
					new ServerMsg(message, ConnectedPlayer).Send();
					break;
				case ServerCommandType.MoveSun:
					if (IsAdmin)
					{
						Debug.Assert(Param >= 0 && Param <= 360, "Invalid degrees to move sun: " + Param);
						SkyHost.SunAngleRadians = OpenTK.MathHelper.DegreesToRadians(Param);
						//send a sync to each player immediately
						foreach (var player in Server.Controller.Players.Values) new ServerSync(SkyHost.SunAngleRadians, player).Send();
						//send confirmation to player that made the change
						new ServerMsg(string.Format("Moved sun to {0} degrees.", Param), ConnectedPlayer).Send();
					}
					else ConnectedPlayer.SendAdminRequiredMessage();
					break;
				case ServerCommandType.Broadcast:
					if (IsAdmin)
					{
						ServerMsg.Broadcast("Player sent broadcast message."); //theres no way to send the actual message from the client to broadcast yet
					}
					else ConnectedPlayer.SendAdminRequiredMessage();
					break;
			}
		}

	}
}
