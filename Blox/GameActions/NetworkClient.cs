using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Hexpoint.Blox.GameObjects.GameItems;
using Hexpoint.Blox.GameObjects.Units;
using Hexpoint.Blox.Hosts.Ui;
using Hexpoint.Blox.Hosts.World;

namespace Hexpoint.Blox.GameActions
{
	internal static class NetworkClient
	{
		internal static IPAddress ServerIp { get; private set; }
		internal static ushort ServerPort { get; private set; }
		internal static TcpClient TcpClient;
		private static NetworkStream _tcpStream;
		internal static bool AcceptPackets;

		/// <summary>All connected players. Includes local player.</summary>
		internal static ConcurrentDictionary<int, Player> Players = new ConcurrentDictionary<int, Player>();

		/// <summary>Timer used for sending player info to the server periodically.</summary>
		private static System.Timers.Timer _playerInfoTimer;

		internal static void Connect(object sender, DoWorkEventArgs e)
		{
			var args = (object[])e.Argument;
			ServerIp = (IPAddress)args[0];
			ServerPort = (ushort)args[1];
			Players.Clear();

			//when running client and server it takes a second for the server to start listening so make multiple connection attempts
			//when joining a server only try once because the attempt can take a long time ~22secs
			var connectionAttempts = (Config.IsSinglePlayer ? 4 : 1);
			for (var tries = 0; tries < connectionAttempts; tries++)
			{
				Settings.Launcher.UpdateProgressInvokable(string.Format("Connecting ({0} of {1})", tries + 1, connectionAttempts), tries + 1, connectionAttempts);
				try
				{
					TcpClient = new TcpClient();
					TcpClient.Connect(ServerIp, ServerPort);
					break;
				}
				catch (SocketException)
				{
					if (tries == connectionAttempts - 1) throw new ServerConnectException();
					Thread.Sleep(1000); //wait one second before trying again
				}
			}

			_tcpStream = TcpClient.GetStream();
			_tcpStream.ReadTimeout = 15000; //15s timeout during connect

			Settings.Launcher.UpdateProgressInvokable("Connected...", 0, 0);
			var connect = new Connect(-1, Config.UserName, new Coords());
			try
			{
				connect.Send();
				//server will immediately reply to tell us where we are and our Id, or disconnect us
				var actionTypebytes = new byte[sizeof(ushort)];
				var bytesRead = 0;
				while (bytesRead < actionTypebytes.Length) bytesRead += _tcpStream.Read(actionTypebytes, bytesRead, actionTypebytes.Length - bytesRead);
				var actionType = (ActionType)BitConverter.ToUInt16(actionTypebytes, 0);
				if (actionType == ActionType.Connect)
				{
					connect.Receive();
				}
				else if(actionType == ActionType.Disconnect)
				{
					var disconnect = new Disconnect();
					disconnect.Receive();
				}
				else
				{
					throw new Exception(string.Format("Received {0} packet out of order during connect sequence.", actionType));
				}
				
				Game.Player = Players[connect.PlayerId];

				//then a list of the players connected, followed by the world
				Settings.Launcher.UpdateProgressInvokable("Waiting for World...", 0, 0);
				while (!WorldData.IsLoaded)
				{
					bytesRead = 0;
					while (bytesRead < actionTypebytes.Length) bytesRead += _tcpStream.Read(actionTypebytes, bytesRead, actionTypebytes.Length - bytesRead);

					actionType = (ActionType)BitConverter.ToUInt16(actionTypebytes, 0);
					switch (actionType)
					{
						case ActionType.Connect:
							var recvPlayerList = new Connect();
							recvPlayerList.Receive();
							break;
						case ActionType.GetWorld:
							var getWorld = new GetWorld();
							getWorld.Receive();
							break;
						default:
							throw new Exception(string.Format("Received {0} packet out of order during connect sequence.", actionType));
					}
				}
			}
			catch (Exception ex)
			{
				//HandleNetworkError(ex);
				throw new ServerConnectException(ex);
			}

			_tcpStream.ReadTimeout = -1;
			TcpClient.NoDelay = true;
			var listenerThread = new Thread(ListenForServerMessageThread) { IsBackground = true, Name = "ListenForServerMessageThread" };
			listenerThread.Start();

			//send misc player information to server periodically (ie fps, memory).
			_playerInfoTimer = new System.Timers.Timer(PlayerInfo.PLAYER_INFO_SEND_INTERVAL);
			_playerInfoTimer.Start();
			_playerInfoTimer.Elapsed += _playerInfoTimer_Elapsed; //wire elapsed event handler
		}

