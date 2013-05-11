using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Hexpoint.Blox.GameActions;
using Hexpoint.Blox.Hosts.World;
using OpenTK;

namespace Hexpoint.Blox.Server
{
	internal static class Controller
	{
		#region Properties
		internal const int TCP_LISTENER_PORT = 3000;
		internal const string WORLD_SAVED_MESSAGE = "World saved.";
		private static TcpListener _tcpListener;
		private static Thread _tcpListenerThread;
		private static ServerConsole _serverConsole;
		/// <summary>Is there currently a server console window running. Many updates can be skipped if theres not. A single player game controller does not have a server console.</summary>
		internal static bool HasServerConsole { get { return _serverConsole != null; } }
		private static int _nextPlayerId;

		/// <summary>All players connected to the server including the local player when applicable.</summary>
		internal static ConcurrentDictionary<int, NetworkPlayer> Players;

		private static System.Timers.Timer _updateTimer;

		internal static bool CaptureIncoming { get; set; }
		internal static bool CaptureOutgoing { get; set; }
		internal static string AdminPassword { get; set; }
		#endregion

		/// <summary>Launch the game controller.</summary>
		/// <param name="console">null when running controller in a single player game</param>
		public static void Launch(ServerConsole console = null)
		{
			_serverConsole = console;
			Players = new ConcurrentDictionary<int, NetworkPlayer>();
			AdminPassword = Settings.Random.Next(10000000, 99999999).ToString(); //default an 8 digit admin password

			if (File.Exists(Settings.WorldFilePath))
			{
				WriteToServerConsoleLog(string.Format("Loading world: {0}", Settings.WorldFilePath));
				WorldData.LoadFromDisk();
				WorldData.IsLoaded = true;
			}
			else
			{
				throw new Exception("World file not found: " + Settings.WorldFilePath);
			}

			if (!Config.IsSinglePlayer)
			{
				WriteToServerConsoleLog("Binding listener to TCP port " + TCP_LISTENER_PORT);
				_tcpListener = new TcpListener(System.Net.IPAddress.Any, TCP_LISTENER_PORT);
				_tcpListenerThread = new Thread(ListenForNewConnectionThread) { IsBackground = true, Name = "ListenForNewConnectionThread" };
				_tcpListenerThread.Start();
				WriteToServerConsoleLog("Listener started.");
			}

			Settings.SaveToDiskEveryMinuteThread = new Thread(SaveToDiskEveryMinuteThread) { IsBackground = true, Priority = ThreadPriority.Lowest, Name = "SaveToDiskEveryMinuteThread" }; //lowest priority makes it significantly less choppy when saving in single-player
			Settings.SaveToDiskEveryMinuteThread.Start();

			if (Config.IsServer)
			{
				_updateTimer = new System.Timers.Timer(1000 / Constants.UPDATES_PER_SECOND) { AutoReset = true };
				_updateTimer.Elapsed += UpdateHandler;
				_updateTimer.Start();
				UpdateStopwatch.Start();

				//start a thread to run periodic server tasks on a regular interval
				new Thread(() =>
					{
						while (true)
						{
							Thread.Sleep(30000); //30 seconds
							//send sync action to each player
							foreach (var player in Players.Values) new ServerSync(SkyHost.SunAngleRadians, player).Send();
						}
					// ReSharper disable FunctionNeverReturns
					}) {IsBackground = true, Priority = ThreadPriority.Normal, Name = "Server30SecondTasks"}.Start();
					// ReSharper restore FunctionNeverReturns
			}
		}

		/// <summary>Write a message to the server console log.</summary>
		internal static void WriteToServerConsoleLog(string msg)
		{
			// ReSharper disable RedundantStringFormatCall (gm: need the string.format when using only a single string param or it matches the category signature instead)
			Debug.WriteLine(string.Format("[Server] {0}", msg));
			// ReSharper restore RedundantStringFormatCall
			if (HasServerConsole) _serverConsole.UpdateLogInvokable(msg);
		}

		internal static void WriteToServerStreamLog(GameAction gameAction, NetworkPlayer player, bool isSending)
		{
			_serverConsole.UpdateStreamLogInvokable(gameAction, player, isSending);
		}

		internal static void UpdateServerConsolePlayerList()
		{
			if (HasServerConsole) _serverConsole.UpdatePlayerListInvokable();
		}

		/// <summary>Runs in single player and servers.</summary>
		private static void SaveToDiskEveryMinuteThread()
		{
			while(true)
			{
				Thread.Sleep(60000);
				try
				{
					WorldData.SaveToDisk();
					WriteToServerConsoleLog(WORLD_SAVED_MESSAGE);
				}
				catch (Exception ex)
				{
					WriteToServerConsoleLog("Error saving world: " + ex.Message);
				}
			}
		// ReSharper disable FunctionNeverReturns
		}
		// ReSharper restore FunctionNeverReturns

