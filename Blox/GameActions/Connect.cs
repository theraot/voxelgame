using System;
using System.Net.Sockets;
using System.Text;
using Hexpoint.Blox.GameObjects.Units;
using Hexpoint.Blox.Hosts.Ui;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	internal class Connect : GameAction
	{
		public Connect()
		{
			DataLength = sizeof(int) + 16 + 20 + Coords.SIZE;
		}

		public Connect(int playerId, string userName, Coords coords) : this()
		{
			PlayerId = playerId;
			UserName = userName.Length > 16 ? userName.Substring(0, 16) : userName;
			if (Settings.VersionDisplay.Length > 20) throw new Exception("Version string cannot be more than 20 characters.");
			Version = Settings.VersionDisplay;
			Coords = coords;
		}

		public override string ToString()
		{
			return string.Format("Connect ({0}) {1} v{2}", PlayerId, UserName, Version);
		}

		internal override ActionType ActionType { get { return ActionType.Connect; } }
		internal int PlayerId;
		internal string UserName;
		internal string Version;
		internal Coords Coords;

		protected override void Queue()
		{
			base.Queue();
			Write(PlayerId);
			Write(Encoding.ASCII.GetBytes(UserName.PadRight(16)), 16);
			Write(Encoding.ASCII.GetBytes(Version.PadRight(20)), 20);
			Write(ref Coords);
		}

		internal override void Receive()
		{
			lock (TcpClient)
			{
				base.Receive();
				var bytes = ReadStream(DataLength);
				PlayerId = BitConverter.ToInt32(bytes, 0);
				UserName = Encoding.ASCII.GetString(bytes, sizeof(int), 16).TrimEnd();
				Version = Encoding.ASCII.GetString(bytes, sizeof(int) + 16, 20).TrimEnd();
				Coords = new Coords(bytes, sizeof(int) + 16 + 20);
			}

			if (Config.IsServer)
			{
				if (Server.Controller.HasServerConsole) System.Media.SystemSounds.Exclamation.Play();
			}
			else
			{
				//todo: include position in this packet?
				NetworkClient.Players.TryAdd(PlayerId, new Player(PlayerId, UserName, Coords)); //note: it is not possible for the add to fail on ConcurrentDictionary, see: http://www.albahari.com/threading/part5.aspx#_Concurrent_Collections
				if (Game.UiHost != null) //ui host will be null for a client that is launching the game
				{
					Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Server, string.Format("{0} has connected.", UserName)));
					Sounds.Audio.PlaySound(Sounds.SoundType.PlayerConnect);
				}
			}
		}

		public void AcceptNewConnection(TcpClient client)
		{
			if (!Config.IsServer) throw new Exception("Only the server needs this.");

			TcpClient = client;
			var networkStream = client.GetStream();
			networkStream.ReadTimeout = (int)(PlayerInfo.PLAYER_INFO_SEND_INTERVAL * 1.5);
			networkStream.ReadByte();
			networkStream.ReadByte();
			Receive();

			var version = new Version(Version);
			if (version.Major != Settings.Version.Major || version.Minor != Settings.Version.Minor || version.Build != Settings.Version.Build)
			{
				var msg = string.Format("Requires Version {0}", Settings.VersionDisplay); //dont include revision number in this message
				new Disconnect(PlayerId, msg) { ConnectedPlayer = new Server.NetworkPlayer(PlayerId, UserName, TcpClient), Immediate = true }.Send();
				throw new Exception(msg);
			}
		}
	}
}