		//runs in a thread
		public static void ListenForServerMessageThread()
		{
			while (!AcceptPackets) Thread.Sleep(100); //spin until the game window is loaded
			try
			{
				while (TcpClient.Connected)
				{
					var actionTypeBytes = new byte[sizeof(ushort)];
					var bytesRead = 0;
					while (bytesRead < actionTypeBytes.Length) bytesRead += _tcpStream.Read(actionTypeBytes, bytesRead, actionTypeBytes.Length - bytesRead);
					var actionType = (ActionType)BitConverter.ToUInt16(actionTypeBytes, 0);
					switch (actionType)
					{
						case ActionType.AddBlock: new AddBlock().Receive(); break;
						case ActionType.AddBlockItem: new AddBlockItem().Receive(); break;
						case ActionType.AddBlockMulti: new AddBlockMulti().Receive(); break;
						case ActionType.AddCuboid: new AddCuboid().Receive(); break;
						case ActionType.AddProjectile: new AddProjectile().Receive(); break;
						case ActionType.AddStaticItem: new AddStaticItem().Receive(); break;
						case ActionType.AddStructure: new AddStructure().Receive(); break;
						case ActionType.ChatMsg: new ChatMsg().Receive(); break;
						case ActionType.Connect: new Connect().Receive(); break;
						case ActionType.Disconnect: new Disconnect().Receive(); break;
						case ActionType.PickupBlockItem: new PickupBlockItem().Receive(); break;
						case ActionType.PlayerMove: new PlayerMove().Receive(); break;
						case ActionType.PlayerOption: new PlayerOption().Receive(); break;
						case ActionType.RemoveBlock: new RemoveBlock().Receive(); break;
						case ActionType.RemoveBlockItem: new RemoveBlockItem().Receive(); break;
						case ActionType.RemoveBlockMulti: new RemoveBlockMulti().Receive(); break;
						case ActionType.ServerMsg: new ServerMsg().Receive(); break;
						case ActionType.ServerSync: new ServerSync().Receive(); break;
						case ActionType.ServerCommand:
						case ActionType.PlayerInfo:
						case ActionType.Error:
						case ActionType.GetWorld:
							throw new Exception(string.Format("Client should not receive action type: {0}", actionType));
						default:
							throw new Exception(string.Format("Unhandled action type: {0}", actionType));
					}
				}
			}
			catch (Exception ex)
			{
				HandleNetworkError(ex);
			}
		}

		#region Send
		private static Coords _prevCoords = new Coords(0, 0, 0);

		public static void SendPlayerLocation(Coords newCoords, bool forceSend = false)
		{
			var minDeltaToBeDiff = (Players.Count >= 5 ? Constants.BLOCK_SIZE / 4 : Constants.BLOCK_SIZE / 8);
			var minAngleToBeDiff = (Players.Count >= 5 ? Constants.PI_OVER_6 : Constants.PI_OVER_12);
			if (forceSend || Math.Abs(_prevCoords.Xf - newCoords.Xf) > minDeltaToBeDiff || Math.Abs(_prevCoords.Yf - newCoords.Yf) > minDeltaToBeDiff || Math.Abs(_prevCoords.Zf - newCoords.Zf) > minDeltaToBeDiff || Math.Abs(_prevCoords.Direction - newCoords.Direction) > minAngleToBeDiff || Math.Abs(_prevCoords.Pitch - newCoords.Pitch) > minAngleToBeDiff)
			{
				new PlayerMove(newCoords, Game.Player.Id).Send();

				var chunk = WorldData.Chunks[newCoords];
				foreach (var gameItem in chunk.GameItems.Values)
				{
					if (gameItem.Type == GameItemType.BlockItem && newCoords.GetDistanceExact(ref gameItem.Coords) <= 2)
					{
						if (!Config.CreativeMode)
						{
							new PickupBlockItem(Game.Player.Id, gameItem.Id).Send();
						}
					}
				}

				_prevCoords = newCoords;
			}
		}

		/// <summary>Send message to the server to add or remove a block. Block will be removed if the block type is Air.</summary>
		/// <param name="position">position to add the block</param>
		/// <param name="blockType">block type to add</param>
		public static void SendAddOrRemoveBlock(Position position, Block.BlockType blockType)
		{
			if (!position.IsValidBlockLocation) return;
			if (blockType == Block.BlockType.Air) //remove block
			{
				if (position.Y == 0) { Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Error, "Cannot remove a block at the base of the world. Block cancelled.")); return; }
				new RemoveBlock(ref position).Send();
			}
			else //add block
			{
				var head = Game.Player.CoordsHead;
				if (!Config.CreativeMode && Game.Player.Inventory[(int)blockType] <= 0) { Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Error, string.Format("No {0} in inventory.", blockType))); return; }
				if (Block.IsBlockTypeSolid(blockType) && (position.IsOnBlock(ref Game.Player.Coords) || position.IsOnBlock(ref head))) { Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Error, "Attempted to build solid block on self. Not smart. Block cancelled.")); return; }

				foreach (var player in Players.Values)
				{
					head = player.CoordsHead;
					if (!Block.IsBlockTypeSolid(blockType) || (!position.IsOnBlock(ref player.Coords) && !position.IsOnBlock(ref head))) continue;
					Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Error, "Attempted to build solid block on other player. Not nice. Block cancelled.")); return;
				}
				new AddBlock(ref position, blockType).Send();
				if (!Config.CreativeMode) Game.Player.Inventory[(int)blockType]--;
			}
		}

		public static void Disconnect()
		{
			lock (TcpClient)
			{
				new Disconnect(Game.Player.Id, "Quit").Send();
				if (TcpClient.Connected)
				{
					_tcpStream.Close();
					TcpClient.Close();
				}
			}
		}

		private static void _playerInfoTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (Game.PerformanceHost == null)
			{
				Debug.WriteLine("Performance Host not initialized yet");
				return;
			}
			new PlayerInfo(Game.PerformanceHost.Fps, Game.PerformanceHost.Memory).Send();
		}
		#endregion

		internal static void HandleNetworkError(Exception ex)
		{
			lock (TcpClient)
			{
				if (TcpClient.Connected)
				{
					TcpClient.GetStream().Close();
					TcpClient.Close();
				}
			}

			var msg = string.Format("Disconnected from Server: {0}\n", ex.Message);
#if DEBUG
			msg += ex.StackTrace;
#endif
			if (Settings.Game == null) Utilities.Misc.MessageError(msg);
			if (Game.UiHost != null)
			{
				foreach (var line in msg.Split('\n')) Game.UiHost.AddChatMessage(new ChatMessage(ChatMessageType.Error, line));
			}
			Debug.WriteLine(msg);
		}
	}
}