		private static void ListenForNewConnectionThread()
		{
			try
			{
				_tcpListener.Start();
			}
			catch (SocketException ex)
			{
				//todo: this will cause hard crash on the client, need a nicer way for the client to handle errors here
				if (ex.ErrorCode == 10048) throw new Exception("Only one server allowed at a time.");
			}
			
			while (true)
			{
				TcpClient client;
				System.Net.EndPoint endPoint;
				//blocks until a client has connected to the server
				try
				{
					client = _tcpListener.AcceptTcpClient();
					client.GetStream().WriteTimeout = 30000;
					endPoint = client.Client.RemoteEndPoint;
				}
				catch
				{
					WriteToServerConsoleLog("Failed to accept connection");
					continue;
				}
				
				try
				{
					WriteToServerConsoleLog(string.Format("Accepting connection from {0}", endPoint));
					var connect = new Connect();
					connect.AcceptNewConnection(client); //backdoor constructor because a player does not exist yet

					var player = new NetworkPlayer(_nextPlayerId, connect.UserName, client) {Coords = new Coords(WorldData.SizeInBlocksX / 2f, 0, WorldData.SizeInBlocksZ / 2f)};
					player.Coords.Yf = WorldData.Chunks[player.Coords].HeightMap[player.Coords.Xblock % Chunk.CHUNK_SIZE, player.Coords.Zblock % Chunk.CHUNK_SIZE] + 1; //start player on block above the surface
					new Connect(player.Id, player.UserName, player.Coords) { ConnectedPlayer = player, Immediate = true }.Send();
					_nextPlayerId++;

					WriteToServerConsoleLog(String.Format("{0} (id {1}, ip {2}) Connected", player.UserName, player.Id, player.IpAddress));
					var tcpThread = new Thread(() => PlayerThread(player)) { IsBackground = true, Name = "PlayerThread" };
					tcpThread.Start();
				}
				catch (Exception ex)
				{
					WriteToServerConsoleLog(string.Format("Failed to accept connection from {0}: {1}", endPoint, ex.Message));
				}
			}
			// ReSharper disable FunctionNeverReturns
		}
		// ReSharper restore FunctionNeverReturns

