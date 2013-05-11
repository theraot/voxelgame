using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.Server
{
	internal class NetworkPlayer : GameObjects.Units.Player
	{
		#region Constructors
		internal NetworkPlayer(int id, string userName, TcpClient tcpClient) : base(id, userName, new Coords())
		{
			TcpClient = tcpClient;
			ConnectTime = DateTime.Now;
		}
		#endregion

		#region Networking Properties
		private TcpClient _tcpClient;
		internal TcpClient TcpClient
		{
			get { return _tcpClient; }
			set
			{
				_tcpClient = value;
				IpAddress = ((System.Net.IPEndPoint)TcpClient.Client.RemoteEndPoint).Address.ToString();
				Port = (ushort)((System.Net.IPEndPoint)TcpClient.Client.RemoteEndPoint).Port;
			}
		}

		/// <summary>Use this way to display the client IP address rather then string manipulations.</summary>
		internal string IpAddress { get; private set; }

		/// <summary>Use this way to display the client port number rather then string manipulations.</summary>
		internal ushort Port { get; private set; }

		internal bool IsConnected { get { return TcpClient.Client.Connected; } }
		internal DateTime ConnectTime { get; private set; }
		internal TimeSpan ConnectDuration { get { return DateTime.Now - ConnectTime; } }

		internal ConcurrentQueue<GameAction> SendQueue = new ConcurrentQueue<GameAction>();
		#endregion

		#region Session Info Properties
		internal bool IsAdmin;
		internal bool IsCreative;
		internal int MoveSpeedMultiplier = 1;

		internal short Fps;
		internal short Memory;

		internal string FlagsText { get { return string.Format("{0}{1}{2}", 
			IsAdmin ? "A" : string.Empty, 
			IsCreative ? "C" : string.Empty, 
			MoveSpeedMultiplier != 1 ? "S" + MoveSpeedMultiplier : string.Empty ); } }
		#endregion

		#region Network Messages
		internal void SendAdminRequiredMessage()
		{
			new ServerMsg("Must be an Admin to use this command.", this).Send();
		}
		#endregion
	}
}