		private static void PlayerThread(NetworkPlayer player)
		{
			NetworkStream clientStream;

			//make all the introductions. we do this before sending the world so the client doesn't see them as new connections
			foreach (var otherPlayer in Players.Values)
			{
				try
				{
					new Connect(otherPlayer.Id, otherPlayer.UserName, otherPlayer.Coords) { ConnectedPlayer = player, Immediate = true }.Send();
				}
				catch (Exception ex)
				{
					WriteToServerConsoleLog(string.Format("{0} {1} caused an exception and was removed: {2}", player.UserName, player.IpAddress, ex.Message));
#if DEBUG
					WriteToServerConsoleLog(ex.StackTrace);
#endif
				}

				new Connect(player.Id, player.UserName, player.Coords) { ConnectedPlayer = otherPlayer }.Send();
			}

			try
			{
				Players.TryAdd(player.Id, player); //note: it is not possible for the add to fail on ConcurrentDictionary, see: http://www.albahari.com/threading/part5.aspx#_Concurrent_Collections
				UpdateServerConsolePlayerList();

				var getWorld = new GetWorld { ConnectedPlayer = player };
				getWorld.Send();
				WriteToServerConsoleLog(String.Format("World send complete to {0} ({1} compressed, {2} uncompressed)", player.IpAddress, getWorld.DataLength, getWorld.UncompressedLength));

				//create a thread to handle communication with connected client
				player.TcpClient.NoDelay = true;
				clientStream = player.TcpClient.GetStream();
			}
			catch (Exception ex)
			{
				HandleNetworkError(player, ex);
				return;	
			}

			var actionTypebytes = new byte[sizeof(ushort)];
			try
			{
				if (!string.IsNullOrWhiteSpace(Config.MOTD)) new ServerMsg(Config.MOTD, player).Send();

				while (true)
				{
					Thread.Sleep(10); //bm: polling is expensive. don't remove this or the server will pin your machine when only a couple users are online
					GameAction gameAction;
					while (player.SendQueue.Count > 0 && player.SendQueue.TryDequeue(out gameAction))
					{
						gameAction.Immediate = true;
						gameAction.Send();
					}

					if (!clientStream.DataAvailable) continue;
					var bytesRead = 0;
					while (bytesRead < actionTypebytes.Length) bytesRead += clientStream.Read(actionTypebytes, bytesRead, actionTypebytes.Length - bytesRead);
					var actionType = (ActionType)BitConverter.ToUInt16(actionTypebytes, 0);
					switch (actionType)
					{
						case ActionType.AddBlock:
							gameAction = new AddBlock();
							break;
						case ActionType.AddBlockItem:
							gameAction = new AddBlockItem();
							break;
						case ActionType.AddBlockMulti:
							gameAction = new AddBlockMulti();
							break;
						case ActionType.AddCuboid:
							gameAction = new AddCuboid();
							break;
						case ActionType.AddProjectile:
							gameAction = new AddProjectile();
							break;
						case ActionType.AddStaticItem:
							gameAction = new AddStaticItem();
							break;
						case ActionType.AddStructure:
							gameAction = new AddStructure();
							break;
						case ActionType.ChatMsg:
							gameAction = new ChatMsg();
							break;
						case ActionType.Disconnect:
							gameAction = new Disconnect();
							break;
						case ActionType.PickupBlockItem:
							gameAction = new PickupBlockItem();
							break;
						case ActionType.PlayerInfo:
							gameAction = new PlayerInfo();
							break;
						case ActionType.PlayerMove:
							gameAction = new PlayerMove();
							break;
						case ActionType.PlayerOption:
							gameAction = new PlayerOption();
							break;
						case ActionType.RemoveBlock:
							gameAction = new RemoveBlock();
							break;
						case ActionType.RemoveBlockItem:
							gameAction = new RemoveBlockItem();
							break;
						case ActionType.RemoveBlockMulti:
							gameAction = new RemoveBlockMulti();
							break;
						case ActionType.ServerCommand:
							gameAction = new ServerCommand();
							break;
						case ActionType.Connect:
						case ActionType.ServerMsg:
						case ActionType.ServerSync:
						case ActionType.GetWorld:
							throw new Exception(string.Format("Server should not receive action type: {0}", actionType));
						case ActionType.Error:
							var bytes = 0;
							while (clientStream.ReadByte() != -1)
							{
								bytes++;
							}
							throw new Exception("GameAction 'Error' received. " + bytes + " byte(s) remained in the stream.");
						default:
							throw new Exception(string.Format("Unknown action type: {0}", actionType));
					}
					gameAction.ConnectedPlayer = player;
					gameAction.Receive();
					if (HasServerConsole && CaptureIncoming) //only stream messages if there is a console window and it has requested to display them
					{
						_serverConsole.UpdateStreamLogInvokable(gameAction, player, false);
					}
					if (actionType == ActionType.Disconnect) return;
				}
			}
			catch (Exception ex)
			{		
				HandleNetworkError(player, ex);
			}
		}

		private static readonly Stopwatch UpdateStopwatch = new Stopwatch();
		private static void UpdateHandler(object sender, System.Timers.ElapsedEventArgs e)
		{
			//todo: this is running slightly slower then the updates on clients, might want to investigate at some point
			Debug.Assert(Config.IsServer, "Controller update handler should only be running for servers.");

			//somehow this happens, and would cause an ArgumentOutOfRangeException from FrameEventArgs
			//gm: must be rare, i ran a server for 10 mins with 3 players and this didnt happen, maybe because we're updating more stuff now, doesnt hurt to leave it though for now
			if (UpdateStopwatch.ElapsedMilliseconds <= 0) return;

			var frameEventArgs = new FrameEventArgs(UpdateStopwatch.ElapsedMilliseconds / 1000d);
			UpdateStopwatch.Restart();

			SkyHost.UpdateSun(frameEventArgs);
			WorldData.Chunks.Update(frameEventArgs);
			GameObjects.GameItems.GameItemDynamic.UpdateAll(frameEventArgs);

			unchecked { Settings.UpdateCounter++; }
		}

		internal static void HandleNetworkError(NetworkPlayer player, Exception ex)
		{
			NetworkPlayer removedPlayer;
			if (Players.TryRemove(player.Id, out removedPlayer))
			{
				UpdateServerConsolePlayerList();
				WriteToServerConsoleLog(string.Format("{0} {1} caused an exception and was removed: {2}", player.UserName, player.IpAddress, ex.Message));
#if DEBUG
				WriteToServerConsoleLog(ex.StackTrace);
#endif
				foreach (var otherPlayer in Players.Values)
				{
					new Disconnect(player.Id, "Network Error") { ConnectedPlayer = otherPlayer }.Send(); //inform other connected players of this player disconnect
				}
			}

			lock (player.TcpClient)
			{
				if (player.TcpClient.Connected)
				{
					player.TcpClient.GetStream().Close();
					player.TcpClient.Close();
				}
			}
		}
	}
}